using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Harmonises the sampled gamut and then reduces it to a small, image-aware set
    /// of paint colours. The reduction happens after the floor stage so large source
    /// regions receive palette slots in proportion to their area.
    /// </summary>
    internal sealed class AbstractPaletteTransform : ICandidateTransform, IImageAwareCandidateTransform
    {
        private static readonly IReadOnlyList<StyleParameter> ParameterList = new[]
        {
            new StyleParameter("motherFraction", "Mother colour", 0.0, 0.6, 0.15, ""),
            new StyleParameter("colourCount", "Palette colours", 3.0, 12.0, 8.0, ""),
        };

        public string DisplayName => "Palette";

        public IReadOnlyList<StyleParameter> Parameters => ParameterList;

        public void Transform(MixtureBuilder builder, ParameterValues values)
        {
            builder.BlendInto(builder.MostNeutralPaintIndex(), values["motherFraction"]);
        }

        public CandidateSet Transform(
            CandidateSet candidates,
            int[] pixels,
            int strideInts,
            int width,
            int height,
            in RenderContext context,
            ParameterValues values)
        {
            int requested = Math.Clamp((int)Math.Round(values["colourCount"]), 3, 12);
            if (candidates.Argb.Length <= requested)
            {
                return candidates;
            }

            var samples = CollectSamples(pixels, strideInts, width, height);
            if (samples.Count == 0)
            {
                return candidates;
            }

            var centers = InitialiseCenters(samples, requested);
            for (int iteration = 0; iteration < 6; iteration++)
            {
                var sumL = new double[requested];
                var sumA = new double[requested];
                var sumB = new double[requested];
                var weights = new long[requested];

                foreach (Sample sample in samples)
                {
                    int nearest = NearestCenter(sample, centers);
                    weights[nearest] += sample.Weight;
                    sumL[nearest] += sample.L * sample.Weight;
                    sumA[nearest] += sample.A * sample.Weight;
                    sumB[nearest] += sample.B * sample.Weight;
                }

                for (int i = 0; i < requested; i++)
                {
                    if (weights[i] == 0)
                    {
                        continue;
                    }

                    centers[i] = new Sample(
                        sumL[i] / weights[i],
                        sumA[i] / weights[i],
                        sumB[i] / weights[i],
                        1);
                }
            }

            var selected = new HashSet<int>
            {
                LightestCandidate(candidates),
                DarkestCandidate(candidates),
            };
            foreach (Sample center in centers)
            {
                selected.Add(candidates.FindNearest(center.L, center.A, center.B));
            }

            // Centre collisions are expected when the requested palette is small or
            // the source is nearly monochrome. Fill remaining slots with candidates
            // nearest to evenly spaced source samples, keeping the result bounded.
            for (int i = 0; selected.Count < requested && i < samples.Count; i++)
            {
                selected.Add(candidates.FindNearest(samples[i].L, samples[i].A, samples[i].B));
            }

            for (int candidate = 0; selected.Count < requested && candidate < candidates.Argb.Length; candidate++)
            {
                selected.Add(candidate);
            }

            return candidates.Select(selected);
        }

        private static List<Sample> CollectSamples(int[] pixels, int strideInts, int width, int height)
        {
            var counts = new int[ColorQuantization.CacheSize];
            for (int y = 0; y < height; y++)
            {
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    counts[ColorQuantization.Key(pixels[row + x])]++;
                }
            }

            var samples = new List<Sample>();
            for (int key = 0; key < counts.Length; key++)
            {
                if (counts[key] == 0)
                {
                    continue;
                }

                ColorQuantization.KeyToRgb(key, out int r, out int g, out int b);
                PalettePhotoConverter.RgbToLab(r, g, b, out double l, out double a, out double bStar);
                samples.Add(new Sample(l, a, bStar, counts[key]));
            }

            samples.Sort((left, right) => left.L.CompareTo(right.L));
            return samples;
        }

        private static Sample[] InitialiseCenters(IReadOnlyList<Sample> samples, int count)
        {
            var centers = new Sample[count];
            for (int i = 0; i < count; i++)
            {
                int at = (int)((long)i * (samples.Count - 1) / Math.Max(count - 1, 1));
                centers[i] = samples[at];
            }

            return centers;
        }

        private static int NearestCenter(Sample sample, IReadOnlyList<Sample> centers)
        {
            int best = 0;
            double distance = double.MaxValue;
            for (int i = 0; i < centers.Count; i++)
            {
                double candidateDistance = Distance(sample, centers[i]);
                if (candidateDistance < distance)
                {
                    distance = candidateDistance;
                    best = i;
                }
            }

            return best;
        }

        private static double Distance(Sample left, Sample right)
        {
            double dl = left.L - right.L;
            double da = left.A - right.A;
            double db = left.B - right.B;
            return (1.5 * dl * dl) + (da * da) + (db * db);
        }

        private static int LightestCandidate(CandidateSet candidates)
        {
            int best = 0;
            for (int i = 1; i < candidates.L.Length; i++)
            {
                if (candidates.L[i] > candidates.L[best])
                {
                    best = i;
                }
            }

            return best;
        }

        private static int DarkestCandidate(CandidateSet candidates)
        {
            int best = 0;
            for (int i = 1; i < candidates.L.Length; i++)
            {
                if (candidates.L[i] < candidates.L[best])
                {
                    best = i;
                }
            }

            return best;
        }

        private readonly struct Sample
        {
            public Sample(double l, double a, double b, int weight)
            {
                L = l;
                A = a;
                B = b;
                Weight = weight;
            }

            public double L { get; }
            public double A { get; }
            public double B { get; }
            public int Weight { get; }
        }
    }
}
