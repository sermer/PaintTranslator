using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The real renderer. Task.Run is kept even though single-threaded WebAssembly
/// executes it on the same thread: with WasmEnableThreads (configuration B) it
/// becomes a genuine background render with no other change.
/// </summary>
public sealed class PipelineRenderer : IFrameRenderer
{
    private readonly CandidateSetCache candidates;
    private readonly ColourMapCache colourMaps;

    public PipelineRenderer(CandidateSetCache candidates, ColourMapCache colourMaps)
    {
        this.candidates = candidates;
        this.colourMaps = colourMaps;
    }

    public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token) => Task.Run(() =>
    {
        CandidateSet set = candidates.GetOrCreate(request.Paints, request.Style, request.Values, token);
        if (set == null || token.IsCancellationRequested)
        {
            return null;
        }
        return StylePipeline.Render(
            request.Source, request.Paints, request.Style, request.MarkPixels,
            request.Values, set, token, colourMapCache: colourMaps);
    }, token);
}
