using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Chooses the candidate whose CIELAB coordinates are nearest the target colour
    /// by squared Euclidean distance — plain nearest-neighbour matching, with no
    /// stylistic bias toward any region of the gamut.
    /// <para>
    /// <see cref="NearestIndex"/> is the grid-shell search
    /// <see cref="PalettePhotoConverter"/> used for its own nearest-candidate lookup
    /// before this pipeline existed, relocated here rather than duplicated:
    /// <see cref="PalettePhotoConverter"/> now calls this same method for the
    /// callers it still owns, so there is exactly one implementation of "which
    /// candidate is closest" behind both surfaces.
    /// </para>
    /// </summary>
    internal sealed class NearestQuantiser : IQuantiser
    {
        /// <summary>Gets "Matching", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Matching";

        /// <summary>Gets the empty parameter list: plain nearest matching has nothing to tune.</summary>
        public IReadOnlyList<StyleParameter> Parameters { get; } = Array.Empty<StyleParameter>();

        /// <summary>
        /// Gets <see langword="false"/>: which candidate is nearest a colour never
        /// depends on where that colour sits in the image, so the pipeline may
        /// resolve one answer per distinct colour and reuse it across every pixel
        /// that shares it.
        /// </summary>
        public bool IsPositionDependent => false;

        /// <summary>
        /// Picks the candidate nearest the target colour. Position, and this
        /// stage's own (empty) parameter values, are both irrelevant to the answer.
        /// </summary>
        /// <param name="l">The target L*.</param>
        /// <param name="a">The target a*.</param>
        /// <param name="b">The target b*.</param>
        /// <param name="candidates">The achievable colours to choose from.</param>
        /// <param name="x">Unused, since <see cref="IsPositionDependent"/> is <see langword="false"/>.</param>
        /// <param name="y">Unused, for the same reason.</param>
        /// <param name="context">Unused; the search needs only the candidates and the target colour.</param>
        /// <param name="values">Unused; this stage declares no parameters.</param>
        /// <returns>The chosen candidate's index.</returns>
        public int Map(
            double l, double a, double b, CandidateSet candidates,
            int x, int y, in RenderContext context, ParameterValues values)
        {
            return NearestIndex(candidates, l, a, b);
        }

        /// <summary>
        /// Finds the candidate perceptually nearest (squared CIELAB distance) to a
        /// target colour, by walking <paramref name="candidates"/>'s grid index
        /// outward from the target's own cell in cubic shells and stopping once no
        /// unexamined cell could hold anything closer.
        /// </summary>
        /// <param name="candidates">The achievable-gamut colours to search.</param>
        /// <param name="targetL">The target L*.</param>
        /// <param name="targetA">The target a*.</param>
        /// <param name="targetB">The target b*.</param>
        /// <returns>The nearest candidate's index.</returns>
        internal static int NearestIndex(CandidateSet candidates, double targetL, double targetA, double targetB)
        {
            double[] candL = candidates.L;
            double[] candA = candidates.A;
            double[] candB = candidates.B;
            int[] cellStart = candidates.CellStart;
            int[] members = candidates.Members;
            int perAxis = candidates.CellsPerAxis;

            int homeL = candidates.AxisCell(targetL, candidates.MinL, candidates.CellL);
            int homeA = candidates.AxisCell(targetA, candidates.MinA, candidates.CellA);
            int homeB = candidates.AxisCell(targetB, candidates.MinB, candidates.CellB);

            double bestDistance = double.MaxValue;
            int bestIndex = 0;

            // Scans one cell's members. A local function so the shell walk below can stay
            // about which cells to visit rather than repeating the distance test at each
            // of the places it decides to visit one.
            void Examine(int cell)
            {
                int end = cellStart[cell + 1];
                for (int slot = cellStart[cell]; slot < end; slot++)
                {
                    int i = members[slot];
                    double dl = candL[i] - targetL;
                    double da = candA[i] - targetA;
                    double db = candB[i] - targetB;
                    double distance = (dl * dl) + (da * da) + (db * db);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }
            }

            for (int shell = 0; shell < perAxis; shell++)
            {
                // The target sits somewhere inside its own cell, so a cell this many
                // steps away has its nearest face at least one step less than that.
                // Once even that lower bound beats nothing, no further shell can.
                if (shell > 0)
                {
                    double reach = (shell - 1) * candidates.SmallestCell;
                    if (reach > 0.0 && reach * reach >= bestDistance)
                    {
                        break;
                    }
                }

                int lowL = Math.Max(homeL - shell, 0);
                int highL = Math.Min(homeL + shell, perAxis - 1);
                int lowA = Math.Max(homeA - shell, 0);
                int highA = Math.Min(homeA + shell, perAxis - 1);
                int lowB = Math.Max(homeB - shell, 0);
                int highB = Math.Min(homeB + shell, perAxis - 1);

                for (int cellL = lowL; cellL <= highL; cellL++)
                {
                    bool edgeL = Math.Abs(cellL - homeL) == shell;
                    for (int cellA = lowA; cellA <= highA; cellA++)
                    {
                        bool edgeA = Math.Abs(cellA - homeA) == shell;

                        // Only the cube's surface is new; its interior belongs to shells
                        // already walked. When neither of the first two axes is on the
                        // surface the third has to be, so that row reduces to its two
                        // end cells — visited explicitly, because clamping at the grid's
                        // border makes striding the row unsound.
                        if (edgeL || edgeA)
                        {
                            for (int cellB = lowB; cellB <= highB; cellB++)
                            {
                                Examine(candidates.Flatten(cellL, cellA, cellB));
                            }

                            continue;
                        }

                        if (homeB - shell >= 0)
                        {
                            Examine(candidates.Flatten(cellL, cellA, homeB - shell));
                        }
                        if (shell > 0 && homeB + shell < perAxis)
                        {
                            Examine(candidates.Flatten(cellL, cellA, homeB + shell));
                        }
                    }
                }
            }

            return bestIndex;
        }
    }
}
