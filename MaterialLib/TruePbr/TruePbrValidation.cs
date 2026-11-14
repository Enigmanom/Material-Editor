namespace MaterialLib
{
    public sealed record TruePbrValidationResult(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

    public static class TruePbrValidator
    {
        public static TruePbrValidationResult Validate(TruePbrSection section)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (section.Layout != TruePbrSchema.Layout)
                errors.Add($"Layout must be {TruePbrSchema.Layout} (is {section.Layout}).");

            if (section.PayloadVersion != TruePbrSchema.PayloadVersion)
                errors.Add($"Payload version must be {TruePbrSchema.PayloadVersion} (is {section.PayloadVersion}).");

            ValidateFeatures(section, errors);
            ValidateParameters(section, errors);
            ValidateTextures(section, errors, warnings);

            var emissiveScale = section.Common.EmissiveScale;
            if (!float.IsFinite(emissiveScale) || emissiveScale < 0)
                errors.Add("Emissive Scale must be finite and at least 0.");

            return new TruePbrValidationResult(errors, warnings);
        }

        private static void ValidateFeatures(TruePbrSection section, List<string> errors)
        {
            uint known = TruePbrSchema.Features.Aggregate(0u, (mask, f) => mask | f.Mask);
            if ((section.FeatureFlags & ~known) != 0)
                errors.Add("Feature flags use bits above 9.");

            var features = TruePbrSchema.Features.ToDictionary(f => f.Name);
            foreach (var feature in TruePbrSchema.Features.Where(section.HasFeature))
            {
                foreach (var name in feature.Requires ?? [])
                {
                    if (!section.HasFeature(features[name]))
                        errors.Add($"{feature.Label} needs {features[name].Label}.");
                }

                foreach (var name in feature.Excludes ?? [])
                {
                    if (section.HasFeature(features[name]))
                        errors.Add($"{feature.Label} cannot be combined with {features[name].Label}.");
                }
            }
        }

        private static void ValidateParameters(TruePbrSection section, List<string> errors)
        {
            foreach (var parameter in TruePbrSchema.Parameters)
            {
                for (int i = 0; i < parameter.Count; i++)
                {
                    var value = section.Parameters[parameter.Index + i];
                    if (!float.IsFinite(value) || value < parameter.Min || value > parameter.Max)
                    {
                        var upper = float.IsInfinity(parameter.Max) ? "" : $" and at most {parameter.Max}";
                        errors.Add($"{parameter.ComponentLabel(i)} must be finite, at least {parameter.Min}{upper} (is {value}).");
                    }
                }
            }
        }

        private static void ValidateTextures(TruePbrSection section, List<string> errors, List<string> warnings)
        {
            foreach (var slot in TruePbrSchema.Textures)
            {
                var path = section.Textures[slot.Index];
                if (string.IsNullOrEmpty(path))
                {
                    var feature = slot.RequiredByFeature == null ? null : TruePbrSchema.Features.First(f => f.Name == slot.RequiredByFeature);
                    if (slot.Required)
                        errors.Add($"{slot.Label} texture is required.");
                    else if (feature != null && section.HasFeature(feature))
                        errors.Add($"{slot.Label} texture is required by {feature.Label}.");
                }
                else if (!path.StartsWith(@"pbr\", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("pbr/", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"{slot.Label} texture is not under pbr\\ and may replace a vanilla texture.");
                }
            }
        }
    }
}
