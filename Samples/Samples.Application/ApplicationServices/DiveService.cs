using AutoMapper;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices;
using RCommon.Collections;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using RCommon.Expressions;
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using Samples.Domain.DomainServices;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Application.ApplicationServices
{
    /// <summary>
    /// This represents an Application Service. It is responsible for managing the transactions gemane to our Dive services. While it is not responsible
    /// for any business logic, it does manage the transactions that business logic executes under. It also is responsbile for mapping business entities
    /// to DTOs for general consumption. In seperating these responsibilities from the Controller (which manages the web-based state), we can more easily
    /// accomplish the interface segregation principle upstream. https://en.wikipedia.org/wiki/Interface_segregation_principle
    /// </summary>
    public class DiveService : RCommonAppService, IDiveService
    {
        private readonly IDiveTypeService _diveTypeService;
        private readonly IDiveLocationService _diveLocationService;
        private readonly IMapper _objectMapper;

        /// <summary>
        /// All services are injected into the constructor for testability. This is a practical implementation of the dependency inversion principle. https://en.wikipedia.org/wiki/Dependency_inversion_principle
        /// </summary>
        /// <param name="diveLocationService">Dive Location Domain Service (business service)</param>
        /// <param name="objectMapper">Automapper instance</param>
        /// <param name="logger">Logger</param>
        /// <param name="exceptionManager">Exception Manager</param>
        /// <param name="unitOfWorkScopeFactory">Unit Of Work Scope Factory</param>
        /// <param name="diveTypeService">Dive Type Service (business service)</param>
        public DiveService(IDiveTypeService diveTypeService, IDiveLocationService diveLocationService, IMapper objectMapper, ILogger<DiveService> logger, IExceptionManager exceptionManager, IUnitOfWorkScopeFactory unitOfWorkScopeFactory) :
            base(logger, exceptionManager, unitOfWorkScopeFactory)
        {
            _diveTypeService = diveTypeService;
            _diveLocationService = diveLocationService;
            _objectMapper = objectMapper;
        }

        public async Task<CommandResult<bool>> DeleteDiveLocationAsync(DiveLocationDto diveLocationDto)
        {
            var cmd = new CommandResult<bool>();
            cmd.DataResult = false; // Default return value
            try
            {
                var diveLocation = _objectMapper.Map<DiveLocation>(diveLocationDto); // Map the DTO to an entity

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work/Transaction for multi-step operations.
                {
                    // Build the detail object from the DTO. We could also do this from AutoMapper if we wanted to.
                    var diveDetail = new DiveLocationDetail() { DiveLocationId = diveLocationDto.Id, ImageData = diveLocationDto.ImageData };

                    var detailCmd = await _diveLocationService.DeleteDiveLocationDetailsAsync(diveDetail); // Remove the detail record
                    var locationCmd = await _diveLocationService.DeleteAsync(diveLocation); // Remove the primary DiveLocation record

                    if (detailCmd.ValidationResult.IsValid && !detailCmd.HasException
                        && locationCmd.ValidationResult.IsValid && !locationCmd.HasException) // If all our business logic succeeds and there are no exceptions then commit
                    {
                        scope.Commit(); // Commit the transaction
                        cmd.DataResult = true;
                        cmd.Message = "Dive location successfully deleted."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogInformation(cmd.Message, diveLocationDto); // Log the info

                    }
                    else
                    {
                        cmd.Message = "An Error occured while deleting this dive location."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogWarning(cmd.Message, diveLocationDto); // Log a warning because the error will be handled. We may still be able to recover.
                    }
                    
                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }

            return cmd;
        }

        public async Task<CommandResult<bool>> CreateDiveLocationAsync(DiveLocationDto diveLocationDto)
        {
            var cmd = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                cmd.DataResult = false; // Default return value
                var diveLocation = this._objectMapper.Map<DiveLocation>(diveLocationDto); // Map the DTO to an Entity. This should also get us a unique Id 

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
                {
                    // Build the detail object from the DTO. We could also do this from AutoMapper if we wanted to.
                    var diveDetail = new DiveLocationDetail() { DiveLocationId = diveLocation.Id, ImageData = diveLocationDto.ImageData };

                    var detailCmd = await _diveLocationService.CreateDiveLocationDetailsAsync(diveDetail); // Add the detail record
                    var locationCmd = await _diveLocationService.CreateAsync(diveLocation); // Add the primary DiveLocation record

                    if (detailCmd.ValidationResult.IsValid && !detailCmd.HasException
                        && locationCmd.ValidationResult.IsValid && !locationCmd.HasException) // If all our business logic succeeds and there are no exceptions then commit
                    {
                        scope.Commit(); // Commit the transaction
                        cmd.DataResult = true;
                        cmd.Message = "Dive location successfully created."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogInformation(cmd.Message, diveLocationDto); // Log the info

                    }
                    else
                    {
                        cmd.Message = "An Error occured while creating this dive location."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogWarning(cmd.Message, diveLocationDto); // Log a warning because the error will be handled. We may still be able to recover.
                    }
                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;
        }

        public async Task<CommandResult<bool>> UpdateDiveLocationAsync(DiveLocationDto diveLocationDto)
        {
            var cmd = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                cmd.DataResult = false; // Default return value
                var diveLocation = this._objectMapper.Map<DiveLocation>(diveLocationDto); // Map the DTO to an Entity. This should also get us a unique Id 

                using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
                {

                    if (diveLocationDto.ImageData != null)
                    {
                        // Build the detail object from the DTO. We could also do this from AutoMapper if we wanted to.
                        var diveDetail = new DiveLocationDetail() { DiveLocationId = diveLocation.Id, ImageData = diveLocationDto.ImageData };

                        var detailCmd = await _diveLocationService.UpdateDiveLocationDetailsAsync(diveDetail); // Update the detail record
                    }
                    var locationCmd = await _diveLocationService.UpdateAsync(diveLocation); // Update the primary DiveLocation record

                    if (locationCmd.ValidationResult.IsValid && !locationCmd.HasException) // If all our business logic succeeds and there are no exceptions then commit
                    {
                        scope.Commit(); // Commit the transaction
                        cmd.DataResult = true;
                        cmd.Message = "Dive location successfully updated."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogInformation(cmd.Message, diveLocationDto); // Log the info

                    }
                    else
                    {
                        cmd.Message = "An Error occured while updating this dive location."; //TODO: should put this message in a resource file possibly for translation
                        this.Logger.LogWarning(cmd.Message, diveLocationDto); // Log a warning because the error will be handled. We may still be able to recover.
                    }
                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;
        }

        public async virtual Task<CommandResult<PaginatedListModel<DiveLocationDto>>> GetAllDiveLocationsAsync(int pageIndex, int pageSize)
        {
            var cmd = new CommandResult<PaginatedListModel<DiveLocationDto>>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                
                var locationCmd = await _diveLocationService.GetAllDiveLocationsAsync(true, pageIndex, pageSize);// Perform the work

                if (locationCmd.HasException)
                {

                    // Set a friendly error message. We may be able to recover from this.
                    cmd.Message = "An Error occured while retrieving the dive locations. Please try refreshing your screen.";
                }
                else
                {
                    var diveLocationList = _objectMapper.Map<ICollection<DiveLocationDto>>(locationCmd.DataResult.OrderBy(x=>x.LocationName)); // I would normally write a custom type converter (see below) for this if time allowed
                    //cmd.DataResult = _objectMapper.Map<PaginatedList<DiveLocationDto>>(locationCmd.DataResult); // Would need a custom type converter to do this but the in-memory affect would be the same
                    cmd.DataResult = new PaginatedListModel<DiveLocationDto>()
                    {
                        Data = diveLocationList,
                        PageIndex = locationCmd.DataResult.PageIndex,
                        PageSize = locationCmd.DataResult.PageSize,
                        TotalPages = locationCmd.DataResult.TotalPages,
                        TotalCount = locationCmd.DataResult.TotalCount,
                        HasNextPage = locationCmd.DataResult.HasNextPage,
                        HasPreviousPage = locationCmd.DataResult.HasPreviousPage
                    };// Map the PaginatedList to a DTO
                    
                }
                
            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;

        }

        public async virtual Task<CommandResult<PaginatedListModel<DiveLocationDto>>> SearchDiveLocationsAsync(string searchTerms, int pageIndex, int pageSize)
        {
            var cmd = new CommandResult<PaginatedListModel<DiveLocationDto>>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {
                
                var locationCmd = await _diveLocationService.SearchDiveLocationsAsync(searchTerms, true, pageIndex, pageSize);// Perform the work

                if (locationCmd.HasException)
                {

                    // Set a friendly error message. We may be able to recover from this.
                    cmd.Message = "An Error occured while retrieving the dive locations. Please try refreshing your screen.";
                }
                else
                {
                    var diveLocationList = _objectMapper.Map<ICollection<DiveLocationDto>>(locationCmd.DataResult.OrderBy(x => x.LocationName)); // I would normally write a custom type converter (see below) for this if time allowed
                    //cmd.DataResult = _objectMapper.Map<PaginatedList<DiveLocationDto>>(locationCmd.DataResult); // Would need a custom type converter to do this but the in-memory affect would be the same
                    cmd.DataResult = new PaginatedListModel<DiveLocationDto>()
                    {
                        Data = diveLocationList,
                        PageIndex = locationCmd.DataResult.PageIndex,
                        PageSize = locationCmd.DataResult.PageSize,
                        TotalPages = locationCmd.DataResult.TotalPages,
                        TotalCount = locationCmd.DataResult.TotalCount,
                        HasNextPage = locationCmd.DataResult.HasNextPage,
                        HasPreviousPage = locationCmd.DataResult.HasPreviousPage
                    };// Map the PaginatedList to a DTO

                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;

        }


        public async virtual Task<CommandResult<ICollection<DiveTypeDto>>> GetAllDiveTypesAsync()
        {
            var cmd = new CommandResult<ICollection<DiveTypeDto>>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var diveTypeCmd = await _diveTypeService.GetAllAsync();// Perform the work

                if (diveTypeCmd.HasException)
                {

                    // Set a friendly error message. We may be able to recover from this.
                    cmd.Message = "An Error occured while retrieving the dive types. Please try refreshing your screen.";
                }
                else
                {
                    cmd.DataResult = _objectMapper.Map<ICollection<DiveTypeDto>>(diveTypeCmd.DataResult.OrderBy(x=>x.DiveTypeName)); // Map the entity to a DTO
                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;

        }


        public async virtual Task<CommandResult<DiveLocationDto>> GetDiveLocationByIdAsync(Guid id)
        {
            var cmd = new CommandResult<DiveLocationDto>(); // We only return serializable Data transfer objects (DTO) from this layer

            try
            {

                var diveTypeCmd = await _diveLocationService.GetByIdAsync(id);// Perform the work

                if (diveTypeCmd.HasException)
                {

                    // Set a friendly error message. We may be able to recover from this.
                    cmd.Message = "An Error occured while retrieving the dive location. Please try refreshing your screen.";
                }
                else
                {
                    cmd.DataResult = _objectMapper.Map<DiveLocationDto>(diveTypeCmd.DataResult); // Map the entity to a DTO
                }

            }
            catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            }
            catch (AutoMapperMappingException ex) // Mapping Exception
            {
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
            {
                cmd.Exception = ex;
                this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            }
            return cmd;

        }



        
    }
}
