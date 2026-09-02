using PaintTranslator.Imaging;

namespace PaintTranslator.Web.Session;

/// <summary>The pipeline behind the scheduler; a fake in tests, PipelineRenderer in the app.</summary>
public interface IFrameRenderer
{
    Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token);
}
