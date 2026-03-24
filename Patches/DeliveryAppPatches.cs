using System;
using HarmonyLib;
using DeliveriesProMax.Core;
using DeliveriesProMax.UI;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using UnityEngine;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Harmony patches for the DeliveryApp phone UI.
    /// </summary>
    [HarmonyPatch]
    public static class DeliveryAppPatches
    {
        [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(DeliveryApp __instance)
        {
            try
            {
                // Check if we already injected UI into this specific instance
                // by looking for our panel. This handles scene reloads properly.
                var existing = __instance.transform.Find("DeliveriesProMax_HistoryPanel");
                if (existing != null) return;

                Mod.Logger.Msg("Injecting Deliveries Pro Max UI into DeliveryApp...");
                DeliveryAppUI.InjectUI(__instance);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"DeliveryApp.Awake patch error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hook CreateDeliveryStatusDisplay to enhance status displays.
        /// Parameter name "instance" matches the original method signature.
        /// </summary>
        [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.CreateDeliveryStatusDisplay))]
        [HarmonyPostfix]
        public static void CreateDeliveryStatusDisplay_Postfix(
            DeliveryApp __instance,
            DeliveryInstance instance)
        {
            try
            {
                DeliveryAppUI.EnhanceStatusDisplay(__instance, instance);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"CreateDeliveryStatusDisplay patch error: {ex.Message}");
            }
        }
    }
}
