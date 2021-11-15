using System;
using System.Diagnostics;
using System.Transactions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RCommon.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionAttribute : ActionFilterAttribute
    {
        public TransactionAttribute()
        {
            Order = 1;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
            if (Debugger.IsAttached)
            {
                transactionOptions.Timeout = TimeSpan.FromMinutes(5);
            }
            ((RCommonController)context.Controller).TransactionScope =
                new TransactionScope(TransactionScopeOption.Required, transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            using (var transactionScope = ((RCommonController)context.Controller).TransactionScope)
            {
                if (context.Exception == null)
                {
                    transactionScope.Complete();
                }
            }
        }
    }
}