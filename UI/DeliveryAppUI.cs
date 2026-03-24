using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using ScheduleOne.Delivery;
using ScheduleOne.Money;
using ScheduleOne.UI.Phone.Delivery;

namespace DeliveriesProMax.UI
{
    /// <summary>
    /// Handles injection and management of custom UI elements in the DeliveryApp.
    /// </summary>
    public static class DeliveryAppUI
    {
        private static GameObject? _historyPanel;
        private static GameObject? _historyButton;
        private static Transform? _historyContentParent;
        private static readonly List<GameObject> _historyEntries = new();

        private static readonly Color ButtonNormal = new(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color ButtonHighlight = new(0.3f, 0.3f, 0.3f, 0.9f);
        private static readonly Color FavoriteColor = new(1f, 0.84f, 0f, 1f);
        private static readonly Color RecurringColor = new(0.3f, 0.69f, 0.31f, 1f);
        private static readonly Color TextColor = new(0.9f, 0.9f, 0.9f, 1f);

        public static void InjectUI(DeliveryApp deliveryApp)
        {
            try
            {
                var appTransform = deliveryApp.transform;
                CreateHistoryButton(appTransform);
                CreateHistoryPanel(appTransform);
                Mod.Logger.Msg("UI injection complete.");
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"InjectUI error: {ex.Message}");
            }
        }

        public static void EnhanceStatusDisplay(DeliveryApp deliveryApp, DeliveryInstance deliveryInstance)
        {
            try
            {
                var statusContainer = deliveryApp.StatusDisplayContainer;
                if (statusContainer == null || statusContainer.childCount == 0) return;

                var latestDisplay = statusContainer.GetChild(statusContainer.childCount - 1);
                if (latestDisplay == null) return;

                AddActionButtons(latestDisplay.gameObject, deliveryInstance);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"EnhanceStatusDisplay error: {ex.Message}");
            }
        }

        public static void RefreshHistoryPanel()
        {
            if (_historyContentParent == null) return;

            foreach (var entry in _historyEntries)
            {
                if (entry != null) UnityEngine.Object.Destroy(entry);
            }
            _historyEntries.Clear();

            var history = Mod.Instance?.HistoryTracker?.GetHistory() ?? new List<SavedDelivery>();

            foreach (var delivery in history)
            {
                CreateHistoryEntry(_historyContentParent, delivery);
            }
        }

        // ----------------------------------------------------------------
        // Private UI creation
        // ----------------------------------------------------------------

        private static void CreateHistoryButton(Transform parent)
        {
            _historyButton = CreateButton(parent, "HistoryButton", "History",
                new Vector2(0, -30), new Vector2(100, 30));

            var button = _historyButton.GetComponent<Button>();
            button.onClick.AddListener((UnityEngine.Events.UnityAction)ToggleHistoryPanel);
        }

        private static void CreateHistoryPanel(Transform parent)
        {
            _historyPanel = new GameObject("DeliveriesProMax_HistoryPanel");
            _historyPanel.transform.SetParent(parent, false);

            var rect = _historyPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -50);

            var bg = _historyPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            var layout = _historyPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var scrollObj = new GameObject("ScrollContent");
            scrollObj.transform.SetParent(_historyPanel.transform, false);
            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            _historyContentParent = scrollObj.transform;

            CreateText(scrollObj.transform, "HistoryHeader", "Delivery History", 16, TextAlignmentOptions.Center);

