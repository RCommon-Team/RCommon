using RCommon.Application.DTO;
using RCommon.Domain.DomainServices;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.Domain.DomainServices
{
    /// <summary>
    /// Represents a service contract with <see cref="DiveTypeService"/>
    /// </summary>
    public interface IDiveTypeService : ICrudDomainService<DiveType>
    {
        
    }
}
