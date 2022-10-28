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

using System;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    ///<summary>
    ///</summary>
    public interface IUnitOfWorkScope : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Event fired when the scope is comitting.
        /// </summary>
        event Action<IUnitOfWorkScope> ScopeComitting;

        /// <summary>
        /// Event fired when the scope is rollingback.
        /// </summary>
        event Action<IUnitOfWorkScope> ScopeRollingback;

        /// <summary>
        /// Event fired when scope is beginning
        /// </summary>
        event Action<IUnitOfWorkScope> ScopeBeginning;

        /// <summary>
        /// Event fired when scope is completed
        /// </summary>
        event Action<IUnitOfWorkScope> ScopeCompleted;

        /// <summary>
        /// Gets the unique Id of the <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <value>A <see cref="Guid"/> representing the unique Id of the scope.</value>
        Guid TransactionId { get; }

        ///<summary>
        /// Commits the current running transaction in the scope.
        ///</summary>
        void Commit();

        /// <summary>
        /// Begins the transaction in the scope
        /// </summary>
        /// <param name="transactionMode"></param>
        /// <param name="isolationLevel"></param>
        void Begin(TransactionMode transactionMode = TransactionMode.Default, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Begins the transaction in the scope
        /// </summary>
        /// <param name="transactionMode"></param>
        void Begin(TransactionMode transactionMode = TransactionMode.Default);
    }
}
