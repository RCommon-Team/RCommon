using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Models
{
    public class UserSearchRequestModel : PagingRequestModel
    {
        public UserSearchRequestModel()
        {

        }

        public string SearchString { get; set; }
    }
}
