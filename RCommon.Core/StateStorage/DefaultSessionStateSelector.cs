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

using RCommon;
using RCommon.StateStorage.AspNetCore;

namespace RCommon.StateStorage
{
    /// <summary>
    /// Default implementation of <see cref="ISessionStateSelector"/>.
    /// </summary>
    public class DefaultSessionStateSelector : ISessionStateSelector
    {
        readonly IEnvironmentAccessor _environmentAccessor;

        /// <summary>
        /// Default Constructor.
        /// Creates a new instance of <see cref="DefaultContextStateSelector"/> class.
        /// </summary>
        /// <param name="context">An instance of <see cref="IEnvironmentAccessor"/>.</param>
        public DefaultSessionStateSelector(IEnvironmentAccessor environmentAccessor)
        {
            _environmentAccessor = environmentAccessor;
        }

        /// <summary>
        /// Gets the implementation of <see cref="ISessionState"/> to use.
        /// </summary>
        /// <returns></returns>
        public ISessionState Get()
        {
            if (_environmentAccessor.IsHttpWebApplication)
            {
                return new HttpSessionState(_environmentAccessor.HttpContextAccessor);
            }
            else
            {
                return null;
            }
        }
    }
}