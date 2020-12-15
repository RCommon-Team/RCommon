using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.Domain.DomainServices;
using RCommon.Domain.Repositories;
using RCommon.ExceptionHandling;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.Domain.DomainServices
{
    public class DiveTypeService : CrudDomainService<DiveType>, IDiveTypeService
    {
        private readonly IEagerFetchingRepository<DiveType> _diveTypeRepository;

        public DiveTypeService(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IEagerFetchingRepository<DiveType> diveTypeRepository, ILogger<DiveTypeService> logger, IExceptionManager exceptionManager) 
            : base(unitOfWorkScopeFactory, diveTypeRepository, logger, exceptionManager)
        {
            _diveTypeRepository = diveTypeRepository;
            _diveTypeRepository.DataStoreName = DataStoreDefinitions.Samples;
        }
    }
}
