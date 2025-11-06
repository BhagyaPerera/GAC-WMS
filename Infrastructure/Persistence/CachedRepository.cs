using Ardalis.Specification;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class CachedRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedRepository<T>> _logger;
        private readonly IRepository<T> _sourceRepository;
        private MemoryCacheEntryOptions _cacheOptions;

        public CachedRepository(IMemoryCache cache,
            ILogger<CachedRepository<T>> logger,
            IRepository<T> sourceRepository)
        {
            _cache = cache;
            _logger = logger;
            _sourceRepository = sourceRepository;

            _cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(relative: TimeSpan.FromSeconds(20));
        }

        /// <inheritdoc/>
        public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            // TODO: Add Caching
            return _sourceRepository.AnyAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Add Caching
            return _sourceRepository.AnyAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            // TODO: Add Caching
            return _sourceRepository.CountAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Add Caching
            return _sourceRepository.CountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.GetByIdAsync(id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.GetByIdAsync(id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T> GetBySpecAsync<Spec>(Spec specification, CancellationToken cancellationToken = default) where Spec : ISingleResultSpecification, ISpecification<T>
        {
            if (specification.CacheEnabled)
            {
                string key = $"{specification.CacheKey}-GetBySpecAsync";
                _logger.LogInformation("Checking cache for " + key);
                return _cache.GetOrCreate(key, entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    _logger.LogWarning("Fetching source data for " + key);
                    return _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
                });
            }
            return _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TResult> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        {
            var bustkey = $"{typeof(T).FullName}-bust";
            string key = $"{typeof(T).FullName}-ListAsync";

            string bustVal;
            if (_cache.TryGetValue(bustkey, out bustVal))
            {
                _cache.Remove(bustkey);
                _cache.Remove(key);
            }

            _logger.LogInformation("Checking cache for " + key);
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(_cacheOptions);
                return _sourceRepository.ListAsync(cancellationToken);
            });
        }

        /// <inheritdoc/>
        public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            if (specification.CacheEnabled)
            {
                var bustkey = $"{typeof(T).FullName}-bust";
                string key = $"{specification.CacheKey}-ListAsync";

                string bustVal;
                if (_cache.TryGetValue(bustkey, out bustVal))
                {
                    _cache.Remove(bustkey);
                    _cache.Remove(key);
                }

                _logger.LogInformation("Checking cache for " + key);
                return _cache.GetOrCreate(key, entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    _logger.LogWarning("Fetching source data for " + key);
                    return _sourceRepository.ListAsync(specification, cancellationToken);
                });
            }
            return _sourceRepository.ListAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (specification.CacheEnabled)
            {
                var bustkey = $"{typeof(T).FullName}-bust";
                string key = $"{specification.CacheKey}-ListAsync";

                string bustVal;
                if (_cache.TryGetValue(bustkey, out bustVal))
                {
                    _cache.Remove(bustkey);
                    _cache.Remove(key);
                }

                _logger.LogInformation("Checking cache for " + key);
                return _cache.GetOrCreate(key, entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    _logger.LogWarning("Fetching source data for " + key);
                    return _sourceRepository.ListAsync(specification, cancellationToken);
                });
            }
            return _sourceRepository.ListAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.FirstOrDefaultAsync<TResult>(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.SingleOrDefaultAsync(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            return _sourceRepository.SingleOrDefaultAsync<TResult>(specification, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
        {
            return _sourceRepository.AsAsyncEnumerable(specification);
        }
    }
}
