using System;
using System.Collections.Generic;
using System.Linq;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace toolbox.Options.Input
{
    public sealed class ControlsConfig
    {
        public string OptionId = "controls.bindings";
        public string Category = "Controls";
        /// <summary>Action maps offered for rebinding; null means every map.</summary>
        public string[] Maps;
        /// <summary>Actions never offered, by name or "Map/Action" (aim axes, the pause key).</summary>
        public string[] ExcludedActions;
        /// <summary>Control schemes (binding groups) offered; null means every scheme that has bindings in the chosen maps.</summary>
        public string[] Schemes;
        /// <summary>Offer the parts of composites (WASD) one by one.</summary>
        public bool IncludeCompositeParts = true;
        public RebindConfig Rebind = new RebindConfig();
        /// <summary>Section label for a scheme; the scheme name by default.</summary>
        public Func<string, string> SchemeLabel;
        /// <summary>Row label for an action; its humanized name by default.</summary>
        public Func<InputAction, string> ActionLabel;
    }

    /// <summary>One rebindable binding: the row's payload (<see cref="OptionEntry.Tag"/>).</summary>
    public sealed class KeybindTarget
    {
        public ControlsOptionsPack Pack { get; internal set; }
        public InputAction Action { get; internal set; }
        public int BindingIndex { get; internal set; }
        public string Scheme { get; internal set; }
        public string Label { get; internal set; }

        public InputBinding Binding => Action.bindings[BindingIndex];
        public bool HasOverride => Binding.hasOverrides;

        /// <summary>"Space", "Left Button", "Button South": the current key.</summary>
        public string DisplayString() =>
            Action.GetBindingDisplayString(BindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
    }

    /// <summary>
    /// Key rebinding for an <see cref="InputActionAsset"/>: one <see cref="BindingOverridesOption"/> holding the
    /// overrides, a private rebinding copy of the asset, the targets (one per binding of the chosen maps and
    /// schemes), and <see cref="AddToPage"/> to place a keybind row per target. Live copies (each <c>PlayerInput</c>)
    /// follow the option through <see cref="Track"/>. Add it with <see cref="StandardInputOptions.AddControls"/>.
    /// </summary>
    public sealed class ControlsOptionsPack : IDisposable
    {
        readonly IDisposable mirror;

        internal ControlsOptionsPack(OptionsStore store, InputActionAsset source, ControlsConfig config)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Settings = config ?? new ControlsConfig();
            Asset = UnityEngine.Object.Instantiate(source);
            Asset.name = source.name + " (rebinding)";
            Bindings = new BindingOverridesOption(Settings.OptionId) { Category = Settings.Category };
            store.Register(Bindings);
            mirror = Bindings.Bind(_ => Bindings.ApplyTo(Asset));
        }

        public ControlsConfig Settings { get; }
        public BindingOverridesOption Bindings { get; }
        /// <summary>The copy the menu rebinds against. Never enabled; live copies get the result through <see cref="Track"/>.</summary>
        public InputActionAsset Asset { get; }
        public RebindFlow Flow { get; } = new RebindFlow();

        /// <summary>The schemes offered: the configured ones, else every scheme with bindings in the chosen maps.</summary>
        public IEnumerable<string> Schemes()
        {
            if (Settings.Schemes != null)
                return Settings.Schemes;
            var used = new HashSet<string>();
            foreach (var map in Maps())
            {
                foreach (var binding in map.bindings)
                {
                    if (binding.isComposite || string.IsNullOrEmpty(binding.groups))
                        continue;
                    foreach (var group in binding.groups.Split(InputBinding.Separator))
                        used.Add(group);
                }
            }

            return Asset.controlSchemes.Select(scheme => scheme.bindingGroup).Where(used.Contains).ToList();
        }

        /// <summary>One target per non-composite binding of the chosen maps that belongs to <paramref name="scheme"/>.</summary>
        public IEnumerable<KeybindTarget> Targets(string scheme)
        {
            foreach (var map in Maps())
            {
                foreach (var action in map.actions)
                {
                    if (IsExcluded(action))
                        continue;
                    var bindings = action.bindings;
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        var binding = bindings[i];
                        if (binding.isComposite)
                            continue;
                        if (binding.isPartOfComposite && !Settings.IncludeCompositeParts)
                            continue;
                        if (!InGroup(binding.groups, scheme))
                            continue;
                        yield return new KeybindTarget
                        {
                            Pack = this,
                            Action = action,
                            BindingIndex = i,
                            Scheme = scheme,
                            Label = LabelFor(action, binding),
                        };
                    }
                }
            }
        }

        /// <summary>Appends a section per scheme with a keybind row per target. Rows use the theme's keybind prefab or the built-in row.</summary>
        public void AddToPage(OptionsPage page)
        {
            foreach (var scheme in Schemes())
            {
                page.Section(Settings.SchemeLabel != null ? Settings.SchemeLabel(scheme) : scheme);
                foreach (var target in Targets(scheme))
                {
                    var captured = target;
                    page.Option(Bindings.Id, entry =>
                    {
                        entry.Tag = captured;
                        entry.Label = captured.Label;
                        entry.RowFactory = DefaultKeybindUI.CreateRow;
                    });
                }
            }
        }

        /// <summary>Applies the stored overrides to a live copy now and after every change. Dispose when the copy goes away.</summary>
        public IDisposable Track(IInputActionCollection2 live) => Bindings.Bind(_ => Bindings.ApplyTo(live));

        /// <summary>Removes every override.</summary>
        public void ResetAll()
        {
            Asset.RemoveAllBindingOverrides();
            Bindings.Capture(Asset);
        }

        public void Dispose()
        {
            Flow.Dispose();
            mirror.Dispose();
            if (Asset != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(Asset);
                else
                    UnityEngine.Object.DestroyImmediate(Asset);
            }
        }

        IEnumerable<InputActionMap> Maps() =>
            Asset.actionMaps.Where(map => Settings.Maps == null || Array.IndexOf(Settings.Maps, map.name) >= 0);

        bool IsExcluded(InputAction action)
        {
            if (Settings.ExcludedActions == null)
                return false;
            foreach (var excluded in Settings.ExcludedActions)
            {
                if (excluded == action.name || excluded == action.actionMap.name + "/" + action.name)
                    return true;
            }

            return false;
        }

        string LabelFor(InputAction action, InputBinding binding)
        {
            string label = Settings.ActionLabel != null ? Settings.ActionLabel(action) : Option.Humanize(action.name);
            if (binding.isPartOfComposite && !string.IsNullOrEmpty(binding.name))
                label += " · " + Option.Humanize(binding.name);
            return label;
        }

        static bool InGroup(string groups, string scheme)
        {
            if (string.IsNullOrEmpty(groups))
                return false;
            foreach (var group in groups.Split(InputBinding.Separator))
            {
                if (group == scheme)
                    return true;
            }

            return false;
        }
    }

    public static class StandardInputOptions
    {
        /// <summary>Key rebinding for <paramref name="actions"/>. See <see cref="ControlsOptionsPack"/>.</summary>
        public static ControlsOptionsPack AddControls(this OptionsStore store, InputActionAsset actions, ControlsConfig config = null) =>
            new ControlsOptionsPack(store, actions, config);
    }
}
