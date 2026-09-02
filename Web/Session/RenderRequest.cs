using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Everything a render needs, snapshotted when it is scheduled. Immutable so a
/// request already in flight is unaffected when the user changes a slider or
/// loads another image; the generation is how a stale result is recognised.
/// </summary>
public sealed record RenderRequest(
    PixelImage Source,
    IReadOnlyList<PigmentCoefficients> Paints,
    StyleDefinition Style,
    int MarkPixels,
    IReadOnlyDictionary<IPipelineStage, ParameterValues> Values,
    long Generation,
    bool IsPreview);
