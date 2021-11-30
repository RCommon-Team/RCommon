using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    public class SearchPaginatedListRequest : PaginatedListRequest, ISearchPaginatedListRequest
    {
        public SearchPaginatedListRequest()
        {

        }
        public string SearchString { get; set; }
    }
}
