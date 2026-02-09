#region license
//Copyright 2010 Ritesh Rao 

//Licensed under the Apache License, Version 2.0 (the "License"); 
//you may not use this file except in compliance with the License. 
//You may obtain a copy of the License at 

//http://www.apache.org/licenses/LICENSE-2.0 

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion
#region license compliance
//Substantial changes to the original code have been made in the form of namespace reorganization, 
//and method signature.
//Original code here: https://github.com/riteshrao/ncommon/blob/v1.2/NCommon/src/Data/Impl/TransactionScopeHelper.cs
#endregion

using Microsoft.Extensions.Logging;
using System;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Helper class to create <see cref="TransactionScope"/> instances.
    /// </summary>
    public static class TransactionScopeHelper
    {

        /// <summary>
        /// Creates a <see cref="TransactionScope"/> based on the <see cref="IUnitOfWork.TransactionMode"/>
        /// of the specified unit of work.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output about the created scope type.</param>
        /// <param name="unitOfWork">The unit of work whose <see cref="IUnitOfWork.TransactionMode"/> and
        /// <see cref="IUnitOfWork.IsolationLevel"/> determine the scope configuration.</param>
        /// <returns>
        /// A <see cref="TransactionScope"/> configured as follows:
        /// <list type="bullet">
        /// <item><description><see cref="TransactionMode.New"/>: <see cref="TransactionScopeOption.RequiresNew"/> with the specified isolation level.</description></item>
        /// <item><description><see cref="TransactionMode.Supress"/>: <see cref="TransactionScopeOption.Suppress"/> (no transaction).</description></item>
        /// <item><description><see cref="TransactionMode.Default"/>: <see cref="TransactionScopeOption.Required"/> (joins existing or creates new).</description></item>
        /// </list>
        /// All scopes are created with <see cref="TransactionScopeAsyncFlowOption.Enabled"/> for async support.
        /// </returns>
        public static TransactionScope CreateScope(ILogger<UnitOfWork> logger, IUnitOfWork unitOfWork)
        {
            if (unitOfWork.TransactionMode == TransactionMode.New)
            {
                logger.LogDebug("Creating a new TransactionScope with TransactionScopeOption.RequiresNew");
                return new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = unitOfWork.IsolationLevel }, TransactionScopeAsyncFlowOption.Enabled);
            }
            if (unitOfWork.TransactionMode == TransactionMode.Supress)
            {
                logger.LogDebug("Creating a new TransactionScope with TransactionScopeOption.Supress");
                return new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            }
            // Default mode: join existing ambient transaction or create a new one
            logger.LogDebug("Creating a new TransactionScope with TransactionScopeOption.Required");
            return new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}
