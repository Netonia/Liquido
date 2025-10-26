using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Liquido;
using Liquido.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register the client-side Liquid rendering service
builder.Services.AddSingleton<ClientLiquidRenderService>();

await builder.Build().RunAsync();
