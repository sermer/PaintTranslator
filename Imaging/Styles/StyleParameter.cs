using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// One value a stage lets the user adjust, described well enough that the control
    /// panel can build a labelled slider for it without knowing what the stage does.
    /// </summary>
    /// <param name="Id">Stable identifier, unique within the declaring stage only.</param>
    /// <param name="Label">The caption shown beside the slider.</param>
    /// <param name="Minimum">The smallest value the slider offers.</param>
    /// <param name="Maximum">The largest value the slider offers.</param>
    /// <param name="Default">The value a freshly selected style starts at.</param>
    /// <param name="Unit">A short suffix for the readout, such as "px". Empty for a
    /// bare number.</param>
    internal sealed record StyleParameter(
        string Id, string Label, double Minimum, double Maximum, double Default, string Unit);

    /// <summary>
    /// One stage instance's parameter values.
    /// <para>
    /// Storage is per instance, never shared. Two stages may each declare a parameter
    /// called "strength" and neither needs to know the other exists — which is what
    /// keeps stages independent enough that adding one cannot disturb another. A
    /// single global parameter namespace would reintroduce exactly the coupling the
    /// pipeline is shaped to avoid.
    /// </para>
    /// </summary>
    internal sealed class ParameterValues
    {
        private readonly Dictionary<string, double> values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterValues"/> class, seeded
        /// with each parameter's declared default.
        /// </summary>
        /// <param name="parameters">The declaring stage's parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
        public ParameterValues(IReadOnlyList<StyleParameter> parameters)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            values = new Dictionary<string, double>(parameters.Count, StringComparer.Ordinal);
            ResetToDefaults();
        }

        /// <summary>Gets the declarations these values correspond to.</summary>
        public IReadOnlyList<StyleParameter> Parameters { get; }

        /// <summary>
        /// Gets one parameter's current value.
        /// </summary>
        /// <param name="id">The parameter's identifier.</param>
        /// <returns>The current value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the declaring stage has no
        /// such parameter, which is a programming error rather than a runtime
        /// condition — returning zero instead would surface as a slightly wrong
        /// picture and nothing else.</exception>
        public double this[string id] => values.TryGetValue(id, out double value)
            ? value
            : throw new KeyNotFoundException($"No parameter '{id}' is declared by this stage.");

        /// <summary>
        /// Sets one parameter, clamped to its declared range.
        /// </summary>
        /// <param name="id">The parameter's identifier.</param>
        /// <param name="value">The value to store.</param>
        /// <exception cref="KeyNotFoundException">Thrown when no such parameter is declared.</exception>
        public void Set(string id, double value)
        {
            StyleParameter declaration = Find(id);
            values[id] = Math.Clamp(value, declaration.Minimum, declaration.Maximum);
        }

        /// <summary>
        /// Copies the current values into an independent store. Rendering happens on a
        /// worker while the UI remains free to move sliders, so handing that worker the
        /// live dictionary would let one frame observe two different slider positions.
        /// </summary>
        /// <returns>A value-for-value copy that can be read safely while this instance changes.</returns>
        public ParameterValues Snapshot()
        {
            var snapshot = new ParameterValues(Parameters);
            foreach (StyleParameter parameter in Parameters)
            {
                snapshot.values[parameter.Id] = values[parameter.Id];
            }

            return snapshot;
        }

        /// <summary>
        /// Restores every parameter to the value its declaration names as the default.
        /// <para>
        /// Clamped the same way <see cref="Set"/> clamps, rather than assigned directly:
        /// a style whose declared default sits outside its own declared range (a
        /// definition error) would otherwise pass construction unclamped and only be
        /// caught the first time something called <see cref="Set"/>, which may be never.
        /// </para>
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (StyleParameter parameter in Parameters)
            {
                values[parameter.Id] = Math.Clamp(parameter.Default, parameter.Minimum, parameter.Maximum);
            }
        }

        /// <summary>
        /// Looks up a declaration by identifier.
        /// </summary>
        /// <param name="id">The parameter's identifier.</param>
        /// <returns>The matching declaration.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no such parameter is declared.</exception>
        private StyleParameter Find(string id)
        {
            foreach (StyleParameter parameter in Parameters)
            {
                if (string.Equals(parameter.Id, id, StringComparison.Ordinal))
                {
                    return parameter;
                }
            }

            throw new KeyNotFoundException($"No parameter '{id}' is declared by this stage.");
        }
    }
}
