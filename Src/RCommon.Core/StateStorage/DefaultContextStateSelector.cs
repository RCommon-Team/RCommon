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
    /// Default implementation of <see cref="IContextStateSelector"/>.
    /// </summary>
    public class DefaultContextStateSelector : IContextStateSelector
    {
        readonly IEnvironmentAccessor _environment;

        /// <summary>
        /// Default Constructor.
        /// Creates an instance of <see cref="DefaultContextStateSelector"/> class.
        /// </summary>
        /// <param name="context">An instance of <see cref="IEnvironmentAccessor"/>.</param>
        public DefaultContextStateSelector(IEnvironmentAccessor environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Gets the <see cref="IContextState"/> instance to use.
        /// </summary>
        /// <returns></returns>
        public IContextState Get()
        {
            if (_environment.IsHttpWebApplication)
            {
                return new HttpContextState(_environment.HttpContextAccessor);
            }
            else
            {
                return new ThreadLocalState();
            }
        }
    }
}