            _historyPanel.SetActive(false);
        }

        private static void ToggleHistoryPanel()
        {
            if (_historyPanel == null) return;
            bool show = !_historyPanel.activeSelf;
            _historyPanel.SetActive(show);
            if (show) RefreshHistoryPanel();
        }

        private static void AddActionButtons(GameObject displayObj, DeliveryInstance deliveryInstance)
        {
            // Placeholder — buttons are added to history entries.
            // For active deliveries we could add a "track" indicator here later.
        }

        private static void CreateHistoryEntry(Transform parent, SavedDelivery delivery)
        {
            var entryObj = new GameObject($"HistoryEntry_{delivery.Id}");
            entryObj.transform.SetParent(parent, false);

            var rect = entryObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            var bg = entryObj.AddComponent<Image>();
            bg.color = delivery.IsFavorite
                ? new Color(0.15f, 0.15f, 0.1f, 0.9f)
                : new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var hlayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 8;
            hlayout.padding = new RectOffset(8, 8, 5, 5);
            hlayout.childForceExpandWidth = false;
            hlayout.childForceExpandHeight = true;

            var infoText = $"<b>{delivery.ShopName}</b>\n{delivery.Items.Count} items - ${delivery.TotalCost:F2}";
            var textObj = CreateText(entryObj.transform, "Info", infoText, 11, TextAlignmentOptions.Left);
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;

            // ---- Favorite button ----
            var favButton = CreateSmallButton(entryObj.transform, "FavBtn",
                delivery.IsFavorite ? "\u2605" : "\u2606",
                delivery.IsFavorite ? FavoriteColor : TextColor);
            var capturedDelivery = delivery;
            favButton.GetComponent<Button>().onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                var isFav = Mod.Instance?.FavoriteManager?.ToggleFavorite(capturedDelivery.Id) ?? false;
                var t = favButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = isFav ? "\u2605" : "\u2606"; t.color = isFav ? FavoriteColor : TextColor; }
            }));

            // ---- Reorder button ----
            var reorderButton = CreateSmallButton(entryObj.transform, "ReorderBtn", "\u21BB", TextColor);
            reorderButton.GetComponent<Button>().onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                QuickReorder(capturedDelivery);
            }));

            // ---- Recurring toggle ----
            var recurButton = CreateSmallButton(entryObj.transform, "RecurBtn",
                delivery.IsRecurring ? "\u27F3" : "\u25CB",
                delivery.IsRecurring ? RecurringColor : TextColor);
            recurButton.GetComponent<Button>().onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                var isRecur = Mod.Instance?.RecurringManager?.ToggleRecurring(capturedDelivery.Id) ?? false;
                var t = recurButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = isRecur ? "\u27F3" : "\u25CB"; t.color = isRecur ? RecurringColor : TextColor; }
            }));

            _historyEntries.Add(entryObj);
        }

        // ----------------------------------------------------------------
        // Quick reorder logic (called from UI button)
        // ----------------------------------------------------------------

        private static void QuickReorder(SavedDelivery delivery)
        {
            try
            {
                Mod.Logger.Msg($"Quick reorder: {delivery.ShopName}");

                var managerType = Il2CppInterop.Runtime.Il2CppType.From(typeof(DeliveryManager));
                var managers = UnityEngine.Object.FindObjectsOfType(managerType);
                if (managers == null || managers.Count == 0) return;

                var manager = managers[0].TryCast<DeliveryManager>();
                if (manager == null) return;

                var shop = manager.GetShop(delivery.ShopId);
                if (shop == null) { Mod.Logger.Warning($"Shop not found: {delivery.ShopId}"); return; }

                if (manager.GetActiveShopDelivery(shop) != null)
                { Mod.Logger.Msg("Shop already has an active delivery"); return; }

                if (Config.PreventNegativeBalance.Value)
                {
                    var moneyType = Il2CppInterop.Runtime.Il2CppType.From(typeof(MoneyManager));
                    var moneyManagers = UnityEngine.Object.FindObjectsOfType(moneyType);
                    if (moneyManagers != null && moneyManagers.Count > 0)
                    {
                        var mm = moneyManagers[0].TryCast<MoneyManager>();
                        if (mm != null && mm.Balance < delivery.TotalCost)
                        { Mod.Logger.Msg($"Insufficient funds: ${mm.Balance:F2} < ${delivery.TotalCost:F2}"); return; }
                    }
                }

                var shopInterface = shop.ShopInterface;
                if (shopInterface == null) { Mod.Logger.Warning("ShopInterface is null"); return; }

                shopInterface.Cart.ClearCart();
                foreach (var item in delivery.Items)
                {
                    var listing = shopInterface.GetListing(item.ItemId);
                    if (listing != null) shopInterface.Cart.AddItem(listing, item.Quantity);
                }
                shopInterface.ConfirmOrderPressed();
                Mod.Logger.Msg($"Reorder placed for {delivery.ShopName}!");
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"QuickReorder error: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // UI helpers
        // ----------------------------------------------------------------

        private static GameObject CreateButton(Transform parent, string name, string text,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = ButtonNormal;
            var btn = go.AddComponent<Button>();
            var c = btn.colors; c.normalColor = ButtonNormal; c.highlightedColor = ButtonHighlight;
            c.pressedColor = new Color(0.15f, 0.15f, 0.15f, 0.9f); btn.colors = c;
            CreateText(go.transform, $"{name}_Text", text, 12, TextAlignmentOptions.Center);
            return go;
        }

        private static GameObject CreateSmallButton(Transform parent, string name, string text, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 30; le.preferredHeight = 30;
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            go.AddComponent<Button>();
            var tgo = CreateText(go.transform, $"{name}_Text", text, 16, TextAlignmentOptions.Center);
            var tmp = tgo.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = textColor;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.alignment = alignment;
            tmp.color = TextColor; tmp.enableWordWrapping = true; tmp.overflowMode = TextOverflowModes.Ellipsis;
            return go;
        }
    }
}
