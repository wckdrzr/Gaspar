using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WCKDRZR.Gaspar
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public partial class FromFormObjectAttribute : Attribute, IBinderTypeProviderMetadata
    {
        public Type BinderType => typeof(JsonModelBinder);
        public BindingSource BindingSource => BindingSource.Form;
    }

    //https://stackoverflow.com/a/46344854/404459
    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Check the value sent in
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

                // Attempt to convert the input value
                var valueAsString = valueProviderResult.FirstValue;
                if (valueAsString != null)
                {
                    JsonSerializerOptions options = new()
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    try
                    {
                        var result = JsonSerializer.Deserialize(valueAsString, bindingContext.ModelType, options);
                        if (result != null)
                        {
                            bindingContext.Result = ModelBindingResult.Success(result);
                            return Task.CompletedTask;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"FromFormObject: {e.Message}");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}