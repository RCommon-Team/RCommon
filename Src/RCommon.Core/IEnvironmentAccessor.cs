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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Web;

namespace RCommon
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEnvironmentAccessor
    {
        /// <summary>
        /// Gets weather the current application is a web based application.
        /// </summary>
        /// <value>True if the application is a web based application, else false.</value>
        bool IsHttpWebApplication { get; }
        


        IHttpContextAccessor HttpContextAccessor { get; }
    }
}