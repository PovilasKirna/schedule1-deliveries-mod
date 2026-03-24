using System;
using HarmonyLib;
using DeliveriesProMax.Core;
using DeliveriesProMax.UI;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Harmony patches for the DeliveryApp phone UI.
    /// </summary>
    [HarmonyPatch]
    public static class DeliveryAppPatches
    {
        private static bool _uiInjected = false;

        [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(DeliveryApp __instance)
        {
            try
            {
                if (_uiInjected) return;

                Mod.Logger.Msg("Injecting Deliveries Pro Max UI into DeliveryApp...");
                DeliveryAppUI.InjectUI(__instance);
                _uiInjected = true;
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"DeliveryApp.Awake patch error: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.CreateDeliveryStatusDisplay))]
        [HarmonyPostfix]
        public static void CreateDeliveryStatusDisplay_Postfix(
            DeliveryApp __instance,
            DeliveryInstance deliveryInstance)
        {
            try
            {
                DeliveryAppUI.EnhanceStatusDisplay(__instance, deliveryInstance);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"CreateDeliveryStatusDisplay patch error: {ex.Message}");
            }
        }
    }
}
