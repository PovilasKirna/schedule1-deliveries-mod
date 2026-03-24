using System;
using HarmonyLib;
using DeliveriesProMax.Core;
using ScheduleOne.Persistence;

namespace DeliveriesProMax.Patches
{
    /// <summary>
    /// Hooks into the game's save/load to initialize our per-slot save data.
    /// </summary>
    [HarmonyPatch]
    public static class SaveLoadPatches
    {
        [HarmonyPatch(typeof(LoadManager), "CanStartLoading")]
        [HarmonyPostfix]
        public static void CanStartLoading_Postfix(
            LoadManager __instance,
            bool __result)
        {
            if (!__result) return;

            try
            {
                string saveName = "default";

                try
                {
                    var activeSaveInfo = LoadManager.ActiveSaveInfo;
                    if (activeSaveInfo != null)
                    {
                        saveName = activeSaveInfo.SaveName ?? "default";
                    }
                }
                catch
                {
                    try
                    {
                        saveName = $"save_{__instance.SaveSlotNumber}";
                    }
                    catch { }
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
