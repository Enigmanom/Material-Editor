using MaterialLib;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor
{
    public partial class Main
    {
        private const decimal TruePbrUnboundedMax = 1000000;

        private bool TruePbrEnabled
        {
            get { return ControlFactory.GetProperty(ControlNames.TruePbrEnabled, out var property) && Convert.ToBoolean(property); }
        }

        private bool TruePbrVisibility(CustomControl _)
        {
            return TruePbrEnabled;
        }

        private bool TruePbrOverrideVisibility(string overrideName)
        {
            return TruePbrEnabled && ControlFactory.GetProperty(overrideName, out var property) && Convert.ToBoolean(property);
        }

        private static object GetTruePbrValue(string name)
        {
            ControlFactory.GetProperty(name, out var property);
            return property;
        }

        private uint TitleVersion
        {
            get
            {
                bool truePbr = CurrentMaterialType == MaterialType.Material && currentMaterial.Version <= 2 && TruePbrEnabled;
                return truePbr ? BGSM.TruePbrFileVersion : currentMaterial.Version;
            }
        }

        private void UpdateTruePbrTab()
        {
            bool wanted = CurrentMaterialType == MaterialType.Material && (Game)listGame.SelectedIndex == Game.FO4;
            bool present = tabControl.TabPages.Contains(tabPageTruePbr);

            if (wanted && !present)
                tabControl.TabPages.Add(tabPageTruePbr);
            else if (!wanted && present)
                tabControl.TabPages.Remove(tabPageTruePbr);
        }

        private void RefreshTruePbr(CustomControl _)
        {
            RefreshTruePbrRows(_);
            Text = GetTitleText();
        }

        private void RefreshTruePbrRows(CustomControl _)
        {
            // One layout pass for all rows; showing them one by one stalls
            SuspendAll();
            ControlFactory.UpdateVisibility();
            ResumeAll();
            OnChanged();
        }

        private void CreateTruePbrControls(BGSM bgsm, Font fileFont)
        {
            var layout = layoutTruePbr;
            var section = bgsm.TruePBR ?? TruePbrSection.FromMaterial(bgsm);
            var common = section.Common;

            ControlFactory.CreateControl(layout, ControlNames.TruePbrEnabled, bgsm.TruePBR != null, null, RefreshTruePbr);

            ControlFactory.CreateControl(layout, ControlNames.TruePbrTileU, common.TileU, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrTileV, common.TileV, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrOffsetU, common.UvOffset[0], TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrOffsetV, common.UvOffset[1], TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrScaleU, common.UvScale[0], TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrScaleV, common.UvScale[1], TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrAlpha, common.Alpha, TruePbrVisibility);
            ControlFactory.CreateDropdownControl(layout, ControlNames.TruePbrAlphaBlendMode,
                Enum.GetNames<BaseMaterialFile.AlphaBlendModeType>(), (int)common.AlphaBlendMode, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrAlphaTestReference, common.AlphaTestRef, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrAlphaTest, common.AlphaTest, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrRefraction, common.Refraction, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrRefractionFalloff, common.RefractionFalloff, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrRefractionPower, common.RefractionPower, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrModelSpaceNormals, common.ModelSpaceNormals, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrEmissive, common.Emissive, TruePbrVisibility);

            string[] emissiveNames = [ControlNames.TruePbrEmissiveColorR, ControlNames.TruePbrEmissiveColorG, ControlNames.TruePbrEmissiveColorB];
            for (int i = 0; i < emissiveNames.Length; i++)
                CreateTruePbrNumber(emissiveNames[i], common.EmissiveColor[i], 0, float.PositiveInfinity, 0.1M, TruePbrVisibility);

            CreateTruePbrNumber(ControlNames.TruePbrEmissiveScale, common.EmissiveScale, 0, float.PositiveInfinity, 0.1M, TruePbrVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrExternalEmittance, common.ExternalEmittance, TruePbrVisibility);

            foreach (var slot in TruePbrSchema.Textures)
                ControlFactory.CreateFileControl(layout, ControlNames.TruePbrTexture(slot), fileFont, FileControl.FileType.Texture, section.Textures[slot.Index], TruePbrVisibility);

            foreach (var feature in TruePbrSchema.Features)
                ControlFactory.CreateControl(layout, ControlNames.TruePbrFeature(feature), section.HasFeature(feature), TruePbrVisibility);

            foreach (var parameter in TruePbrSchema.Parameters)
            {
                decimal increment = parameter.Max <= 5 ? 0.01M : 0.1M;
                for (int i = 0; i < parameter.Count; i++)
                    CreateTruePbrNumber(ControlNames.TruePbrParameter(parameter, i), section.Parameters[parameter.Index + i], parameter.Min, parameter.Max, increment, TruePbrVisibility);
            }

            var thin = section.ThinSurfaceOverride ?? new TruePbrThinSurfaceOverride();
            ControlFactory.CreateControl(layout, ControlNames.TruePbrThinSurfaceOverride, section.ThinSurfaceOverride != null, TruePbrVisibility, RefreshTruePbrRows);

            bool ThinSurfaceVisibility(CustomControl _) => TruePbrOverrideVisibility(ControlNames.TruePbrThinSurfaceOverride);

            CreateTruePbrNumber(ControlNames.TruePbrThinSurfaceBalance, thin.SubsurfaceBalance, 0, float.PositiveInfinity, 0.01M, ThinSurfaceVisibility);
            CreateTruePbrNumber(ControlNames.TruePbrThinSurfaceFrontSpecular, thin.FrontSpecularWeight, 0, float.PositiveInfinity, 0.01M, ThinSurfaceVisibility);
            CreateTruePbrNumber(ControlNames.TruePbrThinSurfaceBackSpecular, thin.BackSpecularWeight, 0, float.PositiveInfinity, 0.01M, ThinSurfaceVisibility);
            CreateTruePbrNumber(ControlNames.TruePbrThinSurfaceBackRoughness, thin.BackRoughnessScale, 0, float.PositiveInfinity, 0.01M, ThinSurfaceVisibility);

            var fuzz = section.FuzzOverride ?? new TruePbrFuzzOverride();
            ControlFactory.CreateControl(layout, ControlNames.TruePbrFuzzOverride, section.FuzzOverride != null, TruePbrVisibility, RefreshTruePbrRows);

            bool FuzzVisibility(CustomControl _) => TruePbrOverrideVisibility(ControlNames.TruePbrFuzzOverride);

            ControlFactory.CreateControl(layout, ControlNames.TruePbrFuzzRoughness, fuzz.Roughness, FuzzVisibility);
            ControlFactory.CreateControl(layout, ControlNames.TruePbrFuzzRoughnessFromDisplacement, fuzz.RoughnessFromDisplacementGreen, FuzzVisibility);
        }

        private void CreateTruePbrNumber(string name, float value, float min, float max, decimal increment, Func<CustomControl, bool> visibility)
        {
            decimal upper = float.IsInfinity(max) ? TruePbrUnboundedMax : (decimal)max;
            // A file may hold values the number box cannot represent
            decimal shown = float.IsFinite(value) ? Math.Clamp((decimal)Math.Clamp(value, min, (float)upper), (decimal)min, upper) : (decimal)min;
            ControlFactory.CreateDecimalControl(layoutTruePbr, name, shown, (decimal)min, upper, increment, visibility);
        }

        private TruePbrSection ReadTruePbrSection(uint version)
        {
            if (version > 2 || !TruePbrEnabled)
                return null;

            var section = new TruePbrSection();
            var common = section.Common;
            common.TileU = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrTileU));
            common.TileV = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrTileV));
            common.UvOffset = [Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrOffsetU)), Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrOffsetV))];
            common.UvScale = [Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrScaleU)), Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrScaleV))];
            common.Alpha = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrAlpha));
            common.AlphaBlendMode = (BaseMaterialFile.AlphaBlendModeType)Convert.ToInt32(GetTruePbrValue(ControlNames.TruePbrAlphaBlendMode));
            common.AlphaTestRef = Convert.ToByte(GetTruePbrValue(ControlNames.TruePbrAlphaTestReference));
            common.AlphaTest = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrAlphaTest));
            common.Refraction = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrRefraction));
            common.RefractionFalloff = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrRefractionFalloff));
            common.RefractionPower = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrRefractionPower));
            common.ModelSpaceNormals = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrModelSpaceNormals));
            common.Emissive = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrEmissive));
            common.EmissiveColor =
            [
                Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrEmissiveColorR)),
                Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrEmissiveColorG)),
                Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrEmissiveColorB)),
            ];
            common.EmissiveScale = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrEmissiveScale));
            common.ExternalEmittance = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrExternalEmittance));

            foreach (var slot in TruePbrSchema.Textures)
                section.Textures[slot.Index] = Convert.ToString(GetTruePbrValue(ControlNames.TruePbrTexture(slot)));

            foreach (var feature in TruePbrSchema.Features)
                section.SetFeature(feature, Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrFeature(feature))));

            foreach (var parameter in TruePbrSchema.Parameters)
            {
                for (int i = 0; i < parameter.Count; i++)
                    section.Parameters[parameter.Index + i] = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrParameter(parameter, i)));
            }

            if (Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrThinSurfaceOverride)))
            {
                section.ThinSurfaceOverride = new TruePbrThinSurfaceOverride
                {
                    SubsurfaceBalance = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrThinSurfaceBalance)),
                    FrontSpecularWeight = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrThinSurfaceFrontSpecular)),
                    BackSpecularWeight = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrThinSurfaceBackSpecular)),
                    BackRoughnessScale = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrThinSurfaceBackRoughness)),
                };
            }

            if (Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrFuzzOverride)))
            {
                section.FuzzOverride = new TruePbrFuzzOverride
                {
                    Roughness = Convert.ToSingle(GetTruePbrValue(ControlNames.TruePbrFuzzRoughness)),
                    RoughnessFromDisplacementGreen = Convert.ToBoolean(GetTruePbrValue(ControlNames.TruePbrFuzzRoughnessFromDisplacement)),
                };
            }

            return section;
        }

        private bool ConfirmTruePbrSave(BaseMaterialFile material)
        {
            if (material is BGSM { Version: > 2 } && TruePbrEnabled)
            {
                return MessageBox.Show("The TruePBR section is only stored in Fallout 4 materials and will not be saved. Continue?",
                    "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
            }

            if (material is not BGSM { TruePBR: { } section })
                return true;

            var result = TruePbrValidator.Validate(section);
            if (result.Errors.Count > 0)
            {
                MessageBox.Show("The TruePBR section is invalid and was not saved:\n\n" + string.Join("\n", result.Errors.Select(e => "- " + e)),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (result.Warnings.Count > 0)
            {
                MessageBox.Show("TruePBR warnings:\n\n" + string.Join("\n", result.Warnings.Select(w => "- " + w)),
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return true;
        }

        private void CreateTruePbrTooltips()
        {
            ControlFactory.SetTooltip(ControlNames.TruePbrEnabled, toolTip, "Store a TruePBR material (version 100) next to the vanilla one.");
            ControlFactory.SetTooltip(ControlNames.TruePbrTileU, toolTip, "Tile the U texture coordinate of the PBR textures.");
            ControlFactory.SetTooltip(ControlNames.TruePbrTileV, toolTip, "Tile the V texture coordinate of the PBR textures.");
            ControlFactory.SetTooltip(ControlNames.TruePbrOffsetU, toolTip, "Offset the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.TruePbrOffsetV, toolTip, "Offset the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.TruePbrScaleU, toolTip, "Scale the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.TruePbrScaleV, toolTip, "Scale the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.TruePbrAlpha, toolTip, "Fixed alpha value of the PBR material.");
            ControlFactory.SetTooltip(ControlNames.TruePbrAlphaBlendMode, toolTip, "Mode at which alpha is blended into other meshes.");
            ControlFactory.SetTooltip(ControlNames.TruePbrAlphaTestReference, toolTip, "Reference value for alpha testing.");
            ControlFactory.SetTooltip(ControlNames.TruePbrAlphaTest, toolTip, "Cut out using the alpha of the base color texture.");
            ControlFactory.SetTooltip(ControlNames.TruePbrRefraction, toolTip, "Toggle refraction of light.");
            ControlFactory.SetTooltip(ControlNames.TruePbrRefractionFalloff, toolTip, "Toggles refraction falloff.");
            ControlFactory.SetTooltip(ControlNames.TruePbrRefractionPower, toolTip, "Power of the refraction.");
            ControlFactory.SetTooltip(ControlNames.TruePbrModelSpaceNormals, toolTip, "The PBR normal map is in model space.");
            ControlFactory.SetTooltip(ControlNames.TruePbrEmissive, toolTip, "Toggle emissive output.");
            ControlFactory.SetTooltip(ControlNames.TruePbrEmissiveColorR, toolTip, "Emissive color (red). May exceed 1.");
            ControlFactory.SetTooltip(ControlNames.TruePbrEmissiveColorG, toolTip, "Emissive color (green). May exceed 1.");
            ControlFactory.SetTooltip(ControlNames.TruePbrEmissiveColorB, toolTip, "Emissive color (blue). May exceed 1.");
            ControlFactory.SetTooltip(ControlNames.TruePbrEmissiveScale, toolTip, "Emissive multiplier.");
            ControlFactory.SetTooltip(ControlNames.TruePbrExternalEmittance, toolTip, "Glow color is supplied by the game.");

            foreach (var slot in TruePbrSchema.Textures)
                ControlFactory.SetTooltip(ControlNames.TruePbrTexture(slot), toolTip, slot.Tooltip);

            foreach (var feature in TruePbrSchema.Features)
                ControlFactory.SetTooltip(ControlNames.TruePbrFeature(feature), toolTip, feature.Tooltip);

            foreach (var parameter in TruePbrSchema.Parameters)
            {
                for (int i = 0; i < parameter.Count; i++)
                    ControlFactory.SetTooltip(ControlNames.TruePbrParameter(parameter, i), toolTip, parameter.Tooltip);
            }

            ControlFactory.SetTooltip(ControlNames.TruePbrThinSurfaceOverride, toolTip, "Override the thin surface values for this material.");
            ControlFactory.SetTooltip(ControlNames.TruePbrThinSurfaceBalance, toolTip, "Front versus back light balance.");
            ControlFactory.SetTooltip(ControlNames.TruePbrThinSurfaceFrontSpecular, toolTip, "Front specular weight.");
            ControlFactory.SetTooltip(ControlNames.TruePbrThinSurfaceBackSpecular, toolTip, "Back specular weight.");
            ControlFactory.SetTooltip(ControlNames.TruePbrThinSurfaceBackRoughness, toolTip, "Back roughness scale.");
            ControlFactory.SetTooltip(ControlNames.TruePbrFuzzOverride, toolTip, "Override the fuzz values for this material.");
            ControlFactory.SetTooltip(ControlNames.TruePbrFuzzRoughness, toolTip, "Fuzz roughness. Negative uses the base roughness.");
            ControlFactory.SetTooltip(ControlNames.TruePbrFuzzRoughnessFromDisplacement, toolTip, "Take the fuzz roughness from the green channel of the displacement texture.");
        }
    }
}
