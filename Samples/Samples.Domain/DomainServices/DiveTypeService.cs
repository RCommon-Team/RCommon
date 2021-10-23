using Microsoft.Extensions.Logging;
using RCommon.BusinessServices;
using RCommon.DataServices.Transactions;
using RCommon.ExceptionHandling;
using RCommon.ObjectAccess;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.Domain.DomainServices
{
    public class DiveTypeService : CrudBusinessService<DiveType>, IDiveTypeService
    {
        private readonly IFullFeaturedRepository<DiveType> _diveTypeRepository;

        public DiveTypeService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IFullFeaturedRepository<DiveType> diveTypeRepository, ILogger<DiveTypeService> logger, IExceptionManager exceptionManager) 
            : base(unitOfWorkScopeFactory, diveTypeRepository, logger, exceptionManager)
        {
            _diveTypeRepository = diveTypeRepository;
            _diveTypeRepository.DataStoreName = DataStoreDefinitions.Samples;
        }
    }
}
