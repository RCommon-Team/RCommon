using RCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Application.Contracts.Dto
{
    public class DiveLocationSearchRequest : PaginatedListRequest
    {
        public DiveLocationSearchRequest()
        {

        }

        public string SearchTerms { get; set; }
    }
}
