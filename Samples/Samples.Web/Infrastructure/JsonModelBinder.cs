using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Infrastructure
{
    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!bindingContext.HttpContext.Request.Query.ContainsKey(bindingContext.ModelName))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var json = bindingContext.HttpContext.Request.Query[bindingContext.ModelName][0];

            if (json == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(
                    JsonConvert.DeserializeObject(json, bindingContext.ModelType));
            }

            return Task.CompletedTask;
        }
    }
}
