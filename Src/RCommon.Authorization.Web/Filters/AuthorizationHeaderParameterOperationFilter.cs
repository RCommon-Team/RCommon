using Microsoft.AspNetCore.Mvc.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Authorization.Web.Filters
{
    /// <summary>
    /// A Swashbuckle <see cref="IOperationFilter"/> that adds a required "Authorization" header parameter
    /// to every Swagger/OpenAPI operation whose action pipeline includes an <see cref="AuthorizeFilter"/>
    /// and does not allow anonymous access.
    /// </summary>
    public class AuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the given <paramref name="operation"/> by inspecting the action's
        /// filter pipeline for authorization requirements.
        /// </summary>
        /// <param name="operation">The OpenAPI operation being processed.</param>
        /// <param name="context">The filter context containing API description metadata.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;

            // Determine whether the action has authorization enforced and whether anonymous access is permitted.
            var isAuthorized = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is AuthorizeFilter);
            var allowAnonymous = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is IAllowAnonymousFilter);

            if (isAuthorized && !allowAnonymous)
            {
#if NET10_0_OR_GREATER
                operation.Parameters ??= [];
#else
                if (operation.Parameters == null)
                    operation.Parameters = new List<OpenApiParameter>();
#endif

                // Add a required Authorization header so that Swagger UI prompts for a token.
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description = "access token",
                    Required = true
                });
            }
        }
    }
}
