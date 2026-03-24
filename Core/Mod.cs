using System;
using MelonLoader;
using DeliveriesProMax.Data;
using DeliveriesProMax.Features;

namespace DeliveriesProMax.Core
{
    public class Mod : MelonMod
    {
        public static Mod? Instance { get; private set; }
        public static MelonLogger.Instance Logger => Instance!.LoggerInstance;

        public DeliveryHistoryTracker? HistoryTracker { get; private set; }
        public FavoriteManager? FavoriteManager { get; private set; }
        public RecurringDeliveryManager? RecurringManager { get; private set; }
        public SaveDataManager? SaveData { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Config.Initialize();

            SaveData = new SaveDataManager();
            HistoryTracker = new DeliveryHistoryTracker();
            FavoriteManager = new FavoriteManager();
            RecurringManager = new RecurringDeliveryManager();

            LoggerInstance.Msg("Deliveries Pro Max v1.0.0 initialized!");
            LoggerInstance.Msg($"  History retention: {Config.HistoryRetentionCount.Value} deliveries");
            LoggerInstance.Msg($"  Recurring interval: {Config.RecurringIntervalSeconds.Value}s");
        }

        public override void OnUpdate()
        {
            RecurringManager?.Update();
        }

        public override void OnDeinitializeMelon()
        {
            SaveData?.SaveAll();
            LoggerInstance.Msg("Deliveries Pro Max unloaded.");
        }
    }
}
