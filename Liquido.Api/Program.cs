using Liquido.Api.Models;
using Liquido.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<LiquidRenderService>();

// Configure CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();

// Liquid rendering endpoint
app.MapPost("/api/render", async (RenderRequest request, LiquidRenderService renderService) =>
{
    var (success, result, error) = await renderService.RenderAsync(request.JsonData, request.LiquidTemplate);
    
    return new RenderResponse
    {
        Success = success,
        Result = result,
        Error = error
    };
})
.WithName("RenderLiquid")
.WithOpenApi();

app.Run();
