using System;
using System.Collections.Generic;
using System.Linq;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;

namespace DeliveriesProMax.Features
{
    /// <summary>
    /// Tracks completed deliveries and maintains a history buffer.
    /// </summary>
    public class DeliveryHistoryTracker
    {
        /// <summary>
        /// Called when a delivery is completed in the game.
        /// Extracts data from the game's DeliveryInstance and stores it.
        /// </summary>
        /// <param name="shopId">The shop identifier</param>
        /// <param name="shopName">Human-readable shop name</param>
        /// <param name="items">List of items in the delivery</param>
        /// <param name="totalCost">Total cost of the delivery</param>
        public void RecordDelivery(string shopId, string shopName, List<SavedDeliveryItem> items, float totalCost)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            if (saveData == null)
            {
                Mod.Logger.Warning("Cannot record delivery: save data not available");
                return;
            }

            var delivery = new SavedDelivery
            {
                ShopId = shopId,
                ShopName = shopName,
                Items = items,
                TotalCost = totalCost,
                CompletedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Check if this matches an existing favorite — preserve the flag
            var existingFavorite = saveData.DeliveryHistory
                .FirstOrDefault(d => d.IsFavorite && d.ShopId == shopId && ItemsMatch(d.Items, items));
            if (existingFavorite != null)
            {
                delivery.IsFavorite = true;
                delivery.IsRecurring = existingFavorite.IsRecurring;
            }

            saveData.DeliveryHistory.Insert(0, delivery);

            // Trim to configured retention count
            var maxCount = Config.HistoryRetentionCount.Value;
            while (saveData.DeliveryHistory.Count > maxCount)
            {
                var removed = saveData.DeliveryHistory[saveData.DeliveryHistory.Count - 1];
                // Don't remove favorites
                if (removed.IsFavorite)
                {
                    // Find the oldest non-favorite to remove instead
                    var oldestNonFav = saveData.DeliveryHistory
                        .LastOrDefault(d => !d.IsFavorite);
                    if (oldestNonFav != null)
                    {
                        saveData.DeliveryHistory.Remove(oldestNonFav);
                    }
                    else
                    {
                        break; // All entries are favorites, don't remove any
                    }
                }
                else
                {
                    saveData.DeliveryHistory.RemoveAt(saveData.DeliveryHistory.Count - 1);
                }
            }

            Mod.Instance?.SaveData?.SaveAll();
            Mod.Logger.Msg($"Recorded delivery from {shopName}: {items.Count} items, ${totalCost:F2}");
        }

        /// <summary>
        /// Gets the delivery history, optionally filtered to favorites only.
        /// </summary>
        public List<SavedDelivery> GetHistory(bool favoritesOnly = false)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            if (saveData == null) return new List<SavedDelivery>();

            if (favoritesOnly)
                return saveData.DeliveryHistory.Where(d => d.IsFavorite).ToList();

            return saveData.DeliveryHistory.ToList();
        }

        private static bool ItemsMatch(List<SavedDeliveryItem> a, List<SavedDeliveryItem> b)
        {
            if (a.Count != b.Count) return false;
            return a.All(itemA => b.Any(itemB =>
                itemB.ItemId == itemA.ItemId && itemB.Quantity == itemA.Quantity));
        }
    }
}
