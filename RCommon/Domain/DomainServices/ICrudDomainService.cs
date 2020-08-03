using RCommon.Application.DTO;
using System.Threading.Tasks;

namespace Reactor2.CMS.DomainServices
{
    public interface ICrudDomainService<TEntity> 
        where TEntity : class
    {
        CommandResult<TEntity> Create(TEntity entity);
        Task<CommandResult<bool>> CreateAsync(TEntity entity);
        CommandResult<bool> Delete(TEntity entity);
        Task<CommandResult<bool>> DeleteAsync(TEntity entity);
        CommandResult<TEntity> GetById(object primaryKey);
        Task<CommandResult<TEntity>> GetByIdAsync(object primaryKey);
        CommandResult<bool> Update(TEntity entity);
        Task<CommandResult<bool>> UpdateAsync(TEntity entity);
    }
}