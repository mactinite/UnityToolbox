using System;
using TMPro;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace toolbox.Options.Input
{
    /// <summary>Plain uGUI keybind row and rebind prompt, in the style of <see cref="DefaultOptionsUI"/>.</summary>
    public static class DefaultKeybindUI
    {
        /// <summary>A row with a label, a button showing the current key, and a reset button; the button rebinds.</summary>
        public static GameObject CreateRow(Transform parent)
        {
            var rowImage = DefaultOptionsUI.Panel(parent, "Keybind Row", DefaultOptionsUI.Palette.Row);
            var go = rowImage.gameObject;
            DefaultOptionsUI.Size(go, height: DefaultOptionsUI.Metrics.RowHeight);
            DefaultOptionsUI.HStack(go, DefaultOptionsUI.Metrics.Spacing, DefaultOptionsUI.Metrics.RowPadding);

            var label = DefaultOptionsUI.Text(go.transform, "Label", "", DefaultOptionsUI.Metrics.FontSize, TextAlignmentOptions.MidlineLeft);
            DefaultOptionsUI.Size(label.gameObject, flexibleWidth: 1f);

            float height = Mathf.Max(8f, DefaultOptionsUI.Metrics.RowHeight - DefaultOptionsUI.Metrics.RowPadding.vertical - 2f);
            var button = DefaultOptionsUI.Button(go.transform, "Key", "", DefaultOptionsUI.Metrics.ControlWidth, height);
            DefaultOptionsUI.Size(button.gameObject, DefaultOptionsUI.Metrics.ControlWidth, height, flexibleWidth: 0f);
            var value = button.GetComponentInChildren<TMP_Text>();
            value.color = DefaultOptionsUI.Palette.Text;

            var reset = DefaultOptionsUI.Button(go.transform, "Reset", "Reset", DefaultOptionsUI.Metrics.ResetWidth, height);
            reset.GetComponentInChildren<TMP_Text>().fontSize = DefaultOptionsUI.Metrics.SmallFontSize * 0.8f;

            var row = go.AddComponent<KeybindRow>();
            row.ConfigureBase(label, value, reset, button, rowImage);
            return go;
        }

        /// <summary>An overlay with the message and a Cancel button, under <paramref name="parent"/> (normally the root canvas).</summary>
        public static IRebindPrompt CreateRebindPrompt(Transform parent)
        {
            var overlay = DefaultOptionsUI.Panel(parent, "Rebind Prompt", DefaultOptionsUI.Palette.Overlay);
            overlay.raycastTarget = true;
            DefaultOptionsUI.Stretch(overlay.rectTransform);
            DefaultOptionsUI.Size(overlay.gameObject).ignoreLayout = true;

            var panel = DefaultOptionsUI.Panel(overlay.transform, "Panel", DefaultOptionsUI.Palette.Panel);
            panel.raycastTarget = true;
            panel.rectTransform.sizeDelta = DefaultOptionsUI.Metrics.PromptSize;
            DefaultOptionsUI.VStack(panel.gameObject, DefaultOptionsUI.Metrics.Spacing * 1.5f, DefaultOptionsUI.Metrics.MenuPadding);

            var message = DefaultOptionsUI.Text(panel.transform, "Message", "Press a key", DefaultOptionsUI.Metrics.FontSize, TextAlignmentOptions.Center);
            message.textWrappingMode = TextWrappingModes.Normal;
            DefaultOptionsUI.Size(message.gameObject, height: DefaultOptionsUI.Metrics.FontSize * 3f);
            var hint = DefaultOptionsUI.Text(panel.transform, "Hint", "Escape cancels", DefaultOptionsUI.Metrics.SmallFontSize, TextAlignmentOptions.Center, DefaultOptionsUI.Palette.TextDim);
            DefaultOptionsUI.Size(hint.gameObject, height: DefaultOptionsUI.Metrics.SmallFontSize * 1.6f);

            var buttons = DefaultOptionsUI.Rect(panel.transform, "Buttons");
            DefaultOptionsUI.Size(buttons.gameObject, height: DefaultOptionsUI.Metrics.ButtonHeight);
            DefaultOptionsUI.HStack(buttons.gameObject, DefaultOptionsUI.Metrics.Spacing, null, TextAnchor.MiddleCenter);
            var cancel = DefaultOptionsUI.Button(buttons, "Cancel", "Cancel", DefaultOptionsUI.Metrics.TabWidth);

            var prompt = overlay.gameObject.AddComponent<DefaultRebindPrompt>();
            prompt.Configure(message, cancel);
            overlay.gameObject.SetActive(false);
            return prompt;
        }
    }

    /// <summary>"Press a key for X" overlay. Cancel (the button or the UI Cancel action) stops the rebind.</summary>
    public sealed class DefaultRebindPrompt : MonoBehaviour, IRebindPrompt
    {
        [SerializeField] TMP_Text messageText;
        [SerializeField] Button cancelButton;

        Action cancel;
        bool hooked;

        public bool IsShowing { get; private set; }

        public void Show(string message, Action onCancel)
        {
            Hook();
            cancel = onCancel;
            if (messageText != null)
                messageText.text = message;
            gameObject.SetActive(true);
            IsShowing = true;
            // No selection while listening: a Submit on a row must not count as the key, and Escape goes to the flow.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            IsShowing = false;
            cancel = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void Hook()
        {
            if (hooked)
                return;
            hooked = true;
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => cancel?.Invoke());
        }

        internal void Configure(TMP_Text message, Button cancelWidget)
        {
            messageText = message;
            cancelButton = cancelWidget;
        }
    }
}
