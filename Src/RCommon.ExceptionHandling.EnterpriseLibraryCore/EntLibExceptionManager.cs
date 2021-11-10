using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using System.Diagnostics;
using RCommon.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Configuration;
using AutoMapper.Configuration;
using RCommon.ExceptionHandling.EnterpriseLibraryCore.Handlers;
using System.Security;

namespace RCommon.ExceptionHandling.EnterpriseLibraryCore
{
    public class EntLibExceptionManager : IExceptionManager
    {
        public EntLibExceptionManager()
        {

        }

        public void HandleException(Exception ex, string policy)
        {
            Boolean rethrow = false;
            // This shouldn't catch the exception because we may be throwing a new one from the block.
            /*try
            {


            }
            catch (Exception innerException)
            {
                string errorMsg = "An unexpected exception occured while calling HandleException with policy '" + policy + "'."
                    + Environment.NewLine + innerException.ToString();
                throw ex; 
            }*/

            
            rethrow = ExceptionPolicy.HandleException(ex, policy);
            if (rethrow)
            {
                // NOTE: This will truncate the stack of the exception 
                throw ex;
            }

        }
    }
}
