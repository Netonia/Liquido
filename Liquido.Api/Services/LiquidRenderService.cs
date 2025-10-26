using Fluid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Liquido.Api.Services;

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
            var model = JToken.Parse(jsonData);

            // Parse Liquid template
            if (!_parser.TryParse(liquidTemplate, out var template, out var errors))
            {
                var errorMessages = string.Join(", ", errors);
                return (false, null, $"Liquid template error: {errorMessages}");
            }

            // Create template context
            var context = new TemplateContext(model, _options);

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
