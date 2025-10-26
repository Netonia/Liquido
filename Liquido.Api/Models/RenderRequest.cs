namespace Liquido.Api.Models;

public class RenderRequest
{
    public required string JsonData { get; set; }
    public required string LiquidTemplate { get; set; }
}
