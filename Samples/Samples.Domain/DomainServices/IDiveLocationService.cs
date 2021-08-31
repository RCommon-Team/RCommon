
using RCommon.BusinessServices;
using RCommon.Collections;
using RCommon.DataTransferObjects;
using RCommon.Expressions;
using Samples.Domain.Entities;
using System.Threading.Tasks;

namespace Samples.Domain.DomainServices
{
    /// <summary>
    /// Represents a service contract with <see cref="DiveLocationService"/>
    /// </summary>
    public interface IDiveLocationService : ICrudBusinessService<DiveLocation>
    {
        /// <summary>
        /// Retrieves a list of <see cref="DiveLocation"/>
        /// </summary>
        /// <param name="includeDetails">Flag for including Image and other details</param>
        /// <param name="pageIndex">Current index of paging</param>
        /// <param name="pageSize">Size of objects to include in paging</param>
        /// <returns>Returns an object called <see cref="CommandResult{TResult}"/> which encapsulates <see cref="IPaginatedList{T}"/> of type <see cref="DiveLocation"/></returns>
        /// <exception cref="BusinessException">BusinessException wraps all other exceptions.</exception>
        Task<CommandResult<IPaginatedList<DiveLocation>>> GetAllDiveLocationsAsync(bool includeDetails, int pageIndex, int pageSize);

        /// <summary>
        /// Deletes the <see cref="DiveLocationDetail"/> record.
        /// </summary>
        /// <param name="diveDetails">DiveDetail Record</param>
        /// <returns>True if success</returns>
        Task<CommandResult<bool>> DeleteDiveLocationDetailsAsync(DiveLocationDetail diveDetails);

        /// <summary>
        /// Updates details associated with <see cref="DiveLocation"/>
        /// </summary>
        /// <param name="locationDetail">Dive Location Details</param>
        /// <returns>True if success</returns>
        Task<CommandResult<bool>> UpdateDiveLocationDetailsAsync(DiveLocationDetail locationDetail);

        /// <summary>
        /// Creates details associated with <see cref="DiveLocation"/>
        /// </summary>
        /// <param name="locationDetail">Dive Location Details</param>
        /// <returns>True if success</returns>
        Task<CommandResult<bool>> CreateDiveLocationDetailsAsync(DiveLocationDetail locationDetail);

        Task<CommandResult<IPaginatedList<DiveLocation>>> SearchDiveLocationsAsync(string searchTerms, bool includeDetails, int pageIndex, int pageSize);
    }
}