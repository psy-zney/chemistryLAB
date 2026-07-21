using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChemistryLab.Domain
{
    /// <summary>
    /// Player inventory keyed by immutable catalogue item id. Quantities use game grams.
    /// </summary>
    public sealed class Inventory
    {
        private readonly Dictionary<string, decimal> quantities;

        public Inventory()
            : this(null)
        {
        }

        public Inventory(IEnumerable<KeyValuePair<string, decimal>> initialQuantities)
        {
            quantities = new Dictionary<string, decimal>(StringComparer.Ordinal);
            if (initialQuantities == null)
            {
                return;
            }

            foreach (var entry in initialQuantities)
            {
                AddGram(entry.Key, entry.Value);
            }
        }

        public IReadOnlyDictionary<string, decimal> Quantities
        {
            get
            {
                return new ReadOnlyDictionary<string, decimal>(
                    new Dictionary<string, decimal>(quantities, StringComparer.Ordinal));
            }
        }

        public decimal GetGram(string itemId)
        {
            RequireItemId(itemId);
            decimal quantity;
            return quantities.TryGetValue(itemId, out quantity) ? quantity : 0m;
        }

        public void AddGram(string itemId, decimal gram)
        {
            RequireItemId(itemId);
            RequirePositive(gram, nameof(gram));

            decimal current;
            quantities.TryGetValue(itemId, out current);
            quantities[itemId] = current + gram;
        }

        /// <summary>Removes an exact amount; returns false without mutation if unavailable.</summary>
        public bool RemoveGram(string itemId, decimal gram)
        {
            RequireItemId(itemId);
            RequirePositive(gram, nameof(gram));

            decimal current;
            if (!quantities.TryGetValue(itemId, out current) || current < gram)
            {
                return false;
            }

            var remaining = current - gram;
            if (remaining == 0m)
            {
                quantities.Remove(itemId);
            }
            else
            {
                quantities[itemId] = remaining;
            }

            return true;
        }

        /// <summary>
        /// Reserves an exact portion for a reaction. The portion is removed from this
        /// inventory only when the full requested amount is available.
        /// </summary>
        public bool SplitGram(string itemId, decimal gram, out decimal splitGram)
        {
            if (RemoveGram(itemId, gram))
            {
                splitGram = gram;
                return true;
            }

            splitGram = 0m;
            return false;
        }

        private static void RequireItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("A non-empty item id is required.", nameof(itemId));
            }
        }

        private static void RequirePositive(decimal value, string parameterName)
        {
            if (value <= 0m)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }
        }
    }
}
