using RCommon.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.Domain.DomainServices
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

        CommandResult<ICollection<TEntity>> GetAll();

        Task<CommandResult<ICollection<TEntity>>> GetAllAsync();
    }
}