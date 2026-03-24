using System;
using System.Collections.Generic;
using HarmonyLib;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Harmony patches for DeliveryManager — hooks delivery completion for history tracking.
    /// </summary>
    [HarmonyPatch]
    public static class DeliveryManagerPatches
    {
        [HarmonyPatch(typeof(DeliveryManager), nameof(DeliveryManager.SendDelivery))]
        [HarmonyPostfix]
        public static void SendDelivery_Postfix(
            DeliveryManager __instance,
            DeliveryInstance deliveryInstance)
        {
            try
            {
                if (deliveryInstance == null) return;

                // Store delivery info for when it completes
                // We hook SendDelivery since this is when items are known
                string storeName = deliveryInstance.StoreName ?? "Unknown Shop";

                // Extract items from the DeliveryInstance.Items (StringIntPair array)
                var items = new List<SavedDeliveryItem>();
                float totalCost = 0f;

                var instanceItems = deliveryInstance.Items;
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

                // Store this for later completion tracking
                _pendingDeliveries[deliveryInstance.DeliveryID ?? ""] = new PendingDeliveryInfo
                {
                    StoreName = storeName,
                    Items = items,
                    TotalCost = totalCost
                };

                Mod.Logger.Msg($"Tracking delivery from {storeName}: {items.Count} items");
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

                Mod.Instance?.HistoryTracker?.RecordDelivery(shopId, storeName, items, totalCost);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"RecordDeliveryReceipt_Postfix error: {ex.Message}");
            }
        }
    }
}
