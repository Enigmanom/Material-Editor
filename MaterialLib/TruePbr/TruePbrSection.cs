using System.Runtime.Serialization;

namespace MaterialLib
{
    [DataContract]
    public sealed class TruePbrSection
    {
        public const string BlockTag = "CSPB";

        public TruePbrSection()
        {
            SetDefaults();
        }

        [OnDeserializing]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by JSON serialization.")]
        private void OnDeserializing(StreamingContext c)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            Layout = TruePbrSchema.Layout;
            Common = new TruePbrCommon();
            Textures = new string[TruePbrSchema.Textures.Count];
            Array.Fill(Textures, "");
            PayloadVersion = TruePbrSchema.PayloadVersion;
            FeatureFlags = 0;
            Parameters = new float[TruePbrSchema.ParameterCount];
            foreach (var parameter in TruePbrSchema.Parameters)
                Array.Fill(Parameters, parameter.Default, parameter.Index, parameter.Count);
        }

        #region Properties
        [DataMember(Name = "layout", Order = 1)]
        public uint Layout { get; set; }

        [DataMember(Name = "common", Order = 2)]
        public TruePbrCommon Common { get; set; } = new();

        public string[] Textures { get; private set; } = [];

        [DataMember(Name = "payloadVersion", Order = 4)]
        public uint PayloadVersion { get; set; }

        public uint FeatureFlags { get; set; }

        public float[] Parameters { get; private set; } = [];

        [DataMember(Name = "thinSurfaceOverride", Order = 7)]
        public TruePbrThinSurfaceOverride? ThinSurfaceOverride { get; set; }

        [DataMember(Name = "fuzzOverride", Order = 8)]
        public TruePbrFuzzOverride? FuzzOverride { get; set; }

        [DataMember(Name = "textures", Order = 3)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by JSON serialization.")]
        private TruePbrNamedValues TexturesData
        {
            get
            {
                var values = new TruePbrNamedValues();
                foreach (var slot in TruePbrSchema.Textures)
                    values.Set(slot.Name, Textures[slot.Index]);
                return values;
            }
            set
            {
                foreach (var slot in TruePbrSchema.Textures)
                {
                    if (value.TryGetString(slot.Name, out var path))
                        Textures[slot.Index] = path;
                }
            }
        }

        [DataMember(Name = "features", Order = 5)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by JSON serialization.")]
        private TruePbrNamedValues FeaturesData
        {
            get
            {
                var values = new TruePbrNamedValues();
                foreach (var feature in TruePbrSchema.Features)
                    values.Set(feature.Name, HasFeature(feature));
                return values;
            }
            set
            {
                foreach (var feature in TruePbrSchema.Features)
                {
                    if (value.TryGetBool(feature.Name, out var enabled))
                        SetFeature(feature, enabled);
                }
            }
        }

        [DataMember(Name = "parameters", Order = 6)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by JSON serialization.")]
        private TruePbrNamedValues ParametersData
        {
            get
            {
                var values = new TruePbrNamedValues();
                foreach (var parameter in TruePbrSchema.Parameters)
                {
                    var slice = Parameters.AsSpan(parameter.Index, parameter.Count);
                    values.Set(parameter.Name, parameter.Count == 1 ? slice[0] : slice.ToArray());
                }
                return values;
            }
            set
            {
                foreach (var parameter in TruePbrSchema.Parameters)
                {
                    if (value.TryGetFloats(parameter.Name, parameter.Count, out var floats))
                        floats.CopyTo(Parameters, parameter.Index);
                }
            }
        }
        #endregion

        public bool HasFeature(TruePbrFeature feature)
        {
            return (FeatureFlags & feature.Mask) != 0;
        }

        public void SetFeature(TruePbrFeature feature, bool enabled)
        {
            FeatureFlags = enabled ? FeatureFlags | feature.Mask : FeatureFlags & ~feature.Mask;
        }

        public static TruePbrSection FromMaterial(BGSM material)
        {
            var emittance = BaseMaterialFile.Color.FromUInt32(material.EmittanceColor);
            var section = new TruePbrSection();
            section.Common = new TruePbrCommon
            {
                TileU = material.TileU,
                TileV = material.TileV,
                UvOffset = [material.UOffset, material.VOffset],
                UvScale = [material.UScale, material.VScale],
                Alpha = material.Alpha,
                AlphaBlendMode = material.AlphaBlendMode,
                AlphaTest = material.AlphaTest,
                AlphaTestRef = material.AlphaTestRef,
                Refraction = material.Refraction,
                RefractionFalloff = material.RefractionFalloff,
                RefractionPower = material.RefractionPower,
                ModelSpaceNormals = material.ModelSpaceNormals,
                Emissive = material.EmitEnabled,
                EmissiveColor = [emittance.R, emittance.G, emittance.B],
                EmissiveScale = material.EmittanceMult,
                ExternalEmittance = material.ExternalEmittance,
            };
            return section;
        }

        public static TruePbrSection Read(BinaryReader input)
        {
            var layout = input.ReadUInt32();
            if (layout != TruePbrSchema.Layout)
                throw new FormatException($"Unsupported TruePBR layout {layout}.");

            var section = new TruePbrSection { Layout = layout };
            section.Common = TruePbrCommon.Read(input);

            for (int i = 0; i < section.Textures.Length; i++)
                section.Textures[i] = BaseMaterialFile.ReadString(input);

            section.PayloadVersion = input.ReadUInt32();
            if (section.PayloadVersion != TruePbrSchema.PayloadVersion)
                throw new FormatException($"Unsupported TruePBR payload version {section.PayloadVersion}.");

            section.FeatureFlags = input.ReadUInt32();
            for (int i = 0; i < section.Parameters.Length; i++)
                section.Parameters[i] = input.ReadSingle();

            return section;
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Layout);
            Common.Write(output);

            foreach (var path in Textures)
                BaseMaterialFile.WriteString(output, path);

            output.Write(PayloadVersion);
            output.Write(FeatureFlags);
            foreach (var value in Parameters)
                output.Write(value);
        }
    }
}
