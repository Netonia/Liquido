using Fluid;
using System.Text.Json;

namespace Liquido.Api.Services;

public class LiquidRenderService
{
    private readonly FluidParser _parser;

    public LiquidRenderService()
    {
        _parser = new FluidParser();
    }

    public async Task<(bool success, string? result, string? error)> RenderAsync(string jsonData, string liquidTemplate)
    {
        try
        {
            // Parse JSON to object
            object? model;
            try
            {
                model = JsonSerializer.Deserialize<object>(jsonData);
            }
            catch (JsonException ex)
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
            var context = new TemplateContext(model);

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
