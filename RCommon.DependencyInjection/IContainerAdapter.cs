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

using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon.DependencyInjection
{
    ///<summary>
    /// Base interface implemented by specific containers that allow registering components to an IoC container.
    ///</summary>
    public interface IContainerAdapter
    {
        void AddGeneric(Type service, Type implementation);
        void AddScoped(Type service, Func<IServiceProvider, object> implementationFactory);
        void AddScoped(Type service, Type implementation);
        void AddScoped<TService, TImplementation>() where TImplementation : TService;
        void AddScoped<TService>(Func<IServiceProvider, TService> implementationFactory);
        void AddSingleton(Type service, Func<IServiceProvider, object> implementationFactory);
        void AddSingleton(Type service, Type implementation);
        void AddSingleton<TService, TImplementation>() where TImplementation : TService;
        void AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory);
        void AddTransient(Type service, Func<IServiceProvider, object> implementationFactory);
        void AddTransient(Type service, Type implementation);
        void AddTransient<TService, TImplementation>() where TImplementation : TService;
        void AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory);

        IServiceCollection Services { get; }


    }
}