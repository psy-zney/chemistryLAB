using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChemistryLab.Application
{
    public enum ReactionTransactionKind
    {
        Craft = 0,
        Buy = 1,
        QuestReward = 2,
        Penalty = 3,
        CollectProduct = 4
    }

    /// <summary>
    /// Immutable instruction/log record to be applied by one persistence transaction.
    /// This class deliberately performs no database or inventory mutation itself.
    /// </summary>
    public sealed class ReactionTransaction
    {
        public ReactionTransaction(
            string transactionId,
            ReactionTransactionKind kind,
            IReadOnlyDictionary<string, decimal> inputs,
            IReadOnlyDictionary<string, decimal> outputs,
            long dollarDelta,
            long diamondDelta,
            string idempotencyKey,
            DateTimeOffset timestamp)
        {
            TransactionId = RequireText(transactionId, nameof(transactionId));
            Kind = kind;
            Inputs = CopyQuantities(inputs, nameof(inputs));
            Outputs = CopyQuantities(outputs, nameof(outputs));
            DollarDelta = dollarDelta;
            DiamondDelta = diamondDelta;
            IdempotencyKey = RequireText(idempotencyKey, nameof(idempotencyKey));
            Timestamp = timestamp;
        }

        public string TransactionId { get; private set; }
        public ReactionTransactionKind Kind { get; private set; }
        public IReadOnlyDictionary<string, decimal> Inputs { get; private set; }
        public IReadOnlyDictionary<string, decimal> Outputs { get; private set; }
        public long DollarDelta { get; private set; }
        public long DiamondDelta { get; private set; }
        public string IdempotencyKey { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        private static IReadOnlyDictionary<string, decimal> CopyQuantities(
            IReadOnlyDictionary<string, decimal> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new ArgumentException("A non-empty item id is required.", parameterName);
                }

                if (entry.Value <= 0m)
                {
                    throw new ArgumentOutOfRangeException(parameterName, "Item quantities must be greater than zero.");
                }

                copy.Add(entry.Key, entry.Value);
            }

            return new ReadOnlyDictionary<string, decimal>(copy);
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }
}
