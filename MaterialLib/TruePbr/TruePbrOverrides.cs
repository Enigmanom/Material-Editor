using System.Runtime.Serialization;

namespace MaterialLib
{
    [DataContract]
    public sealed class TruePbrThinSurfaceOverride
    {
        public const string BlockTag = "CSTS";

        public TruePbrThinSurfaceOverride()
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
            SubsurfaceBalance = Default("subsurfaceBalance");
            FrontSpecularWeight = Default("thinSurfaceFrontSpecularWeight");
            BackSpecularWeight = Default("thinSurfaceBackSpecularWeight");
            BackRoughnessScale = Default("thinSurfaceBackRoughnessScale");
        }

        [DataMember(Name = "subsurfaceBalance")]
        public float SubsurfaceBalance { get; set; }

        [DataMember(Name = "frontSpecularWeight")]
        public float FrontSpecularWeight { get; set; }

        [DataMember(Name = "backSpecularWeight")]
        public float BackSpecularWeight { get; set; }

        [DataMember(Name = "backRoughnessScale")]
        public float BackRoughnessScale { get; set; }

        public static TruePbrThinSurfaceOverride Read(BinaryReader input)
        {
            return new TruePbrThinSurfaceOverride
            {
                SubsurfaceBalance = input.ReadSingle(),
                FrontSpecularWeight = input.ReadSingle(),
                BackSpecularWeight = input.ReadSingle(),
                BackRoughnessScale = input.ReadSingle(),
            };
        }

        public void Write(BinaryWriter output)
        {
            output.Write(SubsurfaceBalance);
            output.Write(FrontSpecularWeight);
            output.Write(BackSpecularWeight);
            output.Write(BackRoughnessScale);
        }

        private static float Default(string name)
        {
            return TruePbrSchema.Parameters.First(p => p.Name == name).Default;
        }
    }

    [DataContract]
    public sealed class TruePbrFuzzOverride
    {
        public const string BlockTag = "CSFZ";

        public TruePbrFuzzOverride()
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
            Roughness = -1.0f;
        }

        // Negative uses the base roughness.
        [DataMember(Name = "roughness")]
        public float Roughness { get; set; }

        [DataMember(Name = "roughnessFromDisplacementGreen")]
        public bool RoughnessFromDisplacementGreen { get; set; }

        public static TruePbrFuzzOverride Read(BinaryReader input)
        {
            var roughness = input.ReadSingle();
            var fromGreen = input.ReadUInt32();
            if (fromGreen > 1)
                throw new FormatException("Invalid fuzz displacement flag.");

            return new TruePbrFuzzOverride { Roughness = roughness, RoughnessFromDisplacementGreen = fromGreen != 0 };
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Roughness);
            output.Write(RoughnessFromDisplacementGreen ? 1u : 0u);
        }
    }
}
