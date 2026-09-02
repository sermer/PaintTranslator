using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Web;
using PaintTranslator.Web.Interop;
using PaintTranslator.Web.Session;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IKeyValueStore, LocalStorageStore>();
builder.Services.AddSingleton<PaletteStore>();
builder.Services.AddSingleton(new CandidateSetCache());
builder.Services.AddSingleton(new ColourMapCache());
builder.Services.AddSingleton<IFrameRenderer, PipelineRenderer>();
builder.Services.AddSingleton<ConversionSession>();

// Top-level statements compile into an entry point with no declaration to hang
// CanvasInterop's [SupportedOSPlatform("browser")] on, and an assembly-level
// attribute was tried and rejected: it marks every public member this assembly
// exports as browser-only too, which broke Tests.Web (net10.0, no platform
// declared) calling ordinary session types like RenderScheduler. A single-line
// suppression at the one call site the entry point can't otherwise clear is
// narrower than either alternative.
#pragma warning disable CA1416 // This whole app only ever runs in the browser.
await CanvasInterop.ImportAsync();
#pragma warning restore CA1416
await builder.Build().RunAsync();
