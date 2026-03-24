using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using DeliveriesProMax.Core;
using DeliveriesProMax.Data;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;

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

        private static readonly Color ButtonNormal = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color ButtonHighlight = new(0.32f, 0.32f, 0.32f, 1f);
        private static readonly Color ButtonPressed = new(0.16f, 0.16f, 0.16f, 1f);
        private static readonly Color FavoriteColor = new(1f, 0.84f, 0f, 1f);
        private static readonly Color RecurringColor = new(0.3f, 0.69f, 0.31f, 1f);
        private static readonly Color TextColor = new(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color PanelBg = new(0.08f, 0.08f, 0.08f, 0.97f);
        private static readonly Color EntryBg = new(0.16f, 0.16f, 0.16f, 1f);
        private static readonly Color EntryFavBg = new(0.18f, 0.16f, 0.10f, 1f);

        /// <summary>
        /// Clears stale references to destroyed Unity objects.
        /// Must be called before re-injecting UI (e.g., on scene reload).
        /// </summary>
        private static void ClearStaleReferences()
        {
            _historyEntries.Clear();
            _historyContentParent = null;
            _historyPanel = null;
            _historyButton = null;
        }

        /// <summary>
        /// Checks whether a Unity object reference is still valid (not destroyed).
        /// </summary>
        private static bool IsAlive(UnityEngine.Object? obj)
        {
            // In Il2Cpp/Unity, destroyed objects compare equal to null via the implicit bool operator
            return obj != null;
        }

        public static void InjectUI(DeliveryApp deliveryApp)
        {
            try
            {
                // Clear any stale references from a previous scene
                ClearStaleReferences();

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
            // Placeholder for future enhancement of active delivery status displays
        }

        public static void RefreshHistoryPanel()
        {
            if (!IsAlive(_historyContentParent)) return;

            foreach (var entry in _historyEntries)
            {
                if (IsAlive(entry)) UnityEngine.Object.Destroy(entry);
            }
            _historyEntries.Clear();

            var history = Mod.Instance?.HistoryTracker?.GetHistory() ?? new List<SavedDelivery>();

            if (history.Count == 0)
            {
                var emptyMsg = CreateText(_historyContentParent!, "EmptyMsg",
                    "No delivery history yet.\nPlace an order to get started!",
                    12, TextAlignmentOptions.Center);
                var emptyLayout = emptyMsg.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 60;
                _historyEntries.Add(emptyMsg);
            }
            else
            {
                foreach (var delivery in history)
                {
                    CreateHistoryEntry(_historyContentParent!, delivery);
                }
            }
        }

        // ----------------------------------------------------------------
        // UI creation
        // ----------------------------------------------------------------

        private static void CreateHistoryButton(Transform parent)
        {
            _historyButton = new GameObject("DeliveriesProMax_HistoryButton");
            _historyButton.transform.SetParent(parent, false);

            var rect = _historyButton.AddComponent<RectTransform>();
            // Anchor to bottom-right of the delivery app
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-8f, 8f);
            rect.sizeDelta = new Vector2(90f, 28f);

            var img = _historyButton.AddComponent<Image>();
            img.color = ButtonNormal;

            var btn = _historyButton.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHighlight;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonNormal;
            btn.colors = colors;
            btn.onClick.AddListener((UnityEngine.Events.UnityAction)ToggleHistoryPanel);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_historyButton.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "History";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = TextColor;
        }

        private static void CreateHistoryPanel(Transform parent)
        {
            // --- Root panel: covers the delivery app area ---
            _historyPanel = new GameObject("DeliveriesProMax_HistoryPanel");
            _historyPanel.transform.SetParent(parent, false);

            var panelRect = _historyPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(0, 0);
            panelRect.offsetMax = new Vector2(0, 0);

            var panelBg = _historyPanel.AddComponent<Image>();
            panelBg.color = PanelBg;
            // Raycast target so clicks don't pass through to the delivery app behind
            panelBg.raycastTarget = true;

            // --- Header bar with title and close button ---
            var header = new GameObject("Header");
            header.transform.SetParent(_historyPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 36);

            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            // Title text
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(header.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-36, 0);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Delivery History";
            titleTmp.fontSize = 14;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = TextColor;

            // Close button (X)
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform, false);
            var closeBtnRect = closeBtn.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-4, 0);
            closeBtnRect.sizeDelta = new Vector2(30, 0);

            var closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var closeBtnButton = closeBtn.AddComponent<Button>();
            var closeColors = closeBtnButton.colors;
            closeColors.normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            closeColors.highlightedColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            closeColors.pressedColor = new Color(0.8f, 0.15f, 0.15f, 1f);
            closeBtnButton.colors = closeColors;
            closeBtnButton.onClick.AddListener((UnityEngine.Events.UnityAction)HideHistoryPanel);

            var closeText = new GameObject("X");
            closeText.transform.SetParent(closeBtn.transform, false);
            var closeTextRect = closeText.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeTmp = closeText.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "X";
            closeTmp.fontSize = 14;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = TextColor;

            // --- Scroll view for history entries ---
            var scrollViewGo = new GameObject("ScrollView");
            scrollViewGo.transform.SetParent(_historyPanel.transform, false);
            var scrollViewRect = scrollViewGo.AddComponent<RectTransform>();
            // Fill the panel below the header
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(0, 0);
            scrollViewRect.offsetMax = new Vector2(0, -36); // leave space for header

            var scrollViewImg = scrollViewGo.AddComponent<Image>();
            scrollViewImg.color = Color.clear;

            var scrollView = scrollViewGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.scrollSensitivity = 20f;

            // Mask to clip content
            var maskGo = scrollViewGo.AddComponent<Mask>();
            maskGo.showMaskGraphic = false;
            // Need a graphic for the mask to work - reuse the Image we added
            scrollViewImg.color = new Color(0, 0, 0, 0.01f); // nearly invisible but needed for mask

            // Content container inside scroll view
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollViewGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0); // ContentSizeFitter will manage height

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollView.content = contentRect;
            _historyContentParent = contentGo.transform;

            // Start hidden
            _historyPanel.SetActive(false);
        }

        private static void ToggleHistoryPanel()
        {
            if (!IsAlive(_historyPanel)) return;
            bool show = !_historyPanel!.activeSelf;
            _historyPanel.SetActive(show);
            if (show) RefreshHistoryPanel();
        }

        private static void HideHistoryPanel()
        {
            if (IsAlive(_historyPanel))
                _historyPanel!.SetActive(false);
        }

        // ----------------------------------------------------------------
        // History entries
        // ----------------------------------------------------------------

        private static void CreateHistoryEntry(Transform parent, SavedDelivery delivery)
        {
            var entryObj = new GameObject($"HistoryEntry_{delivery.Id}");
            entryObj.transform.SetParent(parent, false);

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 52;
            entryLayout.minHeight = 52;

            var bg = entryObj.AddComponent<Image>();
            bg.color = delivery.IsFavorite ? EntryFavBg : EntryBg;

            var hlayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 6;
            hlayout.padding = new RectOffset(8, 6, 4, 4);
            hlayout.childForceExpandWidth = false;
            hlayout.childForceExpandHeight = true;
            hlayout.childControlWidth = true;
            hlayout.childControlHeight = true;
            hlayout.childAlignment = TextAnchor.MiddleLeft;

            // Info text (flexible, takes remaining space)
            var infoText = $"<b>{delivery.ShopName}</b>\n" +
                           $"{delivery.Items.Count} item{(delivery.Items.Count != 1 ? "s" : "")} - ${delivery.TotalCost:F2}";
            var textObj = CreateText(entryObj.transform, "Info", infoText, 10, TextAlignmentOptions.Left);
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;
            textLayout.minWidth = 50;

            var capturedDelivery = delivery;

            // ---- Favorite button (star) ----
            var favButton = CreateActionButton(entryObj.transform, "FavBtn",
                delivery.IsFavorite ? "\u2605" : "\u2606",
                delivery.IsFavorite ? FavoriteColor : TextColor);
            favButton.GetComponent<Button>().onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                var isFav = Mod.Instance?.FavoriteManager?.ToggleFavorite(capturedDelivery.Id) ?? false;
                var t = favButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = isFav ? "\u2605" : "\u2606"; t.color = isFav ? FavoriteColor : TextColor; }
                bg.color = isFav ? EntryFavBg : EntryBg;
            }));

            // ---- Reorder button (arrow) ----
            var reorderButton = CreateActionButton(entryObj.transform, "ReorderBtn", "\u21BB", TextColor);
            reorderButton.GetComponent<Button>().onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                QuickReorder(capturedDelivery);
            }));

            // ---- Recurring toggle (loop) ----
            var recurButton = CreateActionButton(entryObj.transform, "RecurBtn",
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
        // Quick reorder logic
        // ----------------------------------------------------------------

        private static void QuickReorder(SavedDelivery delivery)
        {
            try
            {
                Mod.Logger.Msg($"Quick reorder: {delivery.ShopName}");

                var deliveryApp = GameRefs.FindDeliveryApp();
                if (deliveryApp == null) return;

                DeliveryShop? targetShop = deliveryApp.GetShop(delivery.ShopId);
                if (targetShop == null)
                {
                    Mod.Logger.Warning($"DeliveryShop not found: {delivery.ShopId}");
                    return;
                }

                if (targetShop.HasActiveDelivery())
                {
                    Mod.Logger.Msg("Shop already has an active delivery");
                    return;
                }

                if (Config.PreventNegativeBalance.Value && !GameRefs.CanPlayerAfford(delivery.TotalCost))
                {
                    Mod.Logger.Msg("Insufficient funds for reorder");
                    return;
                }

                targetShop.ResetCart();

                var listingEntries = targetShop.listingEntries;
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

                targetShop.OrderPressed();
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

        private static GameObject CreateActionButton(Transform parent, string name, string text, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 32;
            le.preferredHeight = 32;
            le.minWidth = 32;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.22f, 0.8f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.22f, 0.22f, 0.22f, 0.8f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            btn.colors = colors;

            var textGo = new GameObject($"{name}_Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false; // let clicks pass through to the button Image

            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = TextColor;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return go;
        }
    }
}
