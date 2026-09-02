using System;
using System.Collections.Generic;
using PaintTranslator.Imaging.Styles.Stages;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// The fixed list of painting styles the application offers, and the lookup the
    /// style picker and the converter use to find one by name.
    /// </summary>
    public static class StyleRegistry
    {
        /// <summary>
        /// Gets every style the application offers, in the order they should be
        /// presented.
        /// </summary>
        public static IReadOnlyList<StyleDefinition> All { get; } = BuildAll();

        /// <summary>
        /// Builds every style the application offers.
        /// <para>
        /// A method rather than a single collection initializer, because Tonalism's
        /// stage instances need their own local names before
        /// <see cref="StyleDefinition.WithDefaults"/> can reference them — a plain
        /// object-initializer list can name a newly constructed stage only once, at
        /// the slot that holds it, with no way to hand that same instance to a
        /// second call afterward.
        /// </para>
        /// </summary>
        /// <returns>Every style the application offers, in presentation order.</returns>
        private static IReadOnlyList<StyleDefinition> BuildAll()
        {
            var realismFloor = new EdgePreservingFloor();
            var realism = new StyleDefinition(
                "Realism",
                1.0,
                new IPreMapStage[] { realismFloor },
                new IdentityRemap(),
                new KeepAllCandidates(),
                new NearestQuantiser(),
                Array.Empty<IPostMapStage>())
                // Realism benefits from a slightly wider edge-preserving gate while
                // retaining its fidelity-first identity/rematching contract. The
                // research found no support for a post-map merge or colour remap.
                .WithDefaults((realismFloor, "edge", 0.10));

            var tonalismFloor = new EdgePreservingFloor();
            var tonalismRemap = new ToneAndChromaRemap();

            // Left at the stages' own declared defaults, Tonalism would render
            // identically to Realism plus a slightly stronger floor — every one of
            // these overrides exists because the style should look different from
            // Realism the moment it is selected, not only once a user has found and
            // moved every slider themselves.
            var tonalism = new StyleDefinition(
                "Tonalism",
                1.2,
                new IPreMapStage[] { tonalismFloor },
                tonalismRemap,
                new KeepAllCandidates(),
                new NearestQuantiser(),
                new IPostMapStage[] { new SmallRegionMerge() })
                .WithDefaults(
                    (tonalismFloor, "strength", 2.0),
                    (tonalismFloor, "edge", 0.10),
                    (tonalismRemap, "contrast", 0.80),
                    (tonalismRemap, "key", -8.0),
                    (tonalismRemap, "chroma", 0.85));

            var fauvismFloor = new EdgePreservingFloor();
            var fauvismRemap = new ToneAndChromaRemap();

            // Fauvism declares no stage Realism and Tonalism do not already use — it
            // is ToneAndChromaRemap at different numbers, which is the point: the
            // stage that renders Tonalism's restraint renders Fauvism's excess just as
            // well, so the pipeline's claim that a stage generalises across styles is
            // demonstrated by this style rather than merely asserted by the earlier
            // two. Only contrast and chroma are overridden below; the floor's
            // "strength" and the remap's "key" already sit at the stage's own declared
            // defaults (1.0 and 0.0), so naming them again here would be a no-op
            // override, which is exactly what the convention above this style warns
            // against recording.
            var fauvism = new StyleDefinition(
                "Fauvism",
                1.3,
                new IPreMapStage[] { fauvismFloor },
                fauvismRemap,
                new KeepAllCandidates(),
                new NearestQuantiser(),
                new IPostMapStage[] { new ContourLines(), new SmallRegionMerge() })
                .WithDefaults(
                    (fauvismFloor, "strength", 3.0),
                    (fauvismRemap, "contrast", 0.95),
                    (fauvismRemap, "chroma", 1.8));

            var postImpressionismFloor = new EdgePreservingFloor();
            var postImpressionismRemap = new ToneAndChromaRemap();

            // Another reuse of the same two stages Tonalism and Fauvism already
            // demonstrate generalise: a stronger floor (strength 3, mark scale 1.6)
            // than either of those two, paired with the same ToneAndChromaRemap that
            // rendered Tonalism's restraint and Fauvism's excess, now pushed toward
            // flat planes of colour rather than a gradient — contrast 1.1 and chroma
            // 1.3 are both mild boosts, not Fauvism's extremes, because the flatness
            // here is meant to come from the floor and area opening, not from the remap.
            var postImpressionism = new StyleDefinition(
                "Post-Impressionism",
                1.6,
                new IPreMapStage[] { postImpressionismFloor },
                postImpressionismRemap,
                new KeepAllCandidates(),
                new NearestQuantiser(),
                new IPostMapStage[] { new SmallRegionMerge() })
                .WithDefaults(
                    (postImpressionismFloor, "strength", 3.0),
                    (postImpressionismRemap, "contrast", 1.0),
                    (postImpressionismRemap, "chroma", 1.45));

            var abstractFloor = new EdgePreservingFloor();
            var abstractRemap = new ToneAndChromaRemap();
            var abstractPalette = new AbstractPaletteTransform();

            // Abstract's structure comes from its restricted, image-aware palette,
            // not from pushing contrast and chroma farther than Post-Impressionism.
            // The floor remains strong enough to establish broad source regions, and
            // the palette transform harmonises those regions while keeping only a
            // small number of achievable paint colours.
            var abstractStyle = new StyleDefinition(
                "Abstract",
                2.5,
                new IPreMapStage[] { abstractFloor },
                abstractRemap,
                abstractPalette,
                new NearestQuantiser(),
                new IPostMapStage[] { new GroundFill(), new SmallRegionMerge() })
                .WithDefaults(
                    (abstractFloor, "strength", 5.0),
                    (abstractRemap, "contrast", 1.1),
                    (abstractRemap, "chroma", 1.0),
                    (abstractPalette, "motherFraction", 0.15),
                    (abstractPalette, "colourCount", 8.0));

            return new[] { realism, tonalism, fauvism, postImpressionism, abstractStyle };
        }

        /// <summary>
        /// Gets the style a freshly opened photo renders with.
        /// <para>
        /// Realism, because it is exactly what the converter did before styles
        /// existed: the mandatory floor, an untouched colour mapping and plain
        /// nearest-candidate matching. A user who never opens a style picker should
        /// see no difference from the single-path converter that predates it.
        /// </para>
        /// </summary>
        public static StyleDefinition Default => All[0];

        /// <summary>
        /// Finds a style by its declared name.
        /// </summary>
        /// <param name="name">The style's <see cref="StyleDefinition.Name"/>.</param>
        /// <returns>The matching style.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no style has that name.</exception>
        public static StyleDefinition ByName(string name)
        {
            foreach (StyleDefinition style in All)
            {
                if (string.Equals(style.Name, name, StringComparison.Ordinal))
                {
                    return style;
                }
            }

            throw new KeyNotFoundException($"No style named '{name}' is registered.");
        }
    }
}
