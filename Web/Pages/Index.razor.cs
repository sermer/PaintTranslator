using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.Versioning;
using Microsoft.JSInterop;
using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Pages;

/// <summary>
/// Code-behind for the single real page. The markup only lays out the toolbar,
/// canvas column and sidebar; every stateful concern (file bytes reaching the
/// session, and the controller's headless diagnostics hook) lives here.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class Index
{
    private (Point? At, string[]? Lines) hover;
    private string? error;
    private string? lastTitle;
    private int hostWidth = 1000, hostHeight = 700;
    private DotNetObjectReference<Index>? self;

    protected override void OnInitialized() => Session.Changed += OnSessionChanged;

    // DynamicDependencyAttribute is only valid on a constructor, method or field, not
    // a class (see ImageCanvas.razor.cs from Task 9), so it is anchored here: JS calls
    // OnFileBytes by name, which a Release/AOT trim would otherwise remove since there
    // is no visible C# call site.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Index))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        self = DotNetObjectReference.Create(this);
        var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
        await module.InvokeVoidAsync("bindFileInputs", self);
        await AutoloadFileIfRequested(module);
    }

    [JSInvokable]
    public Task OnFileBytes(byte[] bytes, string name) => LoadBytes(bytes, name);

    private async Task LoadBytes(byte[] bytes, string name)
    {
        error = null;
        Session.BeginImageOperation();
        try
        {
            PixelImage photo = await ImageLoader.LoadAsync(bytes, name);
            Session.LoadPhoto(photo, name);
        }
        catch (ImageLoadException ex)
        {
            error = ex.Message;
            Console.WriteLine("HOST ERROR " + ex.Message);
        }
        catch (Exception ex)
        {
            // Anything other than a rejected format (a decode failure, an out-of-memory
            // on a huge file, and so on) would otherwise unwind this JS-invoked,
            // fire-and-forget task and vanish with no toast and no visible cause; the
            // raw exception still goes to the console for diagnosis.
            error = $"Could not open '{name}'.";
            Console.WriteLine("HOST ERROR " + error);
            Console.Error.WriteLine(ex);
        }
        finally { Session.EndImageOperation(); }
    }

    // The controller's headless verification path, like `/bench`: `?autofile=<path>`
    // fetches a sample from the site root and runs it through the same LoadBytes
    // funnel as a real open, giving an end-to-end bytes-to-session check with no
    // interactive input available in this environment.
    private async Task AutoloadFileIfRequested(IJSObjectReference module)
    {
        string? path = QueryParam("autofile");
        if (path == null) return;
        byte[] bytes = await module.InvokeAsync<byte[]>("fetchBytes", path);
        await LoadBytes(bytes, path);
    }

    private void OnSessionChanged()
    {
        // The controller drives this headless and greps stdout for title changes,
        // so only log on an actual change rather than every Changed tick (grid and
        // magnifier toggles raise Changed too).
        if (Session.Title != lastTitle)
        {
            lastTitle = Session.Title;
            Console.WriteLine("HOST TITLE " + Session.Title);
        }
        InvokeAsync(StateHasChanged);
    }

    private string? QueryParam(string name)
    {
        string query = new Uri(Nav.Uri).Query;
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == name)
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }
        return null;
    }

    public void Dispose()
    {
        Session.Changed -= OnSessionChanged;
        self?.Dispose();
    }
}
