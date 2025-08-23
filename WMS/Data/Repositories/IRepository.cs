using WMS.Models;
using System.Linq.Expressions;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Generic repository interface dengan company filtering
    /// </summary>
    /// <typeparam name="T">Entity type yang inherit dari BaseEntity</typeparam>
    public interface IRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// Get all entities untuk company yang sedang login
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Get entity by ID dengan company filtering
        /// </summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Find entities dengan condition dan company filtering
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Get single entity dengan condition dan company filtering
        /// </summary>
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Add entity dengan auto-set CompanyId
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Update entity dengan company validation
        /// </summary>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Delete entity dengan company validation
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Delete entity dengan company validation
        /// </summary>
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// Check if entity exists dengan company filtering
        /// </summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Check if entity exists dengan condition dan company filtering
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Count entities dengan company filtering
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Count entities dengan condition dan company filtering
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Get paginated results dengan company filtering
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null);
    }

    /// <summary>
    /// Result untuk paging
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}