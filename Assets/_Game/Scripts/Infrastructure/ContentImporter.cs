using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChemistryLab.Application;
using ChemistryLab.Domain;

namespace ChemistryLab.Infrastructure
{
    /// <summary>
    /// Imports the generated JSON content artifact into immutable runtime records.
    /// It accepts camelCase and PascalCase field names, and inherits contentVersion
    /// from the root document when an individual record does not repeat it.
    /// </summary>
    public sealed class ContentImporter
    {
        public ContentCatalogue ImportFromJson(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) throw new ArgumentException("JSON content is required.", nameof(jsonContent));

            var root = AsObject(new JsonParser(jsonContent).Parse(), "root");
            var contentVersion = OptionalString(root, "contentVersion", "ContentVersion");
            if (string.IsNullOrWhiteSpace(contentVersion)) throw new FormatException("The root contentVersion is required.");

            // The first playable catalogue shipped with nested reaction data. Keep it
            // loadable while the domain model consumes the newer, normalised format.
            if (!HasValue(root, "reactionParticipants", "ReactionParticipants"))
            {
                return ImportLegacyCatalogue(root, contentVersion);
            }

            var catalogue = new ContentCatalogue(
                ImportChemicalItems(GetArray(root, "chemicalItems", "ChemicalItems"), contentVersion),
                ImportReactions(GetArray(root, "reactions", "Reactions"), contentVersion),
                ImportParticipants(GetArray(root, "reactionParticipants", "ReactionParticipants", "participants", "Participants")),
                ImportObservations(GetArray(root, "observations", "Observations", "reactionObservations", "ReactionObservations"), contentVersion),
                ImportLabTools(GetArray(root, "labTools", "LabTools"), contentVersion),
                ImportQuests(GetArray(root, "quests", "Quests", "questTemplates", "QuestTemplates"), contentVersion));

            var issues = new ContentValidator().Validate(catalogue);
            for (var index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == ContentValidationSeverity.Error)
                {
                    throw new FormatException("Invalid content at " + issues[index].Row + "." + issues[index].Field + ": " + issues[index].Message);
                }
            }

