using System;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Maps a position on the colour wheel to the paints that make it and their shares.
    /// <para>
    /// This exists as one type because the wheel's pixels and the wheel's recipes have to
    /// be the same computation. When the generator worked out its wedges inline and the
    /// tooltip worked them out again separately, the two could drift, and a user could
    /// hover a colour and be handed a recipe that does not make it.
    /// </para>
    /// <para>
    /// The layout: each paint owns an anchor on the rim, evenly spaced clockwise from
    /// three o'clock. Between anchors the two flanking paints mix in proportion to the
    /// angle. Moving inward, weight shifts from that flanking pair to an equal share of
    /// every paint, so wedges are pure at the rim and reach the muddy all-paints blend at
    /// the centre.
    /// </para>
    /// </summary>
    public static class BlendGeometry
    {
        /// <summary>
        /// Works out which paints contribute at a point, and how much.
        /// </summary>
        /// <param name="diameter">The wheel's diameter in pixels.</param>
        /// <param name="paintCount">How many paints the wheel is built from.</param>
        /// <param name="x">The point's horizontal position in the wheel bitmap.</param>
        /// <param name="y">The point's vertical position in the wheel bitmap.</param>
        /// <param name="weights">The caller-owned buffer the shares are written into,
        /// length <paramref name="paintCount"/>. Left untouched when the point is
        /// outside the wheel.</param>
        /// <param name="alpha">How opaque the pixel is, fading over the final pixel of
        /// the radius so the rim is anti-aliased rather than stair-stepped.</param>
        /// <returns>True when the point lies inside the wheel.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="weights"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the buffer does not match
        /// <paramref name="paintCount"/>.</exception>
        public static bool TryGetWeights(
            int diameter, int paintCount, double x, double y, double[] weights, out double alpha)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }
            if (weights.Length != paintCount)
            {
                throw new ArgumentException(
                    "The weight buffer must have one slot per paint.", nameof(weights));
            }

            if (!TryGetWedge(diameter, paintCount, x, y, out Wedge wedge, out alpha))
            {
                return false;
            }

            for (int i = 0; i < paintCount; i++)
            {
                weights[i] = wedge.CentreShare;
            }

            weights[wedge.LowerPaint] += wedge.LowerSurplus;
            weights[wedge.UpperPaint] += wedge.UpperSurplus;

            return true;
        }

        /// <summary>
        /// Works out a point's wedge without expanding it to a weight per paint.
        /// <para>
        /// This is the primitive <see cref="TryGetWeights"/> is built on, and it exists
        /// because every paint receives the identical <see cref="Wedge.CentreShare"/>.
        /// A renderer that knows that can sum the whole palette's coefficients once and
        /// then add only the two flanking paints per pixel, which is the same arithmetic
        /// rearranged rather than an approximation of it.
        /// </para>
        /// </summary>
        /// <param name="diameter">The wheel's diameter in pixels.</param>
        /// <param name="paintCount">How many paints the wheel is built from.</param>
        /// <param name="x">The point's horizontal position in the wheel bitmap.</param>
        /// <param name="y">The point's vertical position in the wheel bitmap.</param>
        /// <param name="wedge">The point's flanking paints and their shares.</param>
        /// <param name="alpha">How opaque the pixel is, fading over the final pixel of
        /// the radius so the rim is anti-aliased rather than stair-stepped.</param>
        /// <returns>True when the point lies inside the wheel.</returns>
        public static bool TryGetWedge(
            int diameter, int paintCount, double x, double y, out Wedge wedge, out double alpha)
        {
            wedge = default;
            alpha = 0.0;
            if (paintCount < 1)
            {
                return false;
            }

            double centre = (diameter - 1) / 2.0;
            double radius = diameter / 2.0;
            double dx = x - centre;
            double dy = y - centre;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));

            alpha = Math.Clamp(radius - distance, 0.0, 1.0);
            if (alpha <= 0.0)
            {
                return false;
            }

            double angle = Math.Atan2(dy, dx);
            if (angle < 0.0)
            {
                angle += 2.0 * Math.PI;
            }

            double position = angle * paintCount / (2.0 * Math.PI);
            int lower = (int)position;
            double blend = position - lower;

            // Rounding can push an angle just below 2*pi up to a position of exactly
            // paintCount; that pixel belongs at the start of wedge zero.
            if (lower >= paintCount)
            {
                lower = 0;
                blend = 0.0;
            }

            int upper = (lower + 1) % paintCount;

            double rimShare = Math.Min(distance / radius, 1.0);
            wedge = new Wedge(
                lower,
                upper,
                rimShare * (1.0 - blend),
                rimShare * blend,
                (1.0 - rimShare) / paintCount);

            return true;
        }

        /// <summary>
        /// A point's position between two paint anchors: the equal share every paint
        /// holds, plus the extra the two flanking paints carry.
        /// </summary>
        public readonly struct Wedge
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Wedge"/> struct.
            /// </summary>
            /// <param name="lowerPaint">The index of the paint anticlockwise of the point.</param>
            /// <param name="upperPaint">The index of the paint clockwise of the point.</param>
            /// <param name="lowerSurplus">The lower paint's share above the centre share.</param>
            /// <param name="upperSurplus">The upper paint's share above the centre share.</param>
            /// <param name="centreShare">The share every paint holds, which is what the
            /// muddy all-paints centre is made of.</param>
            public Wedge(
                int lowerPaint, int upperPaint,
                double lowerSurplus, double upperSurplus, double centreShare)
            {
                LowerPaint = lowerPaint;
                UpperPaint = upperPaint;
                LowerSurplus = lowerSurplus;
                UpperSurplus = upperSurplus;
                CentreShare = centreShare;
            }

            /// <summary>Gets the index of the paint anticlockwise of the point.</summary>
            public int LowerPaint { get; }

            /// <summary>Gets the index of the paint clockwise of the point.</summary>
            public int UpperPaint { get; }

            /// <summary>Gets the lower paint's share above the centre share.</summary>
            public double LowerSurplus { get; }

            /// <summary>Gets the upper paint's share above the centre share.</summary>
            public double UpperSurplus { get; }

            /// <summary>Gets the share every paint holds.</summary>
            public double CentreShare { get; }
        }
    }
}
