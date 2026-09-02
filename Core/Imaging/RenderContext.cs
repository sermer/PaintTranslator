using System;
using System.Threading;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Everything about one particular render that a pipeline stage may need and that
    /// is not the stage's own setting: the image's geometry, how large one brushmark
    /// is in pixels, and how much chroma the chosen paints can actually reach.
    /// <para>
    /// Deliberately carries no reference to the style being rendered, not even its
    /// name. A stage that could ask which style invoked it would grow a branch per
    /// style, and tuning one style would then change the others — which is the single
    /// failure mode this pipeline is shaped to prevent. <see cref="MarkPixels"/> is
    /// the user's slider already multiplied by the style's scale factor, so a stage
    /// sees the product and never the two factors.
    /// </para>
    /// </summary>
    public readonly struct RenderContext
    {
        /// <summary>
        /// How many marks span the short edge of a canvas at the default. A detailed
        /// painting occupies roughly this range; going much finer produces marks no
        /// brush would make, and much coarser loses the subject.
        /// </summary>
        private const double MarksAcrossShortEdge = 150.0;

        /// <summary>The smallest mark the default will produce.</summary>
        /// <remarks>
        /// One pixel is exactly the unpaintable case mark size exists to rule out, so
        /// the floor is two.
        /// </remarks>
        private const int SmallestMark = 2;

        /// <summary>The largest mark the default will produce.</summary>
        /// <remarks>
        /// An enormous scan should not default to a mark so large the picture
        /// collapses into a handful of blocks. 128 pixels is the ceiling that
        /// stops this while leaving room for high-resolution captures.
        /// </remarks>
        private const int LargestMark = 128;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext"/> struct.
        /// </summary>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="markPixels">One brushmark's width in pixels, already scaled by
        /// the active style.</param>
        /// <param name="achievableMaxChroma">The largest CIELAB C*ab in the candidate
        /// set for this palette.</param>
        /// <param name="cancellationToken">The signal that this render has been
        /// superseded and should stop.</param>
        public RenderContext(
            int width,
            int height,
            double markPixels,
            double achievableMaxChroma,
            CancellationToken cancellationToken = default)
        {
            Width = width;
            Height = height;
            MarkPixels = markPixels;
            AchievableMaxChroma = achievableMaxChroma;
            CancellationToken = cancellationToken;
        }

        /// <summary>Gets the image width in pixels.</summary>
        public int Width { get; }

        /// <summary>Gets the image height in pixels.</summary>
        public int Height { get; }

        /// <summary>
        /// Gets one brushmark's width in pixels: the user's slider value multiplied by
        /// the active style's scale factor.
        /// </summary>
        public double MarkPixels { get; }

        /// <summary>
        /// Gets the largest CIELAB C*ab present in this render's candidate set.
        /// <para>
        /// Chroma transforms need a ceiling the paints can actually reach. Median
        /// masstone chroma across the library is 33.6 and the best blue is 70.7, so a
        /// transform that boosts toward an abstract maximum would send most of the
        /// image at a chroma no mixture has — many distinct colours would then land on
        /// the same few boundary candidates and the result bands rather than
        /// saturating.
        /// </para>
        /// </summary>
        public double AchievableMaxChroma { get; }

        /// <summary>
        /// Gets the signal that an interactive render has been superseded. Stages
        /// poll it at row or region boundaries so abandoned full-size work releases
        /// the render slot promptly.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Computes the mark size to start an image at, before any user adjustment.
        /// </summary>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <returns>One brushmark's width in pixels, between 2 and 128.</returns>
        public static int DefaultMarkPixels(int width, int height)
        {
            double marks = Math.Min(width, height) / MarksAcrossShortEdge;

            return Math.Clamp((int)Math.Round(marks), SmallestMark, LargestMark);
        }
    }
}
