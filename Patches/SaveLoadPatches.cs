using System;
using HarmonyLib;
using DeliveriesProMax.Core;
using Il2CppScheduleOne.Persistence;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Hooks into the game's save/load to initialize our per-slot save data.
    /// </summary>
    [HarmonyPatch]
    public static class SaveLoadPatches
    {
        [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.StartGame))]
        [HarmonyPostfix]
        public static void StartGame_Postfix(
            LoadManager __instance,
            SaveInfo saveInfo)
        {
            try
            {
                string saveName = "default";

                if (saveInfo != null)
                {
                    saveName = saveInfo.OrganisationName ?? $"save_{saveInfo.SaveSlotNumber}";
                }

                Mod.Logger.Msg($"Game loading detected, save slot: {saveName}");
                Mod.Instance?.SaveData?.SetSaveSlot(saveName);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Save slot detection error: {ex.Message}");
                Mod.Instance?.SaveData?.SetSaveSlot("default");
            }
        }
    }
}
