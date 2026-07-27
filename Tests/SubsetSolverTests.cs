using System;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the per-subset solver, which finds the best proportions of a given handful
    /// of paints for a target colour. Subset *choice* is exhaustive and needs no test
    /// beyond enumeration; what needs testing is that the proportions found within a
    /// subset are actually the best ones.
    /// </summary>
    public class SubsetSolverTests
    {
        /// <summary>
        /// Confirms a target that a subset can hit exactly is hit exactly. If the solver
        /// cannot recover a mixture it was given the ingredients for, it will not find
        /// good approximations either.
        /// </summary>
        [Fact]
        public void RecoversAMixtureItCanReproduceExactly()
        {
            var subset = new[] { Paint("Ultramarine Blue"), Paint("Titanium White") };
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(subset, new[] { 0.3, 0.7 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

            var shares = new double[subset.Length];
            double distance = SubsetSolver.Solve(subset, l, a, b, shares);

            Assert.True(distance < 0.5, $"distance was {distance:F3}");
            Assert.InRange(shares[0], 0.25, 0.35);
            Assert.InRange(shares[1], 0.65, 0.75);
        }

        /// <summary>
        /// Confirms the same for a three-paint subset, where the search space is a
        /// triangle rather than a line and a naive sweep is much likelier to stop short.
        /// </summary>
        [Fact]
        public void RecoversAThreePaintMixture()
        {
            var subset = new[]
            {
                Paint("Phthalo Blue (G.S.)"),
                Paint("Diarylide Yellow"),
                Paint("Titanium White"),
            };

            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(subset, new[] { 0.2, 0.3, 0.5 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

            var shares = new double[subset.Length];
            double distance = SubsetSolver.Solve(subset, l, a, b, shares);

            Assert.True(distance < 1.0, $"distance was {distance:F3}");
            Assert.InRange(shares.Sum(), 1.0 - 1e-9, 1.0 + 1e-9);
            Assert.All(shares, share => Assert.True(share >= 0.0));
        }

        /// <summary>
        /// Confirms a single-paint subset returns that paint at full concentration
        /// without pretending to optimise anything.
        /// </summary>
        [Fact]
        public void ASinglePaintSubsetIsFullConcentration()
        {
            var subset = new[] { Paint("Pyrrole Red") };
            var shares = new double[1];

            SubsetSolver.Solve(subset, 50.0, 60.0, 40.0, shares);

            Assert.Equal(1.0, shares[0]);
        }

        /// <summary>
        /// Confirms refinement genuinely improves on the coarse seed, which is what
        /// separates this from the ratio ladder it replaces.
        /// </summary>
        [Fact]
        public void RefinementImprovesOnTheCoarseSeed()
        {
            var subset = new[] { Paint("Quinacridone Magenta"), Paint("Hansa Yellow Opaque") };
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(subset, new[] { 0.37, 0.63 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

            var shares = new double[subset.Length];
            double refined = SubsetSolver.Solve(subset, l, a, b, shares);

            // The coarse seed can only land on tenths, and 0.37 is not one.
            var coarse = new[] { 0.4, 0.6 };
            KubelkaMunk.Mix(subset, coarse, reflectance);
            SpectralRenderer.ToLab(reflectance, out double cl, out double ca, out double cb);
            double coarseDistance = PaintBlendMatcher.PerceptualDistance(l, a, b, cl, ca, cb);

            Assert.True(refined <= coarseDistance + 1e-9,
                $"refined {refined:F4} was worse than the coarse {coarseDistance:F4}");
        }

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }
    }
}
