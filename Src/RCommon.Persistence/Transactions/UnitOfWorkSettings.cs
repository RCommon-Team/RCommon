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

using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    ///<summary>
    /// Contains default settings for <see cref="UnitOfWork"/> instances, including isolation level and auto-complete behavior.
    ///</summary>
    /// <remarks>
    /// These settings are applied when a <see cref="UnitOfWork"/> is created via DI with the default constructor.
    /// Configure these settings using <see cref="IUnitOfWorkBuilder.SetOptions(Action{UnitOfWorkSettings})"/>.
    /// </remarks>
    public class UnitOfWorkSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkSettings"/> class
        /// with <see cref="IsolationLevel.ReadCommitted"/> and auto-complete disabled.
        /// </summary>
        public UnitOfWorkSettings()
        {
            DefaultIsolation = IsolationLevel.ReadCommitted;
            AutoCompleteScope = false;
        }


        /// <summary>
        /// Gets the default <see cref="IsolationLevel"/>.
        /// </summary>
        public IsolationLevel DefaultIsolation { get; set; }

        /// <summary>
        /// Gets a boolean value indicating weather to auto complete
        /// <see cref="UnitOfWorkScope"/> instances.
        /// </summary>
        public bool AutoCompleteScope { get; set; }
    }
}
