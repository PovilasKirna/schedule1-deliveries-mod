using System;
using System.Collections.Generic;
using HarmonyLib;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Harmony patches for DeliveryManager — hooks delivery send and receipt for history tracking.
    /// </summary>
    [HarmonyPatch]
    public static class DeliveryManagerPatches
    {
        /// <summary>
        /// Hook SendDelivery to capture item data when a delivery is placed.
        /// Parameter name "delivery" matches the original method signature.
        /// </summary>
        [HarmonyPatch(typeof(DeliveryManager), nameof(DeliveryManager.SendDelivery))]
        [HarmonyPostfix]
        public static void SendDelivery_Postfix(
            DeliveryManager __instance,
            DeliveryInstance delivery)
        {
            try
            {
                if (delivery == null) return;

                string storeName = delivery.StoreName ?? "Unknown Shop";
                var items = new List<SavedDeliveryItem>();
                float totalCost = 0f;

                // Extract items from DeliveryInstance.Items (StringIntPair[])
                var instanceItems = delivery.Items;
                if (instanceItems != null)
                {
                    for (int i = 0; i < instanceItems.Length; i++)
                    {
                        var pair = instanceItems[i];
                        if (pair != null)
                        {
                            items.Add(new SavedDeliveryItem
                            {
                                ItemId = pair.String ?? "",
                                ItemName = pair.String ?? "",
                                Quantity = pair.Int
                            });
                        }
                    }
                }

                // Try to compute total cost by looking up shop listing prices
                totalCost = TryComputeCost(storeName, items);

                _pendingDeliveries[delivery.DeliveryID ?? ""] = new PendingDeliveryInfo
                {
                    StoreName = storeName,
                    Items = items,
                    TotalCost = totalCost
                };

                Mod.Logger.Msg($"Tracking delivery from {storeName}: {items.Count} items, cost=${totalCost:F2}");
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"SendDelivery_Postfix error: {ex.Message}");
            }
        }

        private static readonly Dictionary<string, PendingDeliveryInfo> _pendingDeliveries = new();

        private class PendingDeliveryInfo
        {
            public string StoreName { get; set; } = "";
            public List<SavedDeliveryItem> Items { get; set; } = new();
            public float TotalCost { get; set; }
        }

        /// <summary>
        /// Hook RecordDeliveryReceipt_Server to record the delivery in history when completed.
        /// Uses pending delivery data captured at send time when available, falls back to
        /// extracting from the receipt and computing cost from shop listings.
        /// Parameter name "receipt" matches the original method signature.
        /// </summary>
        [HarmonyPatch(typeof(DeliveryManager), nameof(DeliveryManager.RecordDeliveryReceipt_Server))]
        [HarmonyPostfix]
        public static void RecordDeliveryReceipt_Postfix(
            DeliveryManager __instance,
            DeliveryReceipt receipt)
        {
            try
            {
                if (receipt == null) return;

                string storeName = receipt.StoreName ?? "Unknown Shop";
                string shopId = storeName;

                // Try to use pending delivery data captured at send time (has pre-computed cost).
                // DeliveryReceipt may not have DeliveryID, so we match by StoreName + item comparison.
                PendingDeliveryInfo? matchedPending = null;
                string? matchedKey = null;
                foreach (var kvp in _pendingDeliveries)
                {
                    if (kvp.Value.StoreName == storeName)
                    {
                        matchedPending = kvp.Value;
                        matchedKey = kvp.Key;
                        break;
                    }
                }
                if (matchedPending != null && matchedKey != null)
                {
                    _pendingDeliveries.Remove(matchedKey);
                    Mod.Instance?.HistoryTracker?.RecordDelivery(
                        matchedPending.StoreName, matchedPending.StoreName, matchedPending.Items, matchedPending.TotalCost);
                    Mod.Logger.Msg($"Recorded delivery from pending data: {matchedPending.StoreName}");
                    return;
                }

                // Fallback: extract items from receipt directly
                var items = new List<SavedDeliveryItem>();
                float totalCost = 0f;

                var receiptItems = receipt.Items;
                if (receiptItems != null)
                {
                    for (int i = 0; i < receiptItems.Length; i++)
                    {
                        var pair = receiptItems[i];
                        if (pair != null)
                        {
                            items.Add(new SavedDeliveryItem
                            {
                                ItemId = pair.String ?? "",
                                ItemName = pair.String ?? "",
                                Quantity = pair.Int
                            });
                        }
                    }
                }

                totalCost = TryComputeCost(storeName, items);
                Mod.Instance?.HistoryTracker?.RecordDelivery(shopId, storeName, items, totalCost);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"RecordDeliveryReceipt_Postfix error: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to compute the total cost of items by looking up ShopListing prices
        /// from the DeliveryApp's shop listings.
        /// </summary>
        private static float TryComputeCost(string storeName, List<SavedDeliveryItem> items)
        {
            try
            {
                var deliveryApp = GameRefs.FindDeliveryApp();
                if (deliveryApp == null) return 0f;

                var shop = deliveryApp.GetShop(storeName);
                if (shop == null) return 0f;

                var listings = shop.listingEntries;
                if (listings == null) return 0f;

                float total = 0f;
                foreach (var item in items)
                {
                    for (int i = 0; i < listings.Count; i++)
                    {
                        var entry = listings[i];
                        var listing = entry.MatchingListing;
                        if (listing != null && listing.name == item.ItemId)
                        {
                            float unitPrice = listing.Price;
                            item.UnitPrice = unitPrice;
                            total += unitPrice * item.Quantity;
                            break;
                        }
                    }
                }
                return total;
            }
            catch (Exception ex)
            {
                Mod.Logger.Warning($"Could not compute delivery cost: {ex.Message}");
                return 0f;
            }
        }
    }
}
