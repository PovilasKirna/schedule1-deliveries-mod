using System;
using System.Collections.Generic;

namespace DeliveriesProMax.Data
{
    /// <summary>
    /// Represents a saved delivery order that can be re-purchased.
    /// </summary>
    [Serializable]
    public class SavedDelivery
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ShopId { get; set; } = "";
        public string ShopName { get; set; } = "";
        public List<SavedDeliveryItem> Items { get; set; } = new();
        public float TotalCost { get; set; }
        public long CompletedTimestamp { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsRecurring { get; set; }
    }

    [Serializable]
    public class SavedDeliveryItem
    {
        public string ItemId { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public float UnitPrice { get; set; }
    }

    /// <summary>
    /// Root save data for the mod, keyed per game save slot.
    /// </summary>
    [Serializable]
    public class ModSaveData
    {
        public List<SavedDelivery> DeliveryHistory { get; set; } = new();
        public List<string> FavoriteIds { get; set; } = new();
        public List<string> RecurringIds { get; set; } = new();
    }
}
