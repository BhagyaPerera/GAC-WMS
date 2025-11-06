using Ardalis.Specification;

namespace SharedKernal.Interfaces
{
    public interface IRepository<T> : IRepositoryBase<T> where T : class,IAggregateRoot
    {
        // Additional repository methods can be defined here if needed
        // For example, you might want to add methods for specific queries or operations
        // that are not covered by the base IRepositoryBase<T> interface.
        Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    }
}