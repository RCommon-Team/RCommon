
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.BusinessServices
{
    public interface ICrudBusinessService<TEntity>
        where TEntity : IBusinessEntity
    {
        Task<CommandResult<bool>> CreateAsync(TEntity entity);
        Task<CommandResult<bool>> DeleteAsync(TEntity entity);
        Task<CommandResult<TEntity>> GetByIdAsync(object primaryKey);
        Task<CommandResult<bool>> UpdateAsync(TEntity entity);
        Task<CommandResult<ICollection<TEntity>>> GetAllAsync();
    }
}