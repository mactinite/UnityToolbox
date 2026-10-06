using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>
    /// "Keep these settings?" with a countdown that reverts on timeout or Cancel. Runs on unscaled time so it works
    /// while the game is paused. Wire the fields on your own prefab, or let <see cref="DefaultOptionsUI.CreateConfirmPrompt"/> build one.
    /// </summary>
    public sealed class DefaultConfirmPrompt : MonoBehaviour, IConfirmPrompt
    {
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Button keepButton;
        [SerializeField] Button revertButton;
        [SerializeField] string countdownFormat = "Reverting in {0:0} s";

        Action keep;
        Action revert;
        float remaining;
        GameObject previousSelection;
        bool hooked;

        public bool IsShowing { get; private set; }

        public void Show(string message, float seconds, Action onKeep, Action onRevert)
        {
            Hook();
            keep = onKeep;
            revert = onRevert;
            remaining = Mathf.Max(1f, seconds);
            if (messageText != null)
                messageText.text = message;
            UpdateCountdown();

            var eventSystem = EventSystem.current;
            previousSelection = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            gameObject.SetActive(true);
            IsShowing = true;
            if (eventSystem != null && keepButton != null)
                eventSystem.SetSelectedGameObject(keepButton.gameObject);
        }

        public void Hide()
        {
            if (IsShowing)
                Finish();
        }

        void Update()
        {
            if (!IsShowing)
                return;
            remaining -= Time.unscaledDeltaTime;
            UpdateCountdown();
            if (remaining <= 0f)
                Revert();
        }

        void Hook()
        {
            if (hooked)
                return;
            hooked = true;
            if (keepButton != null)
            {
                keepButton.onClick.AddListener(Keep);
                keepButton.gameObject.AddComponent<CancelRelay>().Cancelled = Revert;
            }

            if (revertButton != null)
            {
                revertButton.onClick.AddListener(Revert);
                revertButton.gameObject.AddComponent<CancelRelay>().Cancelled = Revert;
            }

            if (keepButton != null && revertButton != null)
            {
                var keepNavigation = keepButton.navigation;
                keepNavigation.mode = Navigation.Mode.Explicit;
                keepNavigation.selectOnRight = revertButton;
                keepButton.navigation = keepNavigation;
                var revertNavigation = revertButton.navigation;
                revertNavigation.mode = Navigation.Mode.Explicit;
                revertNavigation.selectOnLeft = keepButton;
                revertButton.navigation = revertNavigation;
            }
        }

        void UpdateCountdown()
        {
            if (countdownText != null)
                countdownText.text = string.Format(countdownFormat, Mathf.Ceil(remaining));
        }

        void Keep()
        {
            var action = keep;
            Finish();
            action?.Invoke();
        }

        void Revert()
        {
            var action = revert;
            Finish();
            action?.Invoke();
        }

        void Finish()
        {
            IsShowing = false;
            keep = null;
            revert = null;
            gameObject.SetActive(false);
            if (previousSelection != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }

        internal void Configure(TMP_Text message, TMP_Text countdown, Button keepWidget, Button revertWidget)
        {
            messageText = message;
            countdownText = countdown;
            keepButton = keepWidget;
            revertButton = revertWidget;
        }

        sealed class CancelRelay : MonoBehaviour, ICancelHandler
        {
            public Action Cancelled { get; set; }

            public void OnCancel(BaseEventData eventData) => Cancelled?.Invoke();
        }
    }
}
