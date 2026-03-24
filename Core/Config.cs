using MelonLoader;

namespace DeliveriesProMax.Core
{
    public static class Config
    {
        private static MelonPreferences_Category? _category;

        public static MelonPreferences_Entry<int> HistoryRetentionCount { get; private set; } = null!;
        public static MelonPreferences_Entry<float> RecurringIntervalSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<float> RecurringWindowSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> PreventNegativeBalance { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MaxDeliveryTimeHours { get; private set; } = null!;

        public static void Initialize()
        {
            _category = MelonPreferences.CreateCategory("DeliveriesProMax", "Deliveries Pro Max");

            HistoryRetentionCount = _category.CreateEntry(
                "HistoryRetentionCount",
                10,
                "History Retention Count",
                "Number of past deliveries to remember");

            RecurringIntervalSeconds = _category.CreateEntry(
                "RecurringIntervalSeconds",
                10f,
                "Recurring Interval (seconds)",
                "How often to check and reorder recurring deliveries");

            RecurringWindowSeconds = _category.CreateEntry(
                "RecurringWindowSeconds",
                10f,
                "Recurring Window (seconds)",
                "Time window after a delivery completes before auto-reordering");

            PreventNegativeBalance = _category.CreateEntry(
                "PreventNegativeBalance",
                true,
                "Prevent Negative Balance",
                "Block recurring orders if they would put you in negative balance");

            MaxDeliveryTimeHours = _category.CreateEntry(
                "MaxDeliveryTimeHours",
                6f,
                "Max Delivery Time (hours)",
                "Cap delivery times to this value (vanilla default is 6h)");
        }
    }
}
