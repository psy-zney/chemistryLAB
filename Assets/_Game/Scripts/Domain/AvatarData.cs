using System;

namespace ChemistryLab.Domain
{
    /// <summary>
    /// Player character avatar customization data (skin tone, hair, outfit, goggles/glasses).
    /// </summary>
    public sealed class AvatarData
    {
        public AvatarData(
            string playerName = "Dr. Chemist",
            string skinColorHex = "#FFDBAC",
            string hairStyleId = "hair_short",
            string hairColorHex = "#4A2E10",
            string outfitId = "outfit_labcoat",
            string glassesId = "goggles_safety")
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Dr. Chemist" : playerName;
            SkinColorHex = string.IsNullOrWhiteSpace(skinColorHex) ? "#FFDBAC" : skinColorHex;
            HairStyleId = string.IsNullOrWhiteSpace(hairStyleId) ? "hair_short" : hairStyleId;
            HairColorHex = string.IsNullOrWhiteSpace(hairColorHex) ? "#4A2E10" : hairColorHex;
            OutfitId = string.IsNullOrWhiteSpace(outfitId) ? "outfit_labcoat" : outfitId;
            GlassesId = string.IsNullOrWhiteSpace(glassesId) ? "goggles_safety" : glassesId;
        }

        public string PlayerName { get; private set; }
        public string SkinColorHex { get; private set; }
        public string HairStyleId { get; private set; }
        public string HairColorHex { get; private set; }
        public string OutfitId { get; private set; }
        public string GlassesId { get; private set; }

        public AvatarData WithName(string newName)
        {
            return new AvatarData(newName, SkinColorHex, HairStyleId, HairColorHex, OutfitId, GlassesId);
        }

        public AvatarData WithCustomization(
            string skinColorHex = null,
            string hairStyleId = null,
            string hairColorHex = null,
            string outfitId = null,
            string glassesId = null)
        {
            return new AvatarData(
                PlayerName,
                skinColorHex ?? SkinColorHex,
                hairStyleId ?? HairStyleId,
                hairColorHex ?? HairColorHex,
                outfitId ?? OutfitId,
                glassesId ?? GlassesId);
        }
    }
}
