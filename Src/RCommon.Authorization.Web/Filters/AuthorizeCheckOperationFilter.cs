using Microsoft.AspNetCore.Authorization;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Authorization.Web.Filters
{
    /// <summary>
    /// A Swashbuckle <see cref="IOperationFilter"/> that adds 401/403 response codes and an OAuth2
    /// security requirement to every Swagger/OpenAPI operation decorated with <see cref="AuthorizeAttribute"/>.
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the given <paramref name="operation"/> by checking whether the
        /// controller or action method is decorated with <see cref="AuthorizeAttribute"/>.
        /// If so, 401 and 403 responses are added and an OAuth2 security requirement is attached.
        /// </summary>
        /// <param name="operation">The OpenAPI operation being processed.</param>
        /// <param name="context">The filter context containing method and controller metadata.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check for authorize attribute on the controller type or the action method itself.
            var hasAuthorize = (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false) ||
                               context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (!hasAuthorize) return;

            // Add standard authorization failure responses to the operation.
            operation.Responses?.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses?.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            // Attach an OAuth2 security requirement referencing the "api" scope.
#if NET10_0_OR_GREATER
            var oAuthScheme = new OpenApiSecuritySchemeReference("oauth2");

            operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [ oAuthScheme ] = new List<string> { "api" }
                    }
                };
#else
            var oAuthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [ oAuthScheme ] = new [] { "api" }
                    }
                };
#endif
        }
    }
}
