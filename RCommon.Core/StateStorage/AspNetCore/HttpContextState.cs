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
using System.Diagnostics;
using System.Collections;
using RCommon.Extensions;
using System.Threading;
using Microsoft.AspNetCore.Http;
using RCommon.StateStorage;

namespace RCommon.StateStorage.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ILocalState"/> that stores and retrieves data from
    /// the current HttpContext.
    /// </summary>
    public class HttpContextState : IContextState
    {
        readonly Hashtable _state;
        IHttpContextAccessor _httpAccessor = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="httpContext">The <see cref="IHttpContextAccessor"/> that is injected which is the only thread
        /// safe way to manage objects </param>
        public HttpContextState(IHttpContextAccessor httpContext)
        {
            _httpAccessor = httpContext;
            /*_state = context.HttpContext.Items[typeof (HttpLocalState).FullName] as Hashtable;
            if (_state == null)
            {
                lock (context.HttpContext.Items)
                {
                    
                    context.HttpContext.Items[typeof(HttpLocalState).FullName] = (_state = new Hashtable());
                }
            }*/
            _httpAccessor.HttpContext.Items[typeof(HttpContextState).FullName] = (_state = new Hashtable());
        }

        /// <summary>
        /// Gets state data stored with the default key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <returns>An isntance of <typeparamref name="T"/> or null if not found.</returns>
        public T Get<T>()
        {
            return Get<T>(null);
        }

        /// <summary>
        /// Gets state data stored with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="key">An object representing the unique key with which the data was stored.</param>
        /// <returns>An instance of <typeparamref name="T"/> or null if not found.</returns>
        public T Get<T>(object key)
        {
            return (T)_state[key.BuildFullKey<T>()];
        }

        /// <summary>
        /// Puts state data into the local state with the default key.
        /// </summary>
        /// <typeparam name="T">The type of data to put.</typeparam>
        /// <param name="instance">An instance of <typeparamref name="T"/> to put.</param>
        public void Put<T>(T instance)
        {
            Put(null, instance);
        }

        /// <summary>
        /// Puts state data into the local state with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to put.</typeparam>
        /// <param name="key">An object representing the unique key with which the data is stored.</param>
        /// <param name="instance">An instance of <typeparamref name="T"/> to store.</param>
        public void Put<T>(object key, T instance)
        {

            //Debug.WriteLine(this.GetType().ToString() + ": Putting state data - " + key.ToString() + " - " + instance.ToString());
            _state[key.BuildFullKey<T>()] = instance;
        }

        /// <summary>
        /// Removes state data stored in the local state with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to remove.</typeparam>
        public void Remove<T>()
        {
            Remove<T>(null);
        }

        /// <summary>
        /// Removes state data stored in the local state with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to remove.</typeparam>
        /// <param name="key">An object representing the unique key with which the data was stored.</param>
        public void Remove<T>(object key)
        {
            //Debug.WriteLine(this.GetType().ToString() + ": Removing state data - " + key.ToString());
            _state.Remove(key.BuildFullKey<T>());
        }

        /// <summary>
        /// Clears all state stored in local state.
        /// </summary>
        public void Clear()
        {
            //Debug.WriteLine(this.GetType().ToString() + ": Clearing state data");
            _state.Clear();
        }
    }
}