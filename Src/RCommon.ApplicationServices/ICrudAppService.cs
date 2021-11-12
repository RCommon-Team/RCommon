using AutoMapper;
using RCommon.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public interface ICrudAppService<TDataTransferObject, TEntity>
    {
        Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto);
        Task<CommandResult<bool>> DeleteAsync(TDataTransferObject dto);
        Task<CommandResult<TDataTransferObject>> GetByIdAsync(object primaryKey);
        Task<CommandResult<bool>> UpdateAsync(TDataTransferObject dto);
        Task<CommandResult<ICollection<TDataTransferObject>>> GetAllAsync();

        IMapper ObjectMapper { get; set; }
    }
}