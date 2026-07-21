using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Presentation
{
    /// <summary>An amount temporarily moved from inventory to the experiment bench.</summary>
    public sealed class InventoryReservation
    {
        internal InventoryReservation(string reservationId, string itemId, decimal amountGram)
        {
            ReservationId = reservationId;
            ItemId = itemId;
            AmountGram = amountGram;
        }

        public string ReservationId { get; private set; }
        public string ItemId { get; private set; }
        public decimal AmountGram { get; private set; }
    }

    /// <summary>
    /// Moves exact game-gram portions to a bench reservation. Reservations are removed
    /// from inventory while held, restored on cancellation, and become consumed only
    /// once the experiment commits.
    /// </summary>
    public sealed class InventoryGramSplitter
    {
        private readonly object syncRoot = new object();
        private readonly Inventory inventory;
        private readonly Dictionary<string, InventoryReservation> reservations = new Dictionary<string, InventoryReservation>(StringComparer.Ordinal);

        public InventoryGramSplitter(Inventory inventory)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public IReadOnlyCollection<InventoryReservation> Reservations
        {
            get
            {
                lock (syncRoot) return new List<InventoryReservation>(reservations.Values).AsReadOnly();
            }
        }

        public string Reserve(string itemId, decimal amountGram)
        {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("An item id is required.", nameof(itemId));
            if (amountGram <= 0m) throw new ArgumentOutOfRangeException(nameof(amountGram), "Amount must be greater than zero.");
            lock (syncRoot)
            {
                decimal reserved;
                if (!inventory.SplitGram(itemId, amountGram, out reserved)) return null;
                var reservationId = Guid.NewGuid().ToString("N");
                reservations.Add(reservationId, new InventoryReservation(reservationId, itemId, reserved));
                return reservationId;
            }
        }

        public bool CancelReservation(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId)) return false;
            lock (syncRoot)
            {
                InventoryReservation reservation;
                if (!reservations.TryGetValue(reservationId, out reservation)) return false;
                inventory.AddGram(reservation.ItemId, reservation.AmountGram);
                reservations.Remove(reservationId);
                return true;
            }
        }

        /// <summary>Consumes all supplied reservations. It does not mutate inventory again.</summary>
        public bool CommitReservations(IEnumerable<string> reservationIds)
        {
            if (reservationIds == null) throw new ArgumentNullException(nameof(reservationIds));
            lock (syncRoot)
            {
                var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var reservationId in reservationIds)
                {
                    if (string.IsNullOrWhiteSpace(reservationId) || !uniqueIds.Add(reservationId) || !reservations.ContainsKey(reservationId)) return false;
                }

                if (uniqueIds.Count == 0) return false;
                foreach (var reservationId in uniqueIds) reservations.Remove(reservationId);
                return true;
            }
        }
    }
}
