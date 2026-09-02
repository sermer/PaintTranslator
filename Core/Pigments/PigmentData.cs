using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// The on-disk coefficient format, read and written here so the offline ingest tool
    /// and the runtime loader cannot drift apart.
    /// <para>
    /// Raw IEEE-754 doubles rather than generated source: byte-exact with no rounding
    /// decision to make, about 32KB, and deserialised in well under a millisecond. The
    /// reviewability a binary loses is supplied instead by the manifest the ingest emits
    /// alongside it.
    /// </para>
    /// </summary>
    public static class PigmentData
    {
        /// <summary>Identifies the file and catches a wrong resource being loaded.</summary>
        private static readonly byte[] Magic = { (byte)'P', (byte)'G', (byte)'M', (byte)'T' };

        /// <summary>The format version, bumped whenever the layout changes.</summary>
        private const int FormatVersion = 1;

        /// <summary>
        /// Writes paints to a stream.
        /// </summary>
        /// <param name="stream">The destination stream, left open.</param>
        /// <param name="paints">The paints to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
        public static void Write(Stream stream, IReadOnlyList<PigmentCoefficients> paints)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(SpectralBands.StartWavelengthNm);
            writer.Write(SpectralBands.WavelengthIntervalNm);
            writer.Write(SpectralBands.Count);
            writer.Write(paints.Count);

            foreach (PigmentCoefficients paint in paints)
            {
                writer.Write(paint.Name);
                writer.Write(paint.ColourIndex);
                writer.Write((byte)paint.Provenance);

                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    writer.Write(paint.Absorption[band]);
                }

                // A reflectance-derived paint's scattering is 1 at every band by
                // construction, so writing 59 identical copies of it would be a third of
                // the file for no information.
                if (paint.Provenance == PigmentProvenance.TwoConstantMeasured)
                {
                    for (int band = 0; band < SpectralBands.Count; band++)
                    {
                        writer.Write(paint.Scattering[band]);
                    }
                }
            }
        }

        /// <summary>
        /// Reads paints from a stream.
        /// </summary>
        /// <param name="stream">The source stream, left open.</param>
        /// <returns>The paints, in the order they were written.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the header does not match
        /// what this build expects.</exception>
        public static IReadOnlyList<PigmentCoefficients> Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            byte[] magic = reader.ReadBytes(Magic.Length);
            for (int i = 0; i < Magic.Length; i++)
            {
                if (magic.Length != Magic.Length || magic[i] != Magic[i])
                {
                    throw new InvalidDataException("Not a pigment coefficient file.");
                }
            }

            int version = reader.ReadInt32();
            if (version != FormatVersion)
            {
                throw new InvalidDataException(
                    $"Pigment data is format version {version}; this build reads {FormatVersion}.");
            }

            int start = reader.ReadInt32();
            int interval = reader.ReadInt32();
            int bandCount = reader.ReadInt32();
            if (start != SpectralBands.StartWavelengthNm
                || interval != SpectralBands.WavelengthIntervalNm
                || bandCount != SpectralBands.Count)
            {
                throw new InvalidDataException(
                    $"Pigment data is {bandCount} bands from {start}nm every {interval}nm; " +
                    $"this build uses {SpectralBands.Count} from {SpectralBands.StartWavelengthNm}nm " +
                    $"every {SpectralBands.WavelengthIntervalNm}nm.");
            }

            int paintCount = reader.ReadInt32();
            var paints = new List<PigmentCoefficients>(paintCount);
            for (int i = 0; i < paintCount; i++)
            {
                string name = reader.ReadString();
                string colourIndex = reader.ReadString();
                var provenance = (PigmentProvenance)reader.ReadByte();

                var absorption = new double[SpectralBands.Count];
                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    absorption[band] = reader.ReadDouble();
                }

                double[] scattering = null;
                if (provenance == PigmentProvenance.TwoConstantMeasured)
                {
                    scattering = new double[SpectralBands.Count];
                    for (int band = 0; band < SpectralBands.Count; band++)
                    {
                        scattering[band] = reader.ReadDouble();
                    }
                }

                paints.Add(new PigmentCoefficients(name, colourIndex, provenance, absorption, scattering));
            }

            return paints;
        }
    }
}
