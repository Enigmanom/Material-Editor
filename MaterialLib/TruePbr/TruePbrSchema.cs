namespace MaterialLib
{
    public sealed record TruePbrParameter(string Name, int Index, int Count, float Default, float Min, float Max, string Tooltip)
    {
        public string Label => TruePbrSchema.Humanize(Name);

        public string ComponentLabel(int component)
        {
            return Count == 1 ? Label : $"{Label} {"RGB"[component]}";
        }
    }

    public sealed record TruePbrFeature(string Name, int Bit, string Tooltip, string[]? Requires = null, string[]? Excludes = null)
    {
        public string Label => TruePbrSchema.Humanize(Name);
        public uint Mask => 1u << Bit;
    }

    public sealed record TruePbrTextureSlot(string Name, int Index, string Label, string Tooltip, bool Required = false, string? RequiredByFeature = null);

    public static class TruePbrSchema
    {
        public const uint Layout = 1;
        public const uint PayloadVersion = 2;

        public static IReadOnlyList<TruePbrParameter> Parameters { get; } = BuildParameters(
            Scalar("roughnessScale", 1.0f, 0, float.PositiveInfinity, "Multiplies the roughness from the RMAOS texture."),
            Scalar("specularLevel", 0.04f, 0, 1, "Non-metal reflectance. Multiplies the RMAOS alpha."),
            Scalar("displacementScale", 1.0f, 0, float.PositiveInfinity, "Parallax depth."),
            Color("subsurfaceColor", "Color of the light passing under the surface."),
            Scalar("subsurfaceOpacity", 0.0f, 0, 1, "0 = no subsurface, 1 = full."),
            Color("coatColor", "Tint of the coat."),
            Scalar("coatStrength", 1.0f, 0, 1, "Strength of the coat layer."),
            Scalar("coatRoughness", 1.0f, 0, 1, "Roughness of the coat layer."),
            Scalar("coatSpecularLevel", 0.04f, 0, 1, "Reflectance of the coat layer."),
            Color("fuzzColor", "Color of the fuzz sheen."),
            Scalar("fuzzWeight", 0.0f, 0, 1, "Strength of the fuzz sheen."),
            Scalar("glintScreenSpaceScale", 1.5f, 1, float.PositiveInfinity, "Sparkle size on screen."),
            Scalar("glintLogMicrofacetDensity", 40.0f, 1, 40, "Sparkle density (log scale)."),
            Scalar("glintMicrofacetRoughness", 0.015f, 0.005f, 0.3f, "Sparkle sharpness."),
            Scalar("glintDensityRandomization", 2.0f, 0, 5, "Randomization of the sparkle density."),
            Scalar("subsurfaceBalance", 0.75f, 0, 1, "Foliage and thin surface: front versus back light."),
            Scalar("thinSurfaceFrontSpecularWeight", 0.5f, 0, 1, "Front specular weight of thin surfaces."),
            Scalar("thinSurfaceBackSpecularWeight", 0.5f, 0, 1, "Back specular weight of thin surfaces."),
            Scalar("thinSurfaceBackRoughnessScale", 1.0f, 0, 3, "Back roughness scale of thin surfaces."));

        public static int ParameterCount { get; } = Parameters.Sum(p => p.Count);

        public static IReadOnlyList<TruePbrFeature> Features { get; } =
        [
            new("subsurface", 0, "Light scattering under the surface (skin, wax)."),
            new("twoLayer", 1, "Clear coat over the base (car paint, lacquer).",
                Excludes: ["subsurface", "fuzz", "glint", "foliage", "thinSurface"]),
            new("coloredCoat", 2, "Tinted coat. Needs two layer.", Requires: ["twoLayer"]),
            new("interlayerParallax", 3, "Depth between coat and base. Needs two layer.", Requires: ["twoLayer"]),
            new("coatNormal", 4, "Coat with its own normals. Needs two layer.", Requires: ["twoLayer"]),
            new("fuzz", 5, "Velvet or cloth sheen.", Excludes: ["glint"]),
            new("hairMarschner", 6, "Hair shading. Must be used alone.",
                Excludes: ["subsurface", "twoLayer", "coloredCoat", "interlayerParallax", "coatNormal", "fuzz", "glint", "foliage", "thinSurface"]),
            new("glint", 7, "Sparkles (snow, glitter)."),
            new("foliage", 8, "Leaves, lit from behind. Needs thin surface.", Requires: ["thinSurface"]),
            new("thinSurface", 9, "Paper-thin geometry, lit from both sides."),
        ];

        public static IReadOnlyList<TruePbrTextureSlot> Textures { get; } =
        [
            new("baseColor", 0, "Base Color", "Flat color without baked lighting. Alpha is opacity.", Required: true),
            new("normal", 1, "Normal", "Normal map (DirectX, green down).", Required: true),
            new("emissive", 2, "Emissive", "Glow color."),
            new("displacement", 3, "Displacement", "Height map for parallax.", RequiredByFeature: "interlayerParallax"),
            new("rmaos", 4, "RMAOS", "R roughness, G metalness, B AO, A specular.", Required: true),
            new("features0", 5, "Features 0", "Subsurface color or coat color."),
            new("features1", 6, "Features 1", "Coat normal and roughness, or fuzz.", RequiredByFeature: "coatNormal"),
        ];

        public static string Humanize(string name)
        {
            var chars = new List<char> { char.ToUpperInvariant(name[0]) };
            foreach (char c in name.Skip(1))
            {
                if (char.IsUpper(c))
                    chars.Add(' ');
                chars.Add(c);
            }

            return new string(chars.ToArray());
        }

        private static TruePbrParameter Scalar(string name, float value, float min, float max, string tooltip)
        {
            return new(name, 0, 1, value, min, max, tooltip);
        }

        private static TruePbrParameter Color(string name, string tooltip)
        {
            return new(name, 0, 3, 1.0f, 0, float.PositiveInfinity, tooltip);
        }

        private static List<TruePbrParameter> BuildParameters(params TruePbrParameter[] parameters)
        {
            var result = new List<TruePbrParameter>();
            int index = 0;
            foreach (var parameter in parameters)
            {
                result.Add(parameter with { Index = index });
                index += parameter.Count;
            }

            return result;
        }
    }
}
