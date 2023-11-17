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
//and interface signature.
//Original code here: https://github.com/riteshrao/ncommon/blob/v1.2/NCommon/src/Data/IUnitOfWork.cs
#endregion

using System;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    ///<summary>
    ///</summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Event fired when the scope is comitting.
        /// </summary>
        event Action<IUnitOfWork> ScopeComitting;

        /// <summary>
        /// Event fired when the scope is rollingback.
        /// </summary>
        event Action<IUnitOfWork> ScopeRollingback;

        /// <summary>
        /// Event fired when scope is beginning
        /// </summary>
        event Action<IUnitOfWork> ScopeBeginning;

        /// <summary>
        /// Event fired when scope is completed
        /// </summary>
        event Action<IUnitOfWork> ScopeCompleted;

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
