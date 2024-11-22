using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    [DataContract]
    public record SearchPaginatedListRequest : PaginatedListRequest, ISearchPaginatedListRequest
    {
        public SearchPaginatedListRequest()
        {

        }

        [DataMember]
        public string SearchString { get; set; }
    }
}
