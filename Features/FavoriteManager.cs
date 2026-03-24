using System.Linq;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;

namespace DeliveriesProMax.Features
{
    /// <summary>
    /// Manages favorite (pinned) deliveries.
    /// </summary>
    public class FavoriteManager
    {
        /// <summary>
        /// Toggles the favorite status of a delivery.
        /// </summary>
        public bool ToggleFavorite(string deliveryId)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            if (saveData == null) return false;

            var delivery = saveData.DeliveryHistory.FirstOrDefault(d => d.Id == deliveryId);
            if (delivery == null) return false;

            delivery.IsFavorite = !delivery.IsFavorite;

            // Update the favorites list
            if (delivery.IsFavorite)
            {
                if (!saveData.FavoriteIds.Contains(deliveryId))
                    saveData.FavoriteIds.Add(deliveryId);
            }
            else
            {
                saveData.FavoriteIds.Remove(deliveryId);
                // Also disable recurring if unfavorited
                delivery.IsRecurring = false;
                saveData.RecurringIds.Remove(deliveryId);
            }

            Mod.Instance?.SaveData?.SaveAll();
            Mod.Logger.Msg($"Delivery {deliveryId} favorite: {delivery.IsFavorite}");
            return delivery.IsFavorite;
        }

        /// <summary>
        /// Checks if a delivery is favorited.
        /// </summary>
        public bool IsFavorite(string deliveryId)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            return saveData?.FavoriteIds.Contains(deliveryId) ?? false;
        }
    }
}
