using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Runs a photo through a <see cref="StyleDefinition"/>'s five stages in order,
    /// so the converter has one execution path shared by every style rather than a
    /// hard-coded sequence duplicated per style.
    /// <para>
    /// The order is fixed and matters: the candidate transform runs before the
    /// gamut is sampled, because it can only rewrite which mixtures get rendered,
    /// not which colours are chosen from them; the pre-map stages run before any
    /// colour is looked up, because they may depend on neighbouring pixels in ways
    /// a per-colour cache cannot represent; and the post-map stages see only
    /// candidate indices, never colours, which is what keeps them unable to
    /// synthesise a colour outside the achievable gamut.
    /// </para>
    /// </summary>
    internal static class StylePipeline
    {
        /// <summary>
        /// Renders a photo through one style: obtains that style's candidate set,
        /// filters and maps the pixel buffer through its stages in slot order, and
        /// writes the result into a fresh bitmap. A caller may supply a prepared set;
        /// otherwise this method builds one as before.
        /// </summary>
        /// <param name="source">The photo to convert; it is not modified.</param>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="style">The style whose stages govern this render.</param>
        /// <param name="markPixels">One brushmark's width in pixels, before the
        /// style's own <see cref="StyleDefinition.MarkScale"/> is applied. Zero or
        /// less derives it from the image's own dimensions.</param>
        /// <param name="values">Each of <paramref name="style"/>'s stages, mapped to
        /// the parameter values that stage should render with.</param>
        /// <param name="preparedCandidates">A palette-compatible set prepared by
        /// <see cref="PrepareCandidates"/>, or null to build it during this call.</param>
        /// <returns>A new 32-bit ARGB bitmap, or null when cancellation is observed
        /// during cooperative post-map processing.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>,
        /// <paramref name="paints"/>, <paramref name="style"/> or <paramref name="values"/>
        /// is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        internal static Bitmap Render(
            Bitmap source,
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            int markPixels,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CandidateSet preparedCandidates = null,
            CancellationToken cancellationToken = default,
            RenderDiagnostics diagnostics = null,
            ColourMapCache colourMapCache = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Render(
                SourceFrame.Create(source), paints, style, markPixels, values,
                preparedCandidates, cancellationToken, diagnostics, colourMapCache);
        }

        internal static Bitmap Render(
            SourceFrame source,
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            int markPixels,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CandidateSet preparedCandidates = null,
            CancellationToken cancellationToken = default,
            RenderDiagnostics diagnostics = null,
            ColourMapCache colourMapCache = null)
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
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            // The candidate transform can only narrow or reshape which mixtures get
            // sampled, so it has to run before Build() rather than after — there is
            // no way to remove a candidate once it has already become a colour.
            cancellationToken.ThrowIfCancellationRequested();
            long phaseStarted = diagnostics?.Begin() ?? 0L;
            CandidateSet candidates = preparedCandidates ?? PrepareCandidates(paints, style, values, cancellationToken);
            diagnostics?.End(preparedCandidates == null ? "Candidates: build" : "Candidates: reuse", phaseStarted);

            phaseStarted = diagnostics?.Begin() ?? 0L;
            double achievableMaxChroma = candidates.MaximumChroma;
            double[] achievableMaxChromaByHue = candidates.MaximumChromaByHue;
            diagnostics?.End("Candidates: metadata", phaseStarted);

            int width = source.Width;
            int height = source.Height;

            // The user's slider and the style's own scale are folded into one number
            // here, so every stage downstream sees only the product and can never
            // infer which style is asking by reading the two factors apart.
            double baseMark = markPixels > 0 ? markPixels : RenderContext.DefaultMarkPixels(width, height);
            var context = new RenderContext(
                width,
                height,
                baseMark * style.MarkScale,
                achievableMaxChroma,
                achievableMaxChromaByHue,
                cancellationToken);

            Bitmap result = null;
            phaseStarted = diagnostics?.Begin() ?? 0L;
            int[] pixels = source.CopyPixels();
            int strideInts = width;
            diagnostics?.End("Source: copy pixels", phaseStarted);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (IPreMapStage stage in style.PreMap)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    phaseStarted = diagnostics?.Begin() ?? 0L;
                    stage.Apply(pixels, strideInts, width, height, in context, values[stage]);
                    diagnostics?.End("Pre-map: " + stage.DisplayName, phaseStarted);
                }

                if (style.Candidates is IImageAwareCandidateTransform imageAwareCandidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    phaseStarted = diagnostics?.Begin() ?? 0L;
                    candidates = imageAwareCandidates.Transform(
                        candidates, pixels, strideInts, width, height, in context, values[style.Candidates]);
                    diagnostics?.End("Candidates: image-aware", phaseStarted);

                    phaseStarted = diagnostics?.Begin() ?? 0L;
                    achievableMaxChroma = candidates.MaximumChroma;
                    context = new RenderContext(
                        width,
                        height,
                        baseMark * style.MarkScale,
                        achievableMaxChroma,
                        candidates.MaximumChromaByHue,
                        cancellationToken);
                    diagnostics?.End("Candidates: image metadata", phaseStarted);
                }

                var indices = new int[strideInts * height];
                phaseStarted = diagnostics?.Begin() ?? 0L;
                if (style.Quantiser.IsPositionDependent)
                {
                    ResolvePerPixel(pixels, indices, strideInts, width, height, style, values, candidates, context);
                }
                else
                {
                    int[] resolved = colourMapCache?.GetOrCreate(candidates, style, values, in context);
                    ResolveOncePerColour(
                        pixels, indices, strideInts, width, height, style, values,
                        candidates, context, resolved);
                }
                diagnostics?.End("Mapping", phaseStarted);

                foreach (IPostMapStage stage in style.PostMap)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    phaseStarted = diagnostics?.Begin() ?? 0L;
                    stage.Refine(indices, strideInts, width, height, candidates, in context, values[stage]);
                    diagnostics?.End("Post-map: " + stage.DisplayName, phaseStarted);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                }

                phaseStarted = diagnostics?.Begin() ?? 0L;
                for (int y = 0; y < height; y++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    int row = y * strideInts;
                    for (int x = 0; x < width; x++)
                    {
                        int at = row + x;
                        pixels[at] = source.AlphaAt(at) | (candidates.Argb[indices[at]] & 0x00FFFFFF);
                    }
                }
                diagnostics?.End("Output: compose", phaseStarted);

                phaseStarted = diagnostics?.Begin() ?? 0L;
                result = source.CreateBitmap(pixels);
                diagnostics?.End("Output: write bitmap", phaseStarted);
            }
            catch
            {
                result?.Dispose();
                throw;
            }

            return result;
        }

        /// <summary>
        /// Samples and indexes the gamut produced by a style's palette transform.
        /// This is separated from <see cref="Render"/> because it depends only on the
        /// chosen paints and the transform's build parameters, not on the image.
        /// </summary>
        internal static CandidateSet PrepareCandidates(
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CancellationToken cancellationToken = default)
        {
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
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var builder = new MixtureBuilder(paints);
            style.Candidates.Transform(builder, values[style.Candidates]);
            cancellationToken.ThrowIfCancellationRequested();
            return builder.Build(cancellationToken);
        }

        /// <summary>
        /// Builds a fresh set of default parameter values for every stage a style
        /// uses, keyed by stage instance the way <see cref="Render"/> expects, with
        /// that style's own <see cref="StyleDefinition.DefaultOverrides"/> applied on
        /// top of each stage's declared defaults.
        /// </summary>
        /// <param name="style">The style whose stages need default values.</param>
        /// <returns>One <see cref="ParameterValues"/> per stage in
        /// <paramref name="style"/>, each starting at that stage's own declared
        /// defaults and then adjusted by whichever of this style's
        /// <see cref="StyleDefinition.DefaultOverrides"/> name that stage.</returns>
        internal static IReadOnlyDictionary<IPipelineStage, ParameterValues> DefaultValues(StyleDefinition style)
        {
            var values = new Dictionary<IPipelineStage, ParameterValues>();
            foreach (IPreMapStage stage in style.PreMap)
            {
                values[stage] = new ParameterValues(stage.Parameters);
            }

            values[style.Remap] = new ParameterValues(style.Remap.Parameters);
            values[style.Candidates] = new ParameterValues(style.Candidates.Parameters);
            values[style.Quantiser] = new ParameterValues(style.Quantiser.Parameters);

            foreach (IPostMapStage stage in style.PostMap)
            {
                values[stage] = new ParameterValues(stage.Parameters);
            }

            // Applied after every stage's own ParameterValues already exists, so a
            // style can retune a stage's starting point without that stage ever
            // storing a value it did not declare, and without two styles sharing a
            // stage type being able to influence one another's overrides.
            foreach (((IPipelineStage stage, string parameterId), double value) in style.DefaultOverrides)
            {
                values[stage].Set(parameterId, value);
            }

            return values;
        }

        /// <summary>
        /// Deep-copies every stage's values for one render. The outer dictionary and
        /// each <see cref="ParameterValues"/> are independent of the UI's live stores,
        /// so a worker observes one coherent frame even when another slider moves.
        /// </summary>
        internal static IReadOnlyDictionary<IPipelineStage, ParameterValues> SnapshotValues(
            StyleDefinition style,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values)
        {
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var snapshot = new Dictionary<IPipelineStage, ParameterValues>();
            foreach (IPreMapStage stage in style.PreMap)
            {
                snapshot[stage] = values[stage].Snapshot();
            }

            snapshot[style.Remap] = values[style.Remap].Snapshot();
            snapshot[style.Candidates] = values[style.Candidates].Snapshot();
            snapshot[style.Quantiser] = values[style.Quantiser].Snapshot();

            foreach (IPostMapStage stage in style.PostMap)
            {
                snapshot[stage] = values[stage].Snapshot();
            }

            return snapshot;
        }

        /// <summary>
        /// Resolves one candidate index per distinct quantized colour and fans that
        /// answer out to every pixel sharing it, which is sound only because a
        /// position-independent quantiser is defined to give the same answer to the
        /// same colour wherever it appears.
        /// </summary>
        /// <param name="pixels">The image's ARGB pixels, already through every pre-map stage.</param>
        /// <param name="indices">The candidate-index buffer to fill.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="style">The style supplying the remap and quantiser stages.</param>
        /// <param name="values">Parameter values for every stage in <paramref name="style"/>.</param>
        /// <param name="candidates">The achievable colours the quantiser chooses from.</param>
        /// <param name="context">This render's geometry.</param>
        private static void ResolveOncePerColour(
            int[] pixels, int[] indices, int strideInts, int width, int height,
            StyleDefinition style, IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CandidateSet candidates, RenderContext context,
            int[] resolved)
        {
            if (resolved == null)
            {
                resolved = new int[ColorQuantization.CacheSize];
                Array.Fill(resolved, -1);
            }

            // First pass: mark which quantized colours actually occur, so the remap
            // and quantiser run once per distinct colour instead of once per pixel.
            var used = new bool[ColorQuantization.CacheSize];
            for (int y = 0; y < height; y++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    used[ColorQuantization.Key(pixels[row + x])] = true;
                }
            }

            var keys = new List<int>();
            for (int key = 0; key < ColorQuantization.CacheSize; key++)
            {
                if ((key & 4095) == 0)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                }

                if (used[key])
                {
                    keys.Add(key);
                }
            }

            ParameterValues remapValues = values[style.Remap];
            ParameterValues quantiserValues = values[style.Quantiser];

            // Resolve every distinct colour in parallel; each entry is written by
            // exactly one iteration, so the shared array needs no locking.
            Parallel.For(0, keys.Count, new ParallelOptions
            {
                CancellationToken = context.CancellationToken,
            }, i =>
            {
                int key = keys[i];
                if (resolved[key] >= 0)
                {
                    return;
                }

                ColorQuantization.KeyToRgb(key, out int r, out int g, out int b);
                PalettePhotoConverter.RgbToLab(r, g, b, out double l, out double a, out double bStar);

                style.Remap.Map(l, a, bStar, out double mappedL, out double mappedA, out double mappedB, in context, remapValues);

                // Position is meaningless here: this branch only runs when the
                // quantiser has declared it does not need one.
                resolved[key] = style.Quantiser.Map(
                    mappedL, mappedA, mappedB, candidates, 0, 0, in context, quantiserValues);
            });

            // Second pass: fan the per-colour answer out to every pixel.
            for (int y = 0; y < height; y++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    indices[row + x] = resolved[ColorQuantization.Key(pixels[row + x])];
                }
            }
        }

        /// <summary>
        /// Resolves one candidate index per pixel independently, for a quantiser
        /// that declared its choice depends on where the pixel sits and so cannot
        /// be answered once per colour and reused.
        /// </summary>
        /// <param name="pixels">The image's ARGB pixels, already through every pre-map stage.</param>
        /// <param name="indices">The candidate-index buffer to fill.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="style">The style supplying the remap and quantiser stages.</param>
        /// <param name="values">Parameter values for every stage in <paramref name="style"/>.</param>
        /// <param name="candidates">The achievable colours the quantiser chooses from.</param>
        /// <param name="context">This render's geometry.</param>
        private static void ResolvePerPixel(
            int[] pixels, int[] indices, int strideInts, int width, int height,
            StyleDefinition style, IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CandidateSet candidates, RenderContext context)
        {
            ParameterValues remapValues = values[style.Remap];
            ParameterValues quantiserValues = values[style.Quantiser];

            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = context.CancellationToken,
            }, y =>
            {
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    int pixel = pixels[row + x];
                    PalettePhotoConverter.RgbToLab(
                        (pixel >> 16) & 0xFF, (pixel >> 8) & 0xFF, pixel & 0xFF,
                        out double l, out double a, out double b);

                    style.Remap.Map(l, a, b, out double mappedL, out double mappedA, out double mappedB, in context, remapValues);
                    indices[row + x] = style.Quantiser.Map(
                        mappedL, mappedA, mappedB, candidates, x, y, in context, quantiserValues);
                }
            });
        }

        /// <summary>
        /// Finds the largest chroma present in a candidate set.
        /// </summary>
        /// <param name="candidates">The achievable-gamut colours to scan.</param>
        /// <returns>The largest CIELAB C*ab among <paramref name="candidates"/>, or
        /// zero when it is empty.</returns>
        private static double MaximumChroma(
            CandidateSet candidates,
            CancellationToken cancellationToken = default)
        {
            double largest = 0.0;
            for (int i = 0; i < candidates.Argb.Length; i++)
            {
                if ((i & 4095) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                double chroma = Math.Sqrt((candidates.A[i] * candidates.A[i]) + (candidates.B[i] * candidates.B[i]));
                largest = Math.Max(largest, chroma);
            }

            return largest;
        }

        /// <summary>
        /// Finds the largest achievable chroma in each ten-degree hue sector. Empty
        /// sectors inherit the nearest populated sector so a sparse candidate set
        /// never makes the remap divide by a zero ceiling for an otherwise chromatic
        /// source colour.
        /// </summary>
        private static double[] MaximumChromaByHue(
            CandidateSet candidates,
            double fallback,
            CancellationToken cancellationToken = default)
        {
            var ceilings = new double[RenderContext.HueSectorCount];
            var populated = new bool[ceilings.Length];
            for (int i = 0; i < candidates.Argb.Length; i++)
            {
                if ((i & 4095) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                double chroma = Math.Sqrt((candidates.A[i] * candidates.A[i]) + (candidates.B[i] * candidates.B[i]));
                if (chroma <= 1e-9)
                {
                    continue;
                }

                double angle = Math.Atan2(candidates.B[i], candidates.A[i]) * (180.0 / Math.PI);
                if (angle < 0.0)
                {
                    angle += 360.0;
                }

                int sector = Math.Clamp((int)(angle / (360.0 / RenderContext.HueSectorCount)), 0, RenderContext.HueSectorCount - 1);
                ceilings[sector] = Math.Max(ceilings[sector], chroma);
                populated[sector] = true;
            }

            for (int sector = 0; sector < ceilings.Length; sector++)
            {
                if (populated[sector])
                {
                    continue;
                }

                int nearest = -1;
                int distance = int.MaxValue;
                for (int candidate = 0; candidate < ceilings.Length; candidate++)
                {
                    if (!populated[candidate])
                    {
                        continue;
                    }

                    int direct = Math.Abs(candidate - sector);
                    int circular = Math.Min(direct, ceilings.Length - direct);
                    if (circular < distance)
                    {
                        distance = circular;
                        nearest = candidate;
                    }
                }

                ceilings[sector] = nearest >= 0 ? ceilings[nearest] : fallback;
            }

            return ceilings;
        }
    }
}
