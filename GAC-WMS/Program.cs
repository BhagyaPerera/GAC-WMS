using API.Background;
using Core.Entities.Identity;
using Core.EventHandlers;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Services;
using Core.Services.Identity;
using Infrastructure.Messaging.RabbitMQ;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using RabbitMQ.Client;
using SharedKernal.Interfaces;
using System.Text;
using System.Diagnostics.Tracing;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// =============================================================
// 1️⃣ Controllers + Swagger Setup
// =============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GAC WMS Integration API", Version = "v1" });

    // JWT Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    // Require JWT for secured endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =============================================================
// 2️⃣ Database (EF Core + Identity)
// =============================================================
builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("GAC-WMS") // specify migrations assembly
    );
});

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<IntegrationDbContext>()
    .AddDefaultTokenProviders();

// =============================================================
// 3️⃣ JWT Authentication Configuration
// =============================================================
var jwtSection = configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new Exception("JWT key missing"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// =============================================================
// 4️⃣ Caching + Repositories
// =============================================================
builder.Services.AddMemoryCache();
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(CachedRepository<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(ApiEfRepository<>));

// =============================================================
// 5️⃣ RabbitMQ Configuration
// =============================================================
builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
builder.Services.Configure<RabbitMqConfiguration>(configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IPooledObjectPolicy<IModel>, RabbitMqModelPooledObjectPolicy>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqHostedService>();

// =============================================================
// 6️⃣ Application & Domain Services
// =============================================================
builder.Services.AddScoped<PurchaseOrdersService>();
builder.Services.AddScoped<SalesOrdersService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AuthService>();

// =============================================================
// 7️⃣ Integration Event Handlers
// =============================================================
builder.Services.AddScoped<IIntegrationEventHandler<PurchaseOrderCreateEvent>, PurchaseOrderCreatedEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<SalesOrderCreateEvent>, SalesOrderCreatedEventHandler>();



// =============================================================
// 9️⃣ Additional Background Services
// =============================================================
builder.Services.AddHttpClient();
builder.Services.AddHostedService<FailedMessagesScheduler>();

// =============================================================
// 🔟 Build Application & Seed Roles/Admin
// =============================================================
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeeder.SeedRolesAndAdminAsync(services);
}

// =============================================================
// 🔟 Middleware Pipeline
// =============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GAC WMS Integration API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
