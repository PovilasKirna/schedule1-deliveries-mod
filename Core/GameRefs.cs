using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;

namespace DeliveriesProMax.Core
{
    /// <summary>
    /// Centralized helpers for finding singleton-like game objects at runtime.
    /// Avoids duplicating FindObjectsOfType + TryCast boilerplate across the codebase.
    /// </summary>
    public static class GameRefs
    {
        /// <summary>
        /// Finds the first active DeliveryApp instance in the scene.
        /// </summary>
        public static DeliveryApp? FindDeliveryApp()
        {
            try
            {
                var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(typeof(DeliveryApp));
                var instances = UnityEngine.Object.FindObjectsOfType(il2cppType);
                if (instances != null && instances.Count > 0)
                    return instances[0].TryCast<DeliveryApp>();
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Finds the first active MoneyManager instance in the scene.
        /// </summary>
        public static MoneyManager? FindMoneyManager()
        {
            try
            {
                var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(typeof(MoneyManager));
                var instances = UnityEngine.Object.FindObjectsOfType(il2cppType);
                if (instances != null && instances.Count > 0)
                    return instances[0].TryCast<MoneyManager>();
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Checks if the player can afford a given cost (cash + online balance).
        /// Returns false if MoneyManager cannot be found.
        /// </summary>
        public static bool CanPlayerAfford(float cost)
        {
            var mm = FindMoneyManager();
            if (mm == null) return false;
            return (mm.cashBalance + mm.onlineBalance) >= cost;
        }
    }
}
