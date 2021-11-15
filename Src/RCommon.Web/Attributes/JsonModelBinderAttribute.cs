using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace RCommon.Web.Attributes
{
    public class JsonModelBinderAttribute : IModelBinder
    {
        //TODO: Modify this to IJsonSerializer
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
