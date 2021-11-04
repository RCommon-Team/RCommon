
using RCommon.Collections;
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    /// <summary>
    /// This represents a contract for the DiveService Application Service. 
    /// </summary>
    public interface IDiveService
    {
        Task<CommandResult<bool>> CreateDiveLocationAsync(DiveLocationDto diveLocationDto);
        Task<CommandResult<bool>> DeleteDiveLocationAsync(DiveLocationDto diveLocationDto);
        Task<CommandResult<bool>> UpdateDiveLocationAsync(DiveLocationDto diveLocationDto);

        Task<CommandResult<PaginatedListModel<DiveLocationDto>>> GetAllDiveLocationsAsync(int pageIndex, int pageSize);

        Task<CommandResult<ICollection<DiveTypeDto>>> GetAllDiveTypesAsync();

        Task<CommandResult<DiveLocationDto>> GetDiveLocationByIdAsync(Guid id);

        Task<CommandResult<PaginatedListModel<DiveLocationDto>>> SearchDiveLocationsAsync(string searchTerms, int pageIndex, int pageSize);
    }
}