using Microsoft.AspNetCore.Mvc;
using RCommon.DataServices.Transactions;
using System.Transactions;

namespace RCommon.Web
{
    public class RCommonController : Controller
    {

        /// <summary>
        /// Stores transaction scope for request
        /// </summary>
        public TransactionScope TransactionScope { get; set; }



        public RCommonController()
        {
            
        }
    }
}
