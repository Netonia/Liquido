using Fluid;
using Newtonsoft.Json.Linq;

namespace Liquido.Services;

public class LiquidRenderService
{
    private readonly FluidParser _parser;
    private readonly TemplateOptions _options;

    public LiquidRenderService()
    {
        _parser = new FluidParser();
        _options = new TemplateOptions();
        _options.MemberAccessStrategy.Register<JObject>();
        _options.MemberAccessStrategy.Register<JValue>(o => o.Value);
        _options.MemberAccessStrategy.Register<JArray>();
    }

    public async Task<(bool success, string? result, string? error)> RenderAsync(string jsonData, string liquidTemplate)
    {
        try
        {
            // Parse JSON
            JToken jsonToken;
            try
            {
                jsonToken = JToken.Parse(jsonData);
            }
            catch (Exception ex)
            {
                return (false, null, $"Invalid JSON: {ex.Message}");
            }

            // Parse Liquid template
            if (!_parser.TryParse(liquidTemplate, out var template, out var errors))
            {
                var errorMessages = string.Join(", ", errors);
                return (false, null, $"Liquid template error: {errorMessages}");
            }

            // Create template context
            var context = new TemplateContext(_options);
            
            // If the JSON is an array, set it as 'model' variable
            // If it's an object, set the object as the model and also expose properties as root variables
            if (jsonToken is JArray jArray)
            {
                context.SetValue("model", jArray);
            }
            else if (jsonToken is JObject jObject)
            {
                // Set the entire object as model
                context.SetValue("model", jObject);
                
                // Also expose each property as a root variable for convenience
                foreach (var property in jObject.Properties())
                {
                    context.SetValue(property.Name, property.Value);
                }
            }
            else
            {
                context.SetValue("model", jsonToken);
            }

            // Render the template
            var result = await template.RenderAsync(context);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Rendering error: {ex.Message}");
        }
    }
}
