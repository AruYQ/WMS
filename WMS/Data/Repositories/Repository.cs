using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WMS.Models;
using WMS.Services;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Generic repository implementation dengan automatic company filtering
    /// </summary>
    /// <typeparam name="T">Entity type yang inherit dari BaseEntity</typeparam>
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ICurrentUserService _currentUserService;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<Repository<T>> _logger;

        public Repository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<T>> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        /// <summary>
        /// Get base query dengan company filtering
        /// </summary>
        protected virtual IQueryable<T> GetBaseQuery()
        {
            var companyId = _currentUserService.CompanyId;
            _logger.LogInformation("=== REPOSITORY DEBUG START ===");
            _logger.LogInformation("Repository.GetBaseQuery() - EntityType: {EntityType}, CompanyId: {CompanyId}", 
                typeof(T).Name, companyId);
            
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning empty query for {EntityType}", typeof(T).Name);
                _logger.LogInformation("=== REPOSITORY DEBUG END (EMPTY QUERY) ===");
                return _dbSet.Where(x => false); // Return empty query
            }

            var query = _dbSet.Where(x => x.CompanyId == companyId.Value);
            _logger.LogInformation("Created query with CompanyId filter: {CompanyId} for {EntityType}", companyId.Value, typeof(T).Name);
            _logger.LogInformation("=== REPOSITORY DEBUG END ===");
            return query;
        }

        /// <summary>
        /// Get all entities untuk company yang sedang login
        /// </summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                return await GetBaseQuery().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Get entity by ID dengan company filtering
        /// </summary>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                return await GetBaseQuery().FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {EntityType} by ID {Id} for company {CompanyId}",
                    typeof(T).Name, id, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Find entities dengan condition dan company filtering
        /// </summary>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await GetBaseQuery().Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Get single entity dengan condition dan company filtering
        /// </summary>
        public virtual async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await GetBaseQuery().FirstOrDefaultAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting single {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Add entity dengan auto-set CompanyId
        /// </summary>
        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    throw new InvalidOperationException("No company context found for current user");
                }

                // Auto-set CompanyId
                entity.CompanyId = companyId.Value;

                // Set audit fields
                entity.CreatedDate = DateTime.Now;
                entity.CreatedBy = _currentUserService.Username;

                _dbSet.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, entity.Id, companyId.Value);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Update entity dengan company validation
        /// </summary>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    throw new InvalidOperationException("No company context found for current user");
                }

                // Validate that entity belongs to current company
                var existingEntity = await GetByIdAsync(entity.Id);
                if (existingEntity == null)
                {
                    throw new InvalidOperationException($"{typeof(T).Name} with ID {entity.Id} not found or does not belong to current company");
                }

                // Ensure CompanyId cannot be changed
                entity.CompanyId = companyId.Value;

                // Set audit fields
                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedBy = _currentUserService.Username;

                _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, entity.Id, companyId.Value);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, entity.Id, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Delete entity dengan company validation
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return false;
                }

                return await DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, id, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Delete entity dengan company validation
        /// </summary>
        public virtual async Task<bool> DeleteAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    throw new InvalidOperationException("No company context found for current user");
                }

                // Validate that entity belongs to current company
                if (entity.CompanyId != companyId.Value)
                {
                    throw new InvalidOperationException($"{typeof(T).Name} does not belong to current company");
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, entity.Id, companyId.Value);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, entity.Id, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Check if entity exists dengan company filtering
        /// </summary>
        public virtual async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await GetBaseQuery().AnyAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {EntityType} with ID {Id} for company {CompanyId}",
                    typeof(T).Name, id, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Check if entity exists dengan condition dan company filtering
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await GetBaseQuery().AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Count entities dengan company filtering
        /// </summary>
        public virtual async Task<int> CountAsync()
        {
            try
            {
                return await GetBaseQuery().CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Count entities dengan condition dan company filtering
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await GetBaseQuery().CountAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType} with predicate for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Get paginated results dengan company filtering
        /// </summary>
        public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null)
        {
            try
            {
                var query = GetBaseQuery();

                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<T>
                {
                    Items = items,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged {EntityType} for company {CompanyId}",
                    typeof(T).Name, _currentUserService.CompanyId);
                throw;
            }
        }
    }
}