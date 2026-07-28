using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Imaging.Styles.Stages;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Recreates a photo using only a given set of paints and their physical
    /// mixtures. The achievable gamut is sampled by blending the paints through
    /// the measured Kubelka-Munk kernel alone, in pairs along their whole mixing
    /// line, and in triples across their whole mixing triangle; each pixel is then
    /// replaced with the achievable color nearest to it in CIELAB space, so
    /// "closest" matches human perception rather than raw RGB distance.
    /// <para>
    /// The invariant is that every emitted pixel is a color the paints can genuinely
    /// be mixed to. The operative question for any new step is narrower than "does it
    /// run before the mapping": it is <em>can this synthesise a color outside the
    /// candidate set?</em> Operations before the mapping are always safe. So are
    /// operations after it that only ever <em>select</em> an existing candidate —
    /// modal filters, dithering, hard-edged fills, nearest-neighbour resampling.
    /// What breaks the invariant is post-mapping <em>arithmetic</em>: averaging two
    /// mapped pixels yields a color partway between two mixtures, which is not itself
    /// mixable. Re-running the mapping repairs that cheaply, since it is cached per
    /// distinct quantized color. An earlier version of this comment said only "blur
    /// before mapping", which forbids several operations that are in fact safe.
    /// </para>
    /// <para>
    /// A second invariant — every output <em>region</em> should be large enough for a
    /// brush to have made it — is what the mandatory pre-map floor every style
    /// pipeline includes exists to pursue, not a guarantee it delivers for every
    /// registered style. Mapping each pixel independently amplifies input noise —
    /// measured at 1.7x, and enough to put 44% of pixels into regions of four pixels
    /// or fewer on a photo with ordinary sensor noise — so an edge-preserving filter
    /// always runs before the mapping, whatever the caller passes for
    /// <c>blurRadius</c>, and it keeps every registered style far clear of that
    /// catastrophic case. But the floor's strength is one of five slider-adjustable
    /// parameters a style declares, and a style with a large <c>MarkScale</c> and no
    /// matching floor override can still land above a given fragmentation bar:
    /// measured on the sigma-3 noisy gradient at slider 0, Fauvism (floor strength at
    /// its stage's own weakest default) and Abstract (floor strength already at its
    /// parameter's maximum, outrun by <c>MarkScale</c> 2.5) both exceed 5% of pixels
    /// in sub-mark regions while the other three registered styles stay under 3%. See
    /// <c>StyleBehaviourTests.EveryRegisteredStyleIsPaintable</c>, which pins a
    /// baseline per style rather than one shared bar, <see cref="PaintabilityMetrics"/>,
    /// <see cref="GuidedFilter"/> and <see cref="Styles.Stages.EdgePreservingFloor"/>.
    /// </para>
    /// <para>
    /// The proportions are sampled as continuous shares rather than a few fixed
    /// ratios. Because the output is an 8-bit image and identical colors are
    /// collapsed below, a grid fine enough that refining it yields no further
    /// distinct colors is not an approximation of the achievable gamut — it is the
    /// achievable gamut, and picking the nearest member of it is then exactly the
    /// closest a mixture can get.
    /// </para>
    /// </summary>
    public static class PalettePhotoConverter
    {
        /// <summary>
        /// The window radius the mandatory pre-map filter runs at for a given mark size.
        /// </summary>
        /// <param name="markPixels">One brushmark's width in pixels.</param>
        /// <returns>The guided-filter radius, never below one.</returns>
        /// <remarks>
        /// Half a mark, because a filter window wider than the mark itself would erase
        /// features the mark is meant to be able to render, while a narrower one leaves
        /// noise the mapping then amplifies. Never zero: a radius of zero is the
        /// unfiltered case, which puts 44% of a noisy photograph's pixels into regions
        /// of four pixels or fewer.
        /// </remarks>
        internal static int FloorRadius(double markPixels)
        {
            return Math.Max((int)Math.Round(markPixels / 2.0), 1);
        }

        /// <summary>
        /// Converts a photo so every pixel uses only colors achievable by mixing
        /// the given paints, choosing the perceptually nearest achievable color
        /// for each pixel. Alpha is preserved from the source.
        /// </summary>
        /// <param name="source">The photo to convert; it is not modified.</param>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="blurRadius">The radius, in pixels, of an additional Gaussian
        /// blur to run after the mandatory pre-map floor, for a caller who wants more
        /// simplification than the floor alone provides. Zero adds no further blur —
        /// it does not mean an unfiltered pass, since the floor runs regardless of
        /// this value.</param>
        /// <param name="markPixels">One brushmark's width in pixels, which sets how
        /// strongly the mandatory pre-map filter runs. Zero or less derives it from the
        /// image's own dimensions.</param>
        /// <returns>A new 32-bit ARGB bitmap containing the converted photo.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="paints"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        /// <remarks>
        /// Delegates to the <see cref="StyleDefinition"/> overload with
        /// <see cref="StyleRegistry.Default"/>, so a caller with no style of its own to
        /// offer — every caller that predates the style picker — gets exactly the
        /// behaviour this converter always had, from the one place that behaviour is
        /// defined rather than a second copy of it kept in sync by hand.
        /// </remarks>
        public static Bitmap Convert(
            Bitmap source,
            IReadOnlyList<PigmentCoefficients> paints,
            int blurRadius = 0,
            int markPixels = 0)
        {
            return Convert(source, paints, StyleRegistry.Default, blurRadius, markPixels);
        }

        /// <summary>
        /// Converts a photo through a caller-chosen style, so every pixel uses only
        /// colors achievable by mixing the given paints, choosing the perceptually
        /// nearest achievable color for each pixel. Alpha is preserved from the source.
        /// </summary>
        /// <param name="source">The photo to convert; it is not modified.</param>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="style">The style whose stages define the mapping.</param>
        /// <param name="blurRadius">The radius, in pixels, of an additional Gaussian
        /// blur to run after the mandatory pre-map floor, for a caller who wants more
        /// simplification than the floor alone provides. Zero adds no further blur —
        /// it does not mean an unfiltered pass, since the floor runs regardless of
        /// this value.</param>
        /// <param name="markPixels">One brushmark's width in pixels, which sets how
        /// strongly the mandatory pre-map filter runs. Zero or less derives it from the
        /// image's own dimensions.</param>
        /// <returns>A new 32-bit ARGB bitmap containing the converted photo.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>,
        /// <paramref name="paints"/> or <paramref name="style"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        /// <remarks>
        /// Internal rather than public: <see cref="StyleDefinition"/> is itself internal,
        /// and a public method cannot expose an internal type through its signature. The
        /// style picker that supplies <paramref name="style"/> lives in <c>MainForm</c>,
        /// in the same assembly, so internal visibility is all it needs.
        /// <para>
        /// Delegates to <see cref="StylePipeline.Render"/> rather than running its own
        /// mapping, so behaviour is defined by <paramref name="style"/>'s stages alone
        /// and not a second implementation that could drift from them. The
        /// <paramref name="blurRadius"/> parameter predates the style pipeline and has
        /// no fixed slot of its own in <see cref="StyleDefinition"/>, so it is composed
        /// onto <paramref name="style"/> by <see cref="ComposeWithBlur"/> — the same
        /// helper <c>MainForm</c>'s convert button uses — rather than by logic kept
        /// here as well. This caller has no pre-existing values dictionary of its own,
        /// so it hands <see cref="ComposeWithBlur"/> a fresh
        /// <see cref="StylePipeline.DefaultValues"/> for <paramref name="style"/> to
        /// compose the blur stage's values onto.
        /// </para>
        /// </remarks>
        internal static Bitmap Convert(
            Bitmap source,
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            int blurRadius = 0,
            int markPixels = 0)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (paints.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paints));
            }
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            (StyleDefinition renderStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> values) =
                ComposeWithBlur(style, StylePipeline.DefaultValues(style), blurRadius);

            return StylePipeline.Render(source, paints, renderStyle, markPixels, values);
        }

        /// <summary>
        /// Composes a style and a values dictionary with an optional appended
        /// <see cref="Styles.Stages.OptionalBlur"/> pre-map stage, so this legacy
        /// <c>blurRadius</c> knob and a caller's own live parameter values can be
        /// combined without either caller re-deriving the composition itself.
        /// Shared by <see cref="Convert(Bitmap, IReadOnlyList{PigmentCoefficients}, StyleDefinition, int, int)"/>
        /// and <c>MainForm</c>'s convert button — the two places a
        /// <c>blurRadius</c> slider value meets a style's stages — so the blur
        /// stage's placement after the mandatory floor (see
        /// <see cref="Styles.Stages.OptionalBlur"/> for why the two do not
        /// commute) is defined once rather than kept in sync by hand between them.
        /// </summary>
        /// <param name="style">The style to render with. Not modified: appending
        /// the blur stage returns a new <see cref="StyleDefinition"/> rather than
        /// mutating this one, since <c>MainForm</c> hands in the very style
        /// instance it keeps reusing across conversions.</param>
        /// <param name="values">Each of <paramref name="style"/>'s stages, mapped
        /// to the parameter values it should render with. Not modified:
        /// <c>MainForm</c> keeps one dictionary per style for the lifetime of a
        /// session, so mutating it here would leave a blur-stage entry behind
        /// that accumulates — and leaks into a subsequent conversion — the next
        /// time this same dictionary was composed with a different (or zero)
        /// blur radius.</param>
        /// <param name="blurRadius">The radius, in pixels, of the blur to
        /// append, or zero (or less) to add none.</param>
        /// <returns><paramref name="style"/> and <paramref name="values"/>
        /// unchanged when <paramref name="blurRadius"/> is zero or less;
        /// otherwise a style with a fresh <see cref="Styles.Stages.OptionalBlur"/>
        /// appended to its <see cref="StyleDefinition.PreMap"/>, paired with a
        /// new dictionary holding a copy of <paramref name="values"/> plus that
        /// stage's own default-seeded entry with its radius set.</returns>
        internal static (StyleDefinition Style, IReadOnlyDictionary<IPipelineStage, ParameterValues> Values) ComposeWithBlur(
            StyleDefinition style,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            int blurRadius)
        {
            if (blurRadius <= 0)
            {
                return (style, values);
            }

            var blur = new OptionalBlur();
            StyleDefinition blurredStyle = style with { PreMap = AppendStage(style.PreMap, blur) };

            var blurValues = new ParameterValues(blur.Parameters);
            blurValues.Set("radius", blurRadius);

            var composedValues = new Dictionary<IPipelineStage, ParameterValues>(values) { [blur] = blurValues };

            return (blurredStyle, composedValues);
        }

        /// <summary>
        /// Copies a pre-map stage list with one more stage appended at the end.
        /// </summary>
        /// <param name="stages">The stages to copy, in order.</param>
        /// <param name="stage">The stage to append after all of <paramref name="stages"/>.</param>
        /// <returns>A new list holding <paramref name="stages"/> followed by <paramref name="stage"/>.</returns>
        private static IReadOnlyList<IPreMapStage> AppendStage(IReadOnlyList<IPreMapStage> stages, IPreMapStage stage)
        {
            var appended = new IPreMapStage[stages.Count + 1];
            for (int i = 0; i < stages.Count; i++)
            {
                appended[i] = stages[i];
            }

            appended[stages.Count] = stage;
            return appended;
        }

        /// <summary>
        /// Samples the gamut of colors achievable with the given paints: each paint
        /// alone, every pair across its whole mixing line, and every triple across its
        /// whole mixing triangle, all blended subtractively. Duplicate resulting colors
        /// are collapsed, which is what keeps the search set finite however finely the
        /// proportions are sampled.
        /// <para>
        /// Delegates to <see cref="MixtureBuilder"/>, which owns this enumeration so a
        /// style can apply the same two controlled mutations
        /// (<see cref="MixtureBuilder.BlendInto"/>, <see cref="MixtureBuilder.KeepOnly"/>)
        /// on top of it. An unmodified builder samples exactly what this method always
        /// has, which is what keeps a converted photo with no style applied unchanged.
        /// </para>
        /// </summary>
        /// <param name="paints">The available paints.</param>
        /// <returns>The deduplicated candidate colors, indexed for nearest-color search.</returns>
        private static CandidateSet BuildCandidates(IReadOnlyList<PigmentCoefficients> paints)
        {
            return new MixtureBuilder(paints).Build();
        }

        /// <summary>
        /// Lists the distinct colors the given paints can be mixed to, as this converter
        /// samples them. Exposed so a test can measure how closely the sampling covers
        /// the achievable gamut and can check the indexed search against an exhaustive
        /// one over the very same set.
        /// </summary>
        /// <param name="paints">The paints available for mixing.</param>
        /// <returns>The 32-bit ARGB value of every distinct achievable color.</returns>
        internal static int[] SampleAchievableColors(IReadOnlyList<PigmentCoefficients> paints)
        {
            return BuildCandidates(paints).Argb;
        }

        /// <summary>
        /// Maps colors through the same indexed nearest-candidate search a conversion
        /// uses, without going via a bitmap. Exposed for tests.
        /// </summary>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="targets">The colors to map, as 32-bit ARGB values.</param>
        /// <returns>The nearest achievable color to each target, index-aligned with
        /// <paramref name="targets"/>.</returns>
        internal static int[] MapThroughIndex(IReadOnlyList<PigmentCoefficients> paints, int[] targets)
        {
            CandidateSet candidates = BuildCandidates(paints);
            var mapped = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                mapped[i] = NearestCandidateArgb(candidates, ColorQuantization.Key(targets[i]));
            }

            return mapped;
        }

        /// <summary>
        /// Finds the candidate color perceptually nearest (squared CIELAB distance) to a
        /// quantized source color, by delegating to the grid-shell search
        /// <see cref="NearestQuantiser"/> runs for the style pipeline's own nearest-match
        /// stage — the same search rather than a second copy of it, so this surface and
        /// the pipeline's cannot silently disagree about which candidate is closest.
        /// </summary>
        /// <param name="candidates">The achievable-gamut colors to search.</param>
        /// <param name="cacheKey">The quantized-color cache key identifying the source color, from
        /// <see cref="ColorQuantization"/> — the one quantization scheme both this class and
        /// <see cref="StylePipeline"/> use, so the two can never disagree about which bin a
        /// color falls in.</param>
        /// <returns>The ARGB value of the nearest candidate.</returns>
        private static int NearestCandidateArgb(CandidateSet candidates, int cacheKey)
        {
            ColorQuantization.KeyToRgb(cacheKey, out int r, out int g, out int b);

            RgbToLab(r, g, b, out double targetL, out double targetA, out double targetB);

            int index = NearestQuantiser.NearestIndex(candidates, targetL, targetA, targetB);
            return candidates.Argb[index];
        }

        /// <summary>
        /// Converts an 8-bit sRGB color to CIELAB (D65 white point), the space in
        /// which Euclidean distance approximates perceived color difference.
        /// </summary>
        /// <param name="r">The sRGB red channel, 0 to 255.</param>
        /// <param name="g">The sRGB green channel, 0 to 255.</param>
        /// <param name="b">The sRGB blue channel, 0 to 255.</param>
        /// <param name="labL">The resulting L* component.</param>
        /// <param name="labA">The resulting a* component.</param>
        /// <param name="labB">The resulting b* component.</param>
        internal static void RgbToLab(int r, int g, int b, out double labL, out double labA, out double labB)
        {
            double rl = ColorSpace.SrgbToLinear(r / 255.0);
            double gl = ColorSpace.SrgbToLinear(g / 255.0);
            double bl = ColorSpace.SrgbToLinear(b / 255.0);

            ColorSpace.LinearRgbToXyz(rl, gl, bl, out double x, out double y, out double z);
            ColorSpace.XyzToLab(x, y, z, out labL, out labA, out labB);
        }
    }
}