            return catalogue;
        }

        private static ContentCatalogue ImportLegacyCatalogue(Dictionary<string, object> root, string contentVersion)
        {
            var chemicals = new List<ChemicalItem>(ImportChemicalItems(GetArray(root, "chemicalItems", "ChemicalItems"), contentVersion));
            var reactions = new List<Reaction>();
            var participants = new List<ReactionParticipant>();
            var observations = new List<ReactionObservation>();
            var rawReactions = GetArray(root, "reactions", "Reactions");

            for (var reactionIndex = 0; reactionIndex < rawReactions.Count; reactionIndex++)
            {
                var legacyReaction = AsObject(rawReactions[reactionIndex], "reactions[" + reactionIndex + "]");
                var reactionId = RequiredString(legacyReaction, "id", "Id");
                var legacyParticipants = GetArray(legacyReaction, "participants", "Participants");
                string outputId = null;

                for (var participantIndex = 0; participantIndex < legacyParticipants.Count; participantIndex++)
                {
                    var legacyParticipant = AsObject(legacyParticipants[participantIndex], "reactions[" + reactionIndex + "].participants[" + participantIndex + "]");
                    var role = ReadEnum<ReactionParticipantRole>(legacyParticipant, "role", "Role");
                    var coefficient = RequiredInt(legacyParticipant, "coefficient", "Coefficient");
                    var itemId = RequiredString(legacyParticipant, "itemId", "ItemId");
                    if (role == ReactionParticipantRole.Product && outputId == null) outputId = itemId;
                    participants.Add(new ReactionParticipant(reactionId, itemId, role, coefficient, coefficient * 10m, participantIndex));
                }

                if (string.IsNullOrWhiteSpace(outputId))
                    throw new FormatException("reactions[" + reactionIndex + "] must contain at least one product participant.");

                reactions.Add(new Reaction(
                    reactionId,
                    RequiredString(legacyReaction, "formulaEquation", "EquationDisplay"),
                    "lab.condition.clean",
                    RequiredString(legacyReaction, "requiredToolId", "toolId", "ToolId"),
                    OptionalInt(legacyReaction, 0, "unlockLevel", "UnlockLevel"),
                    outputId,
                    10m,
                    1.5m,
                    "Chemistry Lab legacy catalogue",
                    "Chemistry Lab",
                    LegacyReviewDate,
                    contentVersion));

                var legacyObservations = GetArray(legacyReaction, "observations", "Observations");
                for (var observationIndex = 0; observationIndex < legacyObservations.Count; observationIndex++)
                {
                    var legacyObservation = AsObject(legacyObservations[observationIndex], "reactions[" + reactionIndex + "].observations[" + observationIndex + "]");
                    observations.Add(new ReactionObservation(
                        reactionId,
                        observationIndex,
                        ReadEnum<ReactionObservationType>(legacyObservation, "type", "Type"),
                        ReactionObservationTimingPhase.OnCompletion,
                        ObservationIntensity.Medium,
                        OptionalString(legacyObservation, "beforeColor", "BeforeColor"),
                        OptionalString(legacyObservation, "afterColor", "AfterColor"),
                        null,
                        null,
                        null,
                        RequiredString(legacyObservation, "descriptionKey", "localisationKey", "LocalisationKey"),
                        "Chemistry Lab legacy catalogue",
                        "Chemistry Lab",
                        LegacyReviewDate,
                        contentVersion));
                }
            }

            var tools = ImportLegacyTools(GetArray(root, "labTools", "LabTools"), contentVersion);
            var quests = ImportLegacyQuests(GetArray(root, "questTemplates", "QuestTemplates", "quests", "Quests"), contentVersion);
            var catalogue = new ContentCatalogue(chemicals, reactions, participants, observations, tools, quests);
            var issues = new ContentValidator().Validate(catalogue);
            for (var index = 0; index < issues.Count; index++)
                if (issues[index].Severity == ContentValidationSeverity.Error)
                    throw new FormatException("Invalid legacy content at " + issues[index].Row + "." + issues[index].Field + ": " + issues[index].Message);
            return catalogue;
        }

        private static IEnumerable<LabTool> ImportLegacyTools(List<object> values, string contentVersion)
        {
            var result = new List<LabTool>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "labTools[" + index + "]");
                result.Add(new LabTool(
                    RequiredString(value, "id", "Id"),
                    RequiredString(value, "nameKey", "NameKey"),
                    LabToolType.Container,
                    RequiredDecimal(value, "capacityMl", "capacityGram", "CapacityGram"),
                    true,
                    ToolCleanState.Clean,
                    OptionalInt(value, 0, "unlockLevel", "UnlockLevel"),
                    RequiredLong(value, "price", "Price"),
                    RequiredString(value, "iconAddress", "visualAddressKey", "VisualAddressKey"),
                    new[] { LabToolCapability.Contain, LabToolCapability.Measure, LabToolCapability.Pour, LabToolCapability.Stir },
                    "Chemistry Lab legacy catalogue",
                    "Chemistry Lab",
                    LegacyReviewDate,
                    contentVersion));
            }
            return result;
        }

        private static IEnumerable<QuestTemplate> ImportLegacyQuests(List<object> values, string contentVersion)
        {
            var result = new List<QuestTemplate>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "questTemplates[" + index + "]");
                var reward = AsObject(GetValue(value, "rewardRule", "RewardRule"), "questTemplates[" + index + "].rewardRule");
                var unlockIds = new List<string>();
                var rawUnlockIds = GetArray(reward, "itemUnlockIds", "ItemUnlockIds");
                for (var unlockIndex = 0; unlockIndex < rawUnlockIds.Count; unlockIndex++)
                    unlockIds.Add(ReadString(rawUnlockIds[unlockIndex], "itemUnlockIds[" + unlockIndex + "]"));

                result.Add(new QuestTemplate(
                    RequiredString(value, "id", "Id"),
                    QuestTier.Tutorial,
                    RequiredString(value, "targetReactionId", "TargetReactionId"),
                    RequiredString(value, "titleKey", "dialogueKey", "DialogueKey"),
                    RequiredString(value, "descriptionKey", "applicationKey", "ApplicationKey"),
                    new QuestRewardRule(RequiredLong(reward, "dollars", "Dollars"), OptionalInt(reward, 0, "experience", "diamonds", "Experience"), unlockIds),
                    new List<QuestPrerequisite>(),
                    "Chemistry Lab legacy catalogue",
                    "Chemistry Lab",
                    LegacyReviewDate,
                    contentVersion));
            }
            return result;
        }

        private static readonly DateTimeOffset LegacyReviewDate = new DateTimeOffset(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);

        private static IEnumerable<ChemicalItem> ImportChemicalItems(List<object> values, string fallbackVersion)
        {
            var result = new List<ChemicalItem>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "chemicalItems[" + index + "]");
                result.Add(new ChemicalItem(
                    RequiredString(value, "id", "Id"), RequiredString(value, "nameKey", "NameKey"), RequiredString(value, "formula", "Formula"),
                    ReadEnum<ElementGroup>(value, "group", "Group"), ReadEnum<ChemicalState>(value, "state", "State"),
                    RequiredString(value, "color", "Color"), RequiredString(value, "odorKey", "OdorKey"), ReadEnum<SolubilityLevel>(value, "solubility", "Solubility"),
                    ReadEnum<HazardTier>(value, "hazardTier", "HazardTier"), ReadEnum<ItemRarity>(value, "rarity", "Rarity"),
                    RequiredLong(value, "price", "Price"), RequiredString(value, "iconAddress", "IconAddress"),
                    RequiredString(value, "sourceRef", "SourceRef"), RequiredString(value, "reviewer", "Reviewer"),
                    RequiredDate(value, "reviewedAt", "ReviewedAt"), RecordVersion(value, fallbackVersion), OptionalInt(value, 0, "unlockLevel", "UnlockLevel")));
            }

            return result;
        }

        private static IEnumerable<Reaction> ImportReactions(List<object> values, string fallbackVersion)
        {
            var result = new List<Reaction>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "reactions[" + index + "]");
                result.Add(new Reaction(
                    RequiredString(value, "id", "Id"), RequiredString(value, "equationDisplay", "EquationDisplay"), RequiredString(value, "conditionKey", "ConditionKey"),
                    RequiredString(value, "toolId", "ToolId"), RequiredInt(value, "unlockLevel", "UnlockLevel"), RequiredString(value, "outputId", "OutputId"),
                    RequiredDecimal(value, "outputMassGram", "OutputMassGram"), RequiredDecimal(value, "animationDurationSeconds", "AnimationDurationSeconds"),
                    RequiredString(value, "sourceRef", "SourceRef"), RequiredString(value, "reviewer", "Reviewer"), RequiredDate(value, "reviewedAt", "ReviewedAt"), RecordVersion(value, fallbackVersion)));
            }

            return result;
        }

        private static IEnumerable<ReactionParticipant> ImportParticipants(List<object> values)
        {
            var result = new List<ReactionParticipant>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "reactionParticipants[" + index + "]");
                result.Add(new ReactionParticipant(
                    RequiredString(value, "reactionId", "ReactionId"), RequiredString(value, "itemId", "ItemId"),
                    ReadEnum<ReactionParticipantRole>(value, "role", "Role"), RequiredInt(value, "coefficient", "Coefficient"),
                    RequiredDecimal(value, "massGramGame", "MassGramGame"), RequiredInt(value, "pourOrder", "PourOrder")));
            }

            return result;
        }

        private static IEnumerable<LabTool> ImportLabTools(List<object> values, string fallbackVersion)
        {
            var result = new List<LabTool>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "labTools[" + index + "]");
                var capabilities = new List<LabToolCapability>();
                var rawCapabilities = GetArray(value, "capabilities", "Capabilities");
                for (var capabilityIndex = 0; capabilityIndex < rawCapabilities.Count; capabilityIndex++)
                {
                    capabilities.Add(ReadEnum<LabToolCapability>(rawCapabilities[capabilityIndex], "capabilities[" + capabilityIndex + "]"));
                }

                result.Add(new LabTool(
                    RequiredString(value, "id", "Id"), RequiredString(value, "nameKey", "NameKey"), ReadEnum<LabToolType>(value, "type", "Type"),
                    RequiredDecimal(value, "capacityGram", "CapacityGram"), RequiredBoolean(value, "isOwned", "IsOwned"), ReadEnum<ToolCleanState>(value, "cleanState", "CleanState"),
                    RequiredInt(value, "unlockLevel", "UnlockLevel"), RequiredLong(value, "price", "Price"), RequiredString(value, "visualAddressKey", "VisualAddressKey"), capabilities,
                    RequiredString(value, "sourceRef", "SourceRef"), RequiredString(value, "reviewer", "Reviewer"), RequiredDate(value, "reviewedAt", "ReviewedAt"), RecordVersion(value, fallbackVersion)));
            }

            return result;
        }

        private static IEnumerable<QuestTemplate> ImportQuests(List<object> values, string fallbackVersion)
        {
            var result = new List<QuestTemplate>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "quests[" + index + "]");
                var reward = AsObject(GetValue(value, "rewardRule", "RewardRule"), "quests[" + index + "].rewardRule");
                var unlockIds = new List<string>();
                var rawUnlockIds = GetArray(reward, "itemUnlockIds", "ItemUnlockIds");
                for (var unlockIndex = 0; unlockIndex < rawUnlockIds.Count; unlockIndex++) unlockIds.Add(ReadString(rawUnlockIds[unlockIndex], "itemUnlockIds[" + unlockIndex + "]"));

                var prerequisites = new List<QuestPrerequisite>();
                var rawPrerequisites = GetArray(value, "prerequisites", "Prerequisites");
                for (var prerequisiteIndex = 0; prerequisiteIndex < rawPrerequisites.Count; prerequisiteIndex++)
                {
                    var prerequisite = AsObject(rawPrerequisites[prerequisiteIndex], "prerequisites[" + prerequisiteIndex + "]");
                    prerequisites.Add(new QuestPrerequisite(
                        ReadEnum<QuestPrerequisiteType>(prerequisite, "type", "Type"), OptionalString(prerequisite, "targetId", "TargetId"),
                        RequiredInt(prerequisite, "requiredAmount", "RequiredAmount")));
                }

                result.Add(new QuestTemplate(
                    RequiredString(value, "id", "Id"), ReadEnum<QuestTier>(value, "tier", "Tier"), RequiredString(value, "targetReactionId", "TargetReactionId"),
                    RequiredString(value, "dialogueKey", "DialogueKey"), RequiredString(value, "applicationKey", "ApplicationKey"),
                    new QuestRewardRule(RequiredLong(reward, "dollars", "Dollars"), RequiredInt(reward, "experience", "Experience"), unlockIds), prerequisites,
                    RequiredString(value, "sourceRef", "SourceRef"), RequiredString(value, "reviewer", "Reviewer"), RequiredDate(value, "reviewedAt", "ReviewedAt"), RecordVersion(value, fallbackVersion)));
            }

            return result;
        }

        private static IEnumerable<ReactionObservation> ImportObservations(List<object> values, string fallbackVersion)
        {
            var result = new List<ReactionObservation>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = AsObject(values[index], "observations[" + index + "]");
                result.Add(new ReactionObservation(
                    RequiredString(value, "reactionId", "ReactionId"), RequiredInt(value, "sequence", "Sequence"), ReadEnum<ReactionObservationType>(value, "type", "Type"),
                    ReadEnum<ReactionObservationTimingPhase>(value, "timingPhase", "TimingPhase"), ReadEnum<ObservationIntensity>(value, "intensity", "Intensity"),
                    OptionalString(value, "beforeColor", "BeforeColor"), OptionalString(value, "afterColor", "AfterColor"), OptionalString(value, "particleAssetKey", "ParticleAssetKey"),
                    OptionalString(value, "audioAssetKey", "AudioAssetKey"), OptionalString(value, "hapticAssetKey", "HapticAssetKey"), RequiredString(value, "localisationKey", "LocalisationKey"),
                    RequiredString(value, "sourceRef", "SourceRef"), RequiredString(value, "reviewer", "Reviewer"), RequiredDate(value, "reviewedAt", "ReviewedAt"), RecordVersion(value, fallbackVersion)));
            }

            return result;
        }

        private static string RecordVersion(Dictionary<string, object> value, string fallbackVersion)
        {
            var version = OptionalString(value, "contentVersion", "ContentVersion");
            return string.IsNullOrWhiteSpace(version) ? fallbackVersion : version;
        }

        private static Dictionary<string, object> AsObject(object value, string path)
        {
            var result = value as Dictionary<string, object>;
            if (result == null) throw new FormatException(path + " must be a JSON object.");
            return result;
        }

        private static List<object> GetArray(Dictionary<string, object> value, params string[] names)
        {
            var raw = GetValue(value, names);
            var result = raw as List<object>;
            if (result == null) throw new FormatException(names[0] + " must be a JSON array.");
            return result;
        }

        private static object GetValue(Dictionary<string, object> value, params string[] names)
        {
            for (var index = 0; index < names.Length; index++)
            {
                object result;
                if (value.TryGetValue(names[index], out result)) return result;
            }

            throw new FormatException("Required field '" + names[0] + "' is missing.");
        }

        private static bool HasValue(Dictionary<string, object> value, params string[] names)
        {
            for (var index = 0; index < names.Length; index++)
                if (value.ContainsKey(names[index])) return true;
            return false;
        }

        private static string RequiredString(Dictionary<string, object> value, params string[] names)
        {
            var result = OptionalString(value, names);
            if (string.IsNullOrWhiteSpace(result)) throw new FormatException("Required text field '" + names[0] + "' is missing.");
            return result;
        }

        private static string OptionalString(Dictionary<string, object> value, params string[] names)
        {
            for (var index = 0; index < names.Length; index++)
            {
                object raw;
                if (value.TryGetValue(names[index], out raw)) return raw == null ? null : ReadString(raw, names[index]);
            }

            return null;
        }

        private static string ReadString(object value, string path)
        {
            var result = value as string;
            if (result == null) throw new FormatException(path + " must be a string.");
            return result;
        }

        private static int RequiredInt(Dictionary<string, object> value, params string[] names) { return ConvertNumber<int>(GetValue(value, names), names[0]); }
        private static int OptionalInt(Dictionary<string, object> value, int fallback, params string[] names)
        {
            for (var index = 0; index < names.Length; index++)
            {
                object raw;
                if (value.TryGetValue(names[index], out raw)) return ConvertNumber<int>(raw, names[index]);
            }

            return fallback;
        }
        private static long RequiredLong(Dictionary<string, object> value, params string[] names) { return ConvertNumber<long>(GetValue(value, names), names[0]); }
        private static decimal RequiredDecimal(Dictionary<string, object> value, params string[] names) { return ConvertNumber<decimal>(GetValue(value, names), names[0]); }

        private static T ConvertNumber<T>(object value, string path) where T : IConvertible
        {
            try { return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture); }
            catch (Exception exception) { throw new FormatException(path + " must be a number.", exception); }
        }

        private static bool RequiredBoolean(Dictionary<string, object> value, params string[] names)
        {
            var raw = GetValue(value, names);
            if (!(raw is bool)) throw new FormatException(names[0] + " must be true or false.");
            return (bool)raw;
        }

        private static DateTimeOffset RequiredDate(Dictionary<string, object> value, params string[] names)
        {
            var raw = RequiredString(value, names);
            DateTimeOffset result;
            if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
                throw new FormatException(names[0] + " must be an ISO-8601 timestamp.");
            return result;
        }

        private static TEnum ReadEnum<TEnum>(Dictionary<string, object> value, params string[] names) where TEnum : struct
        {
            return ReadEnum<TEnum>(GetValue(value, names), names[0]);
        }

        private static TEnum ReadEnum<TEnum>(object raw, string path) where TEnum : struct
        {
            try
            {
                TEnum result;
                if (raw is string)
                {
                    if (Enum.TryParse<TEnum>((string)raw, true, out result) && Enum.IsDefined(typeof(TEnum), result)) return result;
                }
                else if (raw is decimal || raw is long || raw is int)
                {
                    result = (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(raw, CultureInfo.InvariantCulture));
                    if (Enum.IsDefined(typeof(TEnum), result)) return result;
                }
            }
            catch (Exception exception) { throw new FormatException(path + " contains an invalid enum value.", exception); }

            throw new FormatException(path + " contains an invalid enum value.");
        }

        /// <summary>Minimal JSON parser kept here to avoid a runtime dependency on UnityEngine or a third-party package.</summary>
        private sealed class JsonParser
        {
            private readonly string source;
            private int position;

            public JsonParser(string source) { this.source = source; }

            public object Parse()
            {
                var result = ParseValue();
                SkipWhitespace();
                if (position != source.Length) throw Error("Unexpected trailing characters.");
                return result;
            }

            private object ParseValue()
            {
                SkipWhitespace();
                if (position >= source.Length) throw Error("A JSON value is expected.");
                switch (source[position])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': ConsumeLiteral("true"); return true;
                    case 'f': ConsumeLiteral("false"); return false;
                    case 'n': ConsumeLiteral("null"); return null;
                    default: return ParseNumber();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                Consume('{');
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryConsume('}')) return result;
                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Consume(':');
                    var value = ParseValue();
                    if (result.ContainsKey(key)) throw Error("Duplicate object key '" + key + "'.");
                    result.Add(key, value);
                    SkipWhitespace();
                    if (TryConsume('}')) return result;
                    Consume(',');
                }
            }

            private List<object> ParseArray()
            {
                Consume('[');
                var result = new List<object>();
                SkipWhitespace();
                if (TryConsume(']')) return result;
                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']')) return result;
                    Consume(',');
                }
            }

            private string ParseString()
            {
                Consume('"');
                var result = new StringBuilder();
                while (position < source.Length)
                {
                    var current = source[position++];
                    if (current == '"') return result.ToString();
                    if (current < ' ') throw Error("Control character in string.");
                    if (current != '\\') { result.Append(current); continue; }
                    if (position >= source.Length) throw Error("Unfinished escape sequence.");
                    var escaped = source[position++];
                    switch (escaped)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ParseUnicodeCharacter()); break;
                        default: throw Error("Invalid escape sequence.");
                    }
                }

                throw Error("Unterminated string.");
            }

            private char ParseUnicodeCharacter()
            {
                if (position + 4 > source.Length) throw Error("Invalid unicode escape.");
                var hex = source.Substring(position, 4);
                position += 4;
                ushort value;
                if (!ushort.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value)) throw Error("Invalid unicode escape.");
                return (char)value;
            }

            private decimal ParseNumber()
            {
                var start = position;
                if (TryConsume('-')) { }
                ConsumeDigits();
                if (TryConsume('.')) ConsumeDigits();
                if (position < source.Length && (source[position] == 'e' || source[position] == 'E'))
                {
                    position++;
                    if (position < source.Length && (source[position] == '+' || source[position] == '-')) position++;
                    ConsumeDigits();
                }

                decimal result;
                if (!decimal.TryParse(source.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out result)) throw Error("Invalid number.");
                return result;
            }

            private void ConsumeDigits()
            {
                var start = position;
                while (position < source.Length && source[position] >= '0' && source[position] <= '9') position++;
                if (start == position) throw Error("A digit is required.");
            }

            private void Consume(char expected)
            {
                SkipWhitespace();
                if (position >= source.Length || source[position] != expected) throw Error("Expected '" + expected + "'.");
                position++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (position < source.Length && source[position] == expected) { position++; return true; }
                return false;
            }

            private void ConsumeLiteral(string literal)
            {
                if (position + literal.Length > source.Length || source.Substring(position, literal.Length) != literal) throw Error("Invalid literal.");
                position += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (position < source.Length && char.IsWhiteSpace(source[position])) position++;
            }

            private FormatException Error(string message) { return new FormatException(message + " Position: " + position + "."); }
        }
    }
}
