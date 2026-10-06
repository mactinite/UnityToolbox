using System;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace toolbox.Options.Input
{
    /// <summary>"Press a key for ..." while a rebind listens. The default is <see cref="DefaultRebindPrompt"/>.</summary>
    public interface IRebindPrompt
    {
        bool IsShowing { get; }
        void Show(string message, Action cancel);
        void Hide();
    }

    /// <summary>
    /// One key binding: label, the current key, and Submit (or a click) to rebind it. Several rows share the pack's
    /// <see cref="BindingOverridesOption"/>; the row's <see cref="OptionEntry.Tag"/> is its <see cref="KeybindTarget"/>.
    /// Reset clears this binding's override only.
    /// </summary>
    public class KeybindRow : OptionRow
    {
        /// <summary>Builds the prompt for a canvas; replace to use your own (see <see cref="IRebindPrompt"/>).</summary>
        public static Func<Transform, IRebindPrompt> PromptFactory = DefaultKeybindUI.CreateRebindPrompt;

        [SerializeField] string listeningText = "...";
        [SerializeField] string promptFormat = "Press a key for {0}";

        KeybindTarget target;
        bool listening;

        public override bool HandlesHorizontalMove => false;

        protected override void OnBind()
        {
            target = Entry?.Tag as KeybindTarget;
            if (target == null)
            {
                Debug.LogWarning($"[Options] KeybindRow for '{Option.Id}' has no KeybindTarget tag; place it through ControlsOptionsPack.AddToPage.", this);
                return;
            }

            if (primary is Button button)
            {
                button.onClick.RemoveListener(Activate);
                button.onClick.AddListener(Activate);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            if (resetButton != null)
                resetButton.gameObject.SetActive(target != null && target.HasOverride && !listening);
        }

        protected override string ValueLabel()
        {
            if (target == null)
                return "";
            return listening ? listeningText : target.DisplayString();
        }

        /// <summary>Starts listening for a key.</summary>
        public override void Activate()
        {
            if (target == null || listening || target.Pack.Flow.IsActive)
                return;

            listening = true;
            Refresh();
            var prompt = ResolvePrompt();
            prompt?.Show(string.Format(promptFormat, Context.Text.Label(Option, Entry)), () => target.Pack.Flow.Cancel());

            target.Pack.Flow.Start(target.Action, target.BindingIndex, target.Pack.Settings.Rebind, outcome =>
            {
                listening = false;
                prompt?.Hide();
                if (outcome == RebindOutcome.Completed)
                {
                    target.Pack.Bindings.Capture(target.Action.actionMap.asset);
                    Context.Commit(this);
                }
                else if (outcome == RebindOutcome.Blocked)
                {
                    Deny();
                }

                Refresh();
                Select();
            });
        }

        /// <summary>Clears this binding's override.</summary>
        public override void ResetToDefault()
        {
            if (target == null || listening)
                return;
            target.Action.RemoveBindingOverride(target.BindingIndex);
            target.Pack.Bindings.Capture(target.Action.actionMap.asset);
            Context.Commit(this);
            Refresh();
        }

        IRebindPrompt ResolvePrompt()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;
            var root = canvas.rootCanvas.transform;
            var existing = root.GetComponentInChildren<DefaultRebindPrompt>(true);
            if (existing != null)
                return existing;
            return PromptFactory?.Invoke(root);
        }

        internal void Configure(string listening, string format)
        {
            listeningText = listening;
            promptFormat = format;
        }
    }
}
