using System;
using System.Collections.Generic;
using HarmonyLib;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using ScheduleOne.Delivery;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Harmony patches for DeliveryManager — hooks delivery completion for history tracking.
    /// </summary>
    [HarmonyPatch]
    public static class DeliveryManagerPatches
    {
        [HarmonyPatch(typeof(DeliveryManager), "DeliveryCompleted")]
        [HarmonyPostfix]
        public static void DeliveryCompleted_Postfix(
            DeliveryManager __instance,
            DeliveryInstance deliveryInstance)
        {
            try
            {
                if (deliveryInstance == null) return;

                var receipt = deliveryInstance.GetReceipt();
                if (receipt == null) return;

                string shopId = "";
                string shopName = "Unknown Shop";

                var shops = __instance.deliveryShops;
                if (shops != null)
                {
                    for (int i = 0; i < shops.Count; i++)
                    {
                        var shop = shops[i];
                        var activeDelivery = __instance.GetActiveShopDelivery(shop);
                        if (activeDelivery != null && activeDelivery.DeliveryID == deliveryInstance.DeliveryID)
                        {
                            shopId = shop.ShopName ?? "";
                            shopName = shop.ShopName ?? "Unknown Shop";
                            break;
                        }
                    }
                }

                var items = new List<SavedDeliveryItem>();
                // TODO: Extract items from receipt once we verify the DeliveryReceipt fields
                // at runtime via the decompiled IL2CPP types
                float totalCost = 0f;

                Mod.Instance?.HistoryTracker?.RecordDelivery(shopId, shopName, items, totalCost);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"DeliveryCompleted_Postfix error: {ex.Message}");
            }
        }
    }
}
