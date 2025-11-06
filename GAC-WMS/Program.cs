using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Text;
using API.Background;
using Core.Services;
using Core.Entities.Identity;
using Core.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Messaging.RabbitMQ;
using Core.Services.Identity;
using Core.Events.ApplicationEvents;
using Core.EventHandlers;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// =============================================================
// 1?.Controllers + Swagger Setup
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
// 2?.Database (EF Core + Identity)
// =============================================================
builder.Services.AddDbContext<IntegrationDbContext>(opts =>
{
    var connStr = configuration.GetConnectionString("DefaultConnection");
    opts.UseSqlServer(connStr);
});

// Identity configuration
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<IntegrationDbContext>()
    .AddDefaultTokenProviders();

// =============================================================
// 3?. JWT Authentication Configuration
// =============================================================
var jwtSection = configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]);

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
// 4?.RabbitMQ Configuration
// =============================================================
builder.Services.Configure<RabbitMqConfiguration>(configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqHostedService>();

// =============================================================
// 5?. Application Services
// =============================================================
builder.Services.AddScoped<PurchaseOrdersService>();
builder.Services.AddScoped<SalesOrdersService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AuthService>();

// =============================================================
// 6?. Integration Event Handlers
// =============================================================
builder.Services.AddScoped<IIntegrationEventHandler<PurchaseOrderCreateEvent>, PurchaseOrderCreatedEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<SalesOrderCreateEvent>, SalesOrderCreatedEventHandler>();

// =============================================================
// 7?. Background Services
// =============================================================


//builder.Services.AddQuartz(q =>
//{
//    // Register the job
//    var jobKey = new JobKey("FileIntegrationJob");
//    q.AddJob<FileIntegrationJob>(opts => opts.WithIdentity(jobKey));

//    // Schedule it with a CRON expression (every 5 minutes)
//    q.AddTrigger(opts => opts
//        .ForJob(jobKey)
//        .WithIdentity("FileIntegrationJob-trigger")
//        .WithCronSchedule("0 */5 * * * ?")  // Every 5 minutes
//    );
//});

//builder.Services.AddQuartzHostedService(opt =>
//{
//    opt.WaitForJobsToComplete = true;
//});

builder.Services.AddHttpClient();
builder.Services.AddHostedService<FailedMessagesScheduler>();

// =============================================================
// 8?. Build Application + Seed Roles/Admin
// =============================================================
var app = builder.Build();

// Seed Roles & Default Admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeeder.SeedRolesAndAdminAsync(services);
}

// =============================================================
// 9?. Middleware Pipeline
// =============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GAC WMS Integration API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();   // ? Must come before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
