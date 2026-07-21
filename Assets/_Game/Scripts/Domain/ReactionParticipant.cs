using System;

namespace ChemistryLab.Domain
{
    public enum ReactionParticipantRole
    {
        Reactant = 0,
        Product = 1
    }

    /// <summary>
    /// A material and its required game quantity within a catalogue reaction.
    /// </summary>
    public sealed class ReactionParticipant
    {
        public ReactionParticipant(
            string reactionId,
            string itemId,
            ReactionParticipantRole role,
            int coefficient,
            decimal massGramGame,
            int pourOrder)
        {
            ReactionId = RequireText(reactionId, nameof(reactionId));
            ItemId = RequireText(itemId, nameof(itemId));
            Role = role;
            Coefficient = RequirePositive(coefficient, nameof(coefficient));
            MassGramGame = RequirePositive(massGramGame, nameof(massGramGame));
            PourOrder = RequireNonNegative(pourOrder, nameof(pourOrder));
        }

        public string ReactionId { get; private set; }
        public string ItemId { get; private set; }
        public ReactionParticipantRole Role { get; private set; }
        public int Coefficient { get; private set; }
        public decimal MassGramGame { get; private set; }
        public int PourOrder { get; private set; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }

        private static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        private static decimal RequirePositive(decimal value, string parameterName)
        {
            if (value <= 0m)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
