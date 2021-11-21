using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using RCommon.Collections;
using RCommon.ExceptionHandling;
using RCommon.Models;
using Samples.Application.ApplicationServices;
using Samples.Application.Contracts.Dto;
using Samples.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Controllers
{
    public class DiveLocationsController : Controller
    {
        private readonly IDiveService _diveService;
        private readonly ILogger<DiveLocationsController> _logger;
        private readonly IExceptionManager _exceptionManager;

        public DiveLocationsController(IDiveService diveService, ILogger<DiveLocationsController> logger, IExceptionManager exceptionManager)
        {
            _diveService = diveService;
            _logger = logger;
            _exceptionManager = exceptionManager;
        }

        public async Task<IActionResult> Index(int? currentPage, string? searchTerms)
        {
            var model = new DiveLocationListModel();
            try
            {
                model.CurrentPage = currentPage.GetValueOrDefault(1);
                CommandResult<PaginatedListModel<DiveLocationDto>> cmd = new CommandResult<PaginatedListModel<DiveLocationDto>>();
                if (searchTerms == null)
                {
                    cmd = await _diveService.GetAllDiveLocationsAsync(model.CurrentPage, PresentationDefaults.PagedDataSize);
                }
                else
                {
                    cmd = await _diveService.SearchDiveLocationsAsync(searchTerms, model.CurrentPage, PresentationDefaults.PagedDataSize);

                }

                model.PagedData = cmd.DataResult;

                return View(model);
            }
            catch (FriendlyApplicationException ex) // Expected
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.PresentationReplacePolicy);
            }
            catch (ApplicationException ex) // Unexpected
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                throw ex;
            }
            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            await this.PopulateSelectLists();
            return View();
        }

       
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, DiveLocationEditModel locationModel)
        {
            try
            {
                if (!this.ModelState.IsValid)
                {
                    await this.PopulateSelectLists();
                    return View(locationModel);
                }

                if (locationModel.Photo != null)
                {

                    using (var stream = new MemoryStream())
                    {
                        await locationModel.Photo.CopyToAsync(stream);
                        locationModel.DiveLocation.ImageData = stream.ToArray(); // TODO: This is pretty resource intensive way to store/access images. Prefer to use file system or CDN with cache.
                    }
                }

                var cmd = await _diveService.UpdateDiveLocationAsync(locationModel.DiveLocation);


                if (this.CheckResult(cmd)) // Success
                {
                    return RedirectToAction(nameof(Index));
                }

                // If we make it to here then we failed
                await this.PopulateSelectLists();
            }
            catch (FriendlyApplicationException ex) // It is possible to recover.
            {
                // This will allow us to format a "friendly error" which we may be able to recover from
                locationModel.DisplayMessage = ex.Message;
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.PresentationReplacePolicy);

            }
            catch (ApplicationException ex) // It is not possbile to recover.
            {
                // This will rethrow the exception and it will get handled by the global error handler
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy); 

            }
            return View(locationModel);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var locationModel = new DiveLocationEditModel();
            try
            {

                var cmd = await _diveService.GetDiveLocationByIdAsync(id);

                await this.PopulateSelectLists();
                locationModel.DiveLocation = cmd.DataResult;
            }
            catch (FriendlyApplicationException ex) // It is possible to recover.
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.PresentationReplacePolicy);

            }
            catch (ApplicationException ex) // It is not possbile to recover.
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);

            }
            return View(locationModel);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            
            try
            {
                var locationCmd = await _diveService.GetDiveLocationByIdAsync(id);
                var deleteCmd = await _diveService.DeleteDiveLocationAsync(locationCmd.DataResult);

                if (this.CheckResult(deleteCmd)) // Success
                {
                    return RedirectToAction(nameof(Index));
                }

                
            }
            catch (FriendlyApplicationException ex) // It is possible to recover.
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.PresentationReplacePolicy);

            }
            catch (ApplicationException ex) // It is not possbile to recover.
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);

            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DiveLocationCreateModel locationModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await this.PopulateSelectLists();
                    return View();
                }

                if (locationModel.Photo != null)
                {

                    using (var stream = new MemoryStream())
                    {
                        await locationModel.Photo.CopyToAsync(stream);
                        locationModel.DiveLocation.ImageData = stream.ToArray(); // TODO: This is pretty resource intensive way to store/access images. Prefer to use file system or CDN with cache.
                    }
                }
                else
                {
                    ModelState.AddModelError("file error", "A File must be selected.");
                    bool bValid = ModelState.IsValid; // Will trigger error messages to be displayed
                    await this.PopulateSelectLists();
                    return View(locationModel);
                }
                
                var cmd = await _diveService.CreateDiveLocationAsync(locationModel.DiveLocation);

                if (this.CheckResult(cmd)) // Success
                {
                    return RedirectToAction(nameof(Index));
                }

                // If we make it to here then we failed
                await this.PopulateSelectLists();
                

            }
            catch (FriendlyApplicationException ex) // It is possible to recover.
            {
                locationModel.DisplayMessage = ex.Message;
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.PresentationReplacePolicy);
                
            }
            catch (ApplicationException ex) // It is not possbile to recover.
            {
                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
                
            }

            return View(locationModel);
        }



        private async Task PopulateSelectLists()
        {
            var diveTypeCmd = await _diveService.GetAllDiveTypesAsync();
            ViewData["DiveTypeId"] = new SelectList(diveTypeCmd.DataResult, "Id", "DiveTypeName");
        }

        /// <summary>
        /// This will evaluate your <see cref="CommandResult{TResult}"/> and if you have validation errors or exceptions in your business layer
        /// then this method will update the <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/> so that it triggers
        /// the appropriate validation summary/info. Note that this only applies to server side validation for now.
        /// </summary>
        /// <typeparam name="TResult">Type embedded in your <see cref="CommandResult{TResult}"/></typeparam>
        /// <param name="cmd">The value of your <see cref="CommandResult{TResult}"/></param>
        /// <returns>true if all is valid</returns>
        private bool CheckResult(CommandResult<bool> cmd)
        {
            if (cmd.HasException)
            {
                ModelState.AddModelError("exception", "Errors");
                return false;
            }
            if (!cmd.ValidationResult.IsValid)
            {
                foreach (var error in cmd.ValidationResult.Errors)
                {
                    ModelState.AddModelError(error.Property, error.Message);
                }
                return false;

            }

            return true;
        }
    }
}
