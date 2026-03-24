using System;
using System.Collections.Generic;
using System.Linq;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using UnityEngine;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Money;

namespace DeliveriesProMax.Features
{
    /// <summary>
    /// Manages recurring (auto-reorder) deliveries.
    /// Checks periodically if any recurring deliveries should be placed.
    /// </summary>
    public class RecurringDeliveryManager
    {
        private float _nextCheckTime;
        private readonly Dictionary<string, float> _cooldowns = new();

        public void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + Config.RecurringIntervalSeconds.Value;

            ProcessRecurringDeliveries();
        }

        /// <summary>
        /// Toggles recurring status for a delivery.
        /// </summary>
        public bool ToggleRecurring(string deliveryId)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            if (saveData == null) return false;

            var delivery = saveData.DeliveryHistory.FirstOrDefault(d => d.Id == deliveryId);
            if (delivery == null) return false;

            delivery.IsRecurring = !delivery.IsRecurring;

            if (delivery.IsRecurring)
            {
                if (!saveData.RecurringIds.Contains(deliveryId))
                    saveData.RecurringIds.Add(deliveryId);
                _cooldowns.Remove(deliveryId);
            }
            else
            {
                saveData.RecurringIds.Remove(deliveryId);
                _cooldowns.Remove(deliveryId);
            }

            Mod.Instance?.SaveData?.SaveAll();
            Mod.Logger.Msg($"Delivery {deliveryId} recurring: {delivery.IsRecurring}");
            return delivery.IsRecurring;
        }

        /// <summary>
        /// Checks if a delivery is set to recurring.
        /// </summary>
        public bool IsRecurring(string deliveryId)
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            return saveData?.RecurringIds.Contains(deliveryId) ?? false;
        }

        private void ProcessRecurringDeliveries()
        {
            var saveData = Mod.Instance?.SaveData?.Data;
            if (saveData == null) return;

            foreach (var deliveryId in saveData.RecurringIds.ToList())
            {
                var delivery = saveData.DeliveryHistory.FirstOrDefault(d => d.Id == deliveryId);
                if (delivery == null || !delivery.IsRecurring)
                {
                    saveData.RecurringIds.Remove(deliveryId);
                    continue;
                }

                if (_cooldowns.TryGetValue(deliveryId, out var cooldownEnd) && Time.time < cooldownEnd)
                    continue;

                if (TryPlaceOrder(delivery))
                {
                    _cooldowns[deliveryId] = Time.time + Config.RecurringWindowSeconds.Value;
                    Mod.Logger.Msg($"Recurring delivery placed: {delivery.ShopName}");
                }
            }
        }

        private bool TryPlaceOrder(SavedDelivery delivery)
        {
            try
            {
                var deliveryApp = GetDeliveryApp();
                if (deliveryApp == null)
                {
                    Mod.Logger.Warning("DeliveryApp not found");
                    return false;
                }

                var deliveryShop = FindDeliveryShop(deliveryApp, delivery.ShopId);
                if (deliveryShop == null)
                {
                    Mod.Logger.Warning($"DeliveryShop not found for: {delivery.ShopId}");
                    return false;
                }

                if (deliveryShop.HasActiveDelivery())
                {
                    return false;
                }

                if (Config.PreventNegativeBalance.Value && !CanPlayerAfford(delivery.TotalCost))
                {
                    Mod.Logger.Msg($"Cannot afford recurring delivery: ${delivery.TotalCost:F2}");
                    return false;
                }

                return PlaceOrderThroughShop(deliveryShop, delivery);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Failed to place recurring order: {ex.Message}");
                return false;
            }
        }

        private static DeliveryApp? GetDeliveryApp()
        {
            try
            {
                var appType = Il2CppInterop.Runtime.Il2CppType.From(typeof(DeliveryApp));
                var instances = UnityEngine.Object.FindObjectsOfType(appType);
                if (instances != null && instances.Count > 0)
                    return instances[0].TryCast<DeliveryApp>();
            }
            catch { }
            return null;
        }

        private static DeliveryShop? FindDeliveryShop(DeliveryApp app, string shopId)
        {
            var shops = app.deliveryShops;
            if (shops == null) return null;

            for (int i = 0; i < shops.Count; i++)
            {
                var shop = shops[i];
                if (shop.MatchingShopInterfaceName == shopId)
                    return shop;
            }
            return null;
        }

        private static bool CanPlayerAfford(float cost)
        {
            try
            {
                var moneyType = Il2CppInterop.Runtime.Il2CppType.From(typeof(MoneyManager));
                var instances = UnityEngine.Object.FindObjectsOfType(moneyType);
                if (instances != null && instances.Count > 0)
                {
                    var moneyManager = instances[0].TryCast<MoneyManager>();
                    if (moneyManager != null)
                        return (moneyManager.cashBalance + moneyManager.onlineBalance) >= cost;
                }
            }
            catch { }
            return false;
        }

        private static bool PlaceOrderThroughShop(DeliveryShop deliveryShop, SavedDelivery delivery)
        {
            try
            {
                var shopInterface = deliveryShop.MatchingShop;
                if (shopInterface == null)
                {
                    Mod.Logger.Warning("MatchingShop (ShopInterface) is null");
                    return false;
                }

                // Reset the cart first
                deliveryShop.ResetCart();

                // Find listing entries and set quantities
                var listingEntries = deliveryShop.listingEntries;
                if (listingEntries != null)
                {
                    foreach (var item in delivery.Items)
                    {
                        for (int i = 0; i < listingEntries.Count; i++)
                        {
                            var entry = listingEntries[i];
                            var listing = entry.MatchingListing;
                            if (listing != null && listing.name == item.ItemId)
                            {
                                entry.SetQuantity(item.Quantity, true);
                                break;
                            }
                        }
                    }
                }

                // Submit the order through the DeliveryShop
                deliveryShop.OrderPressed();
                return true;
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"PlaceOrderThroughShop error: {ex.Message}");
                return false;
            }
        }
    }
}
