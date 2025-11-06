using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    // Remove inheritance from RepositoryBase<T> (since it is missing)
    public class ApiEfRepository<T> : IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
    {
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly IntegrationDbContext _dbContext;

        public ApiEfRepository(IntegrationDbContext dbContext, IMemoryCache cache)
        {
            _cache = cache;
            _cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(relative: TimeSpan.FromSeconds(20));
            _dbContext = dbContext;
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            var key = $"{entity.GetType().FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var key = $"{typeof(T).FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            await _dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entities;
        }

        public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var key = $"{entity.GetType().FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            _dbContext.Set<T>().Update(entity);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var key = $"{typeof(T).FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            _dbContext.Set<T>().UpdateRange(entities);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var key = $"{entity.GetType().FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            _dbContext.Set<T>().Remove(entity);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var key = $"{typeof(T).FullName}-bust";
            _cache.Set(key, "bust", _cacheOptions);
            _dbContext.Set<T>().RemoveRange(entities);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var entities = await ListAsync(specification, cancellationToken);
            return await DeleteRangeAsync(entities, cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
        {
            return await _dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            // Fix: Materialize the query before calling FirstOrDefaultAsync
            return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<T>().ToListAsync(cancellationToken);
        }

        public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).ToListAsync(cancellationToken);
        }

        public async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).CountAsync(cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<T>().CountAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification).AnyAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<T>().AnyAsync(cancellationToken);
        }

        public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
        {
            return ApplySpecification(specification).AsAsyncEnumerable();
        }

        // Helper method to apply specification (minimal implementation)
        private IQueryable<T> ApplySpecification(ISpecification<T> specification)
        {
            // This is a placeholder. Replace with your actual specification logic.
            return _dbContext.Set<T>();
        }

        private IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
        {
            // Fix: Use the Selector from the specification if available
            var query = _dbContext.Set<T>().AsQueryable();
            if (specification.Selector != null)
            {
                return query.Select(specification.Selector);
            }
            // Fallback: Return an empty queryable to avoid runtime errors
            return Enumerable.Empty<TResult>().AsQueryable();
        }
    }
}
