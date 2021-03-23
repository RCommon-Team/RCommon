using RCommon.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.Domain.DomainServices
{
    public interface ICrudDomainService<TEntity> 
        where TEntity : class
    {
        Task<CommandResult<bool>> CreateAsync(TEntity entity);
        Task<CommandResult<bool>> DeleteAsync(TEntity entity);
        Task<CommandResult<TEntity>> GetByIdAsync(object primaryKey);
        Task<CommandResult<bool>> UpdateAsync(TEntity entity);
        Task<CommandResult<ICollection<TEntity>>> GetAllAsync();
    }
}