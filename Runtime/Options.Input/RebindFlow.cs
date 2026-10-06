using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace toolbox.Options.Input
{
    /// <summary>What happens when a new binding already belongs to another action of the same map and scheme.</summary>
    public enum DuplicatePolicy
    {
        /// <summary>Keep both.</summary>
        Allow,
        /// <summary>Give the other action the key this one had.</summary>
        Swap,
        /// <summary>Refuse the new key and keep the old one.</summary>
        Block,
    }

    public enum RebindOutcome
    {
        Completed,
        Cancelled,
        /// <summary>Refused by <see cref="DuplicatePolicy.Block"/>; the binding is unchanged.</summary>
        Blocked,
    }

    public sealed class RebindConfig
    {
        /// <summary>Control that cancels a rebind in progress.</summary>
        public string CancelControl = "<Keyboard>/escape";

        /// <summary>Controls that never count as a press (mouse motion, scroll).</summary>
        public string[] ExcludedControls = { "<Mouse>/position", "<Mouse>/delta", "<Mouse>/scroll", "<Pointer>/position", "<Pointer>/delta" };

        /// <summary>After a match, wait this long for a better one (so a stick axis settles).</summary>
        public float MatchWait = 0.1f;

        public DuplicatePolicy Duplicates = DuplicatePolicy.Swap;

        /// <summary>Only accept controls of the devices the binding's control scheme requires (no keyboard key in a gamepad slot).</summary>
        public bool RestrictToSchemeDevices = true;
    }

    /// <summary>
    /// One interactive rebind at a time: disables the action, listens for a control, resolves duplicates, re-enables.
    /// The callback runs when the operation completes, is cancelled or is blocked.
    /// </summary>
    public sealed class RebindFlow : IDisposable
    {
        InputActionRebindingExtensions.RebindingOperation operation;

        public bool IsActive => operation != null;

        /// <summary>The action being rebound, or null.</summary>
        public InputAction Current { get; private set; }

        public void Start(InputAction action, int bindingIndex, RebindConfig config, Action<RebindOutcome> done)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex));
            Cancel();
            config = config ?? new RebindConfig();

            bool wasEnabled = action.enabled;
            if (wasEnabled)
                action.Disable();
            string previousPath = action.bindings[bindingIndex].effectivePath;
            Current = action;

            var op = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough(config.CancelControl)
                .OnMatchWaitForAnother(config.MatchWait);
            foreach (var excluded in config.ExcludedControls ?? Array.Empty<string>())
                op = op.WithControlsExcluding(excluded);
            if (config.RestrictToSchemeDevices)
            {
                foreach (var devicePath in SchemeDevicePaths(action, bindingIndex))
                    op = op.WithControlsHavingToMatchPath(devicePath);
            }

            op.OnCancel(_ =>
            {
                Finish();
                if (wasEnabled)
                    action.Enable();
                done?.Invoke(RebindOutcome.Cancelled);
            });
            op.OnComplete(_ =>
            {
                Finish();
                var outcome = ResolveDuplicates(action, bindingIndex, previousPath, config.Duplicates);
                if (wasEnabled)
                    action.Enable();
                done?.Invoke(outcome);
            });

            operation = op.Start();
        }

        /// <summary>Cancels a rebind in progress; its callback receives <see cref="RebindOutcome.Cancelled"/>.</summary>
        public void Cancel()
        {
            operation?.Cancel();
        }

        public void Dispose() => Cancel();

        void Finish()
        {
            operation?.Dispose();
            operation = null;
            Current = null;
        }

        /// <summary>Device paths ("&lt;Gamepad&gt;") of the control scheme the binding belongs to, from its first binding group.</summary>
        internal static IEnumerable<string> SchemeDevicePaths(InputAction action, int bindingIndex)
        {
            var asset = action.actionMap != null ? action.actionMap.asset : null;
            string groups = action.bindings[bindingIndex].groups;
            if (asset == null || string.IsNullOrEmpty(groups))
                yield break;
            string group = groups.Split(InputBinding.Separator)[0];
            foreach (var scheme in asset.controlSchemes)
            {
                if (scheme.bindingGroup != group)
                    continue;
                foreach (var requirement in scheme.deviceRequirements)
                    yield return requirement.controlPath;
            }
        }

        /// <summary>
        /// After a rebind of <paramref name="action"/>'s binding, looks for another binding of the same map and scheme on the
        /// same control and applies the policy. Returns Blocked when the policy refused the new key.
        /// </summary>
        internal static RebindOutcome ResolveDuplicates(InputAction action, int bindingIndex, string previousPath, DuplicatePolicy policy)
        {
            if (policy == DuplicatePolicy.Allow || action.actionMap == null)
                return RebindOutcome.Completed;

            var newBinding = action.bindings[bindingIndex];
            string newPath = newBinding.effectivePath;
            if (string.IsNullOrEmpty(newPath) || newPath == previousPath)
                return RebindOutcome.Completed;

            foreach (var other in action.actionMap.actions)
            {
                var bindings = other.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (other == action && i == bindingIndex)
                        continue;
                    var candidate = bindings[i];
                    if (candidate.isComposite || candidate.effectivePath != newPath || !SharesGroup(candidate.groups, newBinding.groups))
                        continue;

                    if (policy == DuplicatePolicy.Swap)
                    {
                        other.ApplyBindingOverride(i, previousPath);
                        return RebindOutcome.Completed;
                    }

                    // Block: put the previous key back.
                    if (previousPath == action.bindings[bindingIndex].path)
                        action.RemoveBindingOverride(bindingIndex);
                    else
                        action.ApplyBindingOverride(bindingIndex, previousPath);
                    return RebindOutcome.Blocked;
                }
            }

            return RebindOutcome.Completed;
        }

        static bool SharesGroup(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b);
            foreach (var groupA in a.Split(InputBinding.Separator))
            {
                foreach (var groupB in b.Split(InputBinding.Separator))
                {
                    if (groupA == groupB)
                        return true;
                }
            }

            return false;
        }
    }
}
