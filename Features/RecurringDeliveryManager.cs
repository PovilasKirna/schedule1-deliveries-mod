using System;
using System.Collections.Generic;
using System.Linq;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using UnityEngine;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Money;

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
                var deliveryManager = GetDeliveryManager();
                if (deliveryManager == null)
                {
                    Mod.Logger.Warning("DeliveryManager not found");
                    return false;
                }

                var shop = deliveryManager.GetShop(delivery.ShopId);
                if (shop == null)
                {
                    Mod.Logger.Warning($"Shop not found: {delivery.ShopId}");
                    return false;
                }

                if (Config.PreventNegativeBalance.Value && !CanPlayerAfford(delivery.TotalCost))
                {
                    Mod.Logger.Msg($"Cannot afford recurring delivery: ${delivery.TotalCost:F2}");
                    return false;
                }

                var activeDelivery = deliveryManager.GetActiveShopDelivery(shop);
                if (activeDelivery != null)
                {
                    return false;
                }

                return PlaceOrderThroughShop(shop, delivery);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Failed to place recurring order: {ex.Message}");
                return false;
            }
        }

        private static DeliveryManager? GetDeliveryManager()
        {
            try
            {
                var managerType = Il2CppInterop.Runtime.Il2CppType.From(typeof(DeliveryManager));
                var instances = UnityEngine.Object.FindObjectsOfType(managerType);
                if (instances != null && instances.Count > 0)
                    return instances[0].TryCast<DeliveryManager>();
            }
            catch { }
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
                        return moneyManager.Balance >= cost;
                }
            }
            catch { }
            return false;
        }

        private static bool PlaceOrderThroughShop(DeliveryShop shop, SavedDelivery delivery)
        {
            try
            {
                var shopInterface = shop.ShopInterface;
                if (shopInterface == null)
                {
                    Mod.Logger.Warning("Shop interface is null");
                    return false;
                }

                shopInterface.Cart.ClearCart();

                foreach (var item in delivery.Items)
                {
                    var listing = shopInterface.GetListing(item.ItemId);
                    if (listing != null)
                    {
                        shopInterface.Cart.AddItem(listing, item.Quantity);
                    }
                    else
                    {
                        Mod.Logger.Warning($"Listing not found for item: {item.ItemId}");
                    }
                }

                shopInterface.ConfirmOrderPressed();
                return true;
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"PlaceOrderThroughGame error: {ex.Message}");
                return false;
            }
        }
    }
}
