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
//Original code here: https://github.com/riteshrao/ncommon/blob/v1.2/NCommon/src/Data/IUnitOfWorkFactory.cs
#endregion

using System;
using System.Transactions;

namespace RCommon.DataServices
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
        IUnitOfWork Create(TransactionMode mode);
        IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}
