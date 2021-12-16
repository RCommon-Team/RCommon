

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Web;

namespace RCommon
{
    /// <summary>
    /// Default implementation of <see cref="IEnvironmentAccessor"/>
    /// </summary>
    public class EnvironmentAccessor : IEnvironmentAccessor
    {
        private readonly IServiceProvider _serviceProvider;
        private IHttpContextAccessor _httpContextAccessor;

        public EnvironmentAccessor(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }


        /// <summary>
        /// Gets weather the current application is a web based application.
        /// </summary>
        /// <value>True if the application is a web based application, else false.</value>
        public bool IsHttpWebApplication
        {
            get
            {
                _httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
                return HttpContextAccessor == null || HttpContextAccessor.HttpContext == null ? false : true;
            }
        }

        public IHttpContextAccessor HttpContextAccessor { get => _httpContextAccessor; }
    }
}
