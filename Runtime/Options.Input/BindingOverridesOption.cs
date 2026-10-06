using UnityEngine.InputSystem;

namespace toolbox.Options.Input
{
    /// <summary>
    /// The player's key bindings as the Input System's binding-override JSON. <see cref="Capture"/> reads them from
    /// the asset a menu rebinds against; <see cref="ApplyTo"/> writes them into any live copy (a <c>PlayerInput</c>
    /// clones the asset per player). Empty means "no overrides", so <c>IsDefault</c> stays meaningful.
    /// Hidden: it has no row of its own; the rows are one per binding (see <see cref="ControlsOptionsPack.AddToPage"/>).
    /// </summary>
    public class BindingOverridesOption : StringOption
    {
        public BindingOverridesOption(string id) : base(id, "")
        {
            Category = "Controls";
            Label = "Key bindings";
            Flags = OptionFlags.Hidden;
            Presentation = OptionPresentation.Keybind;
        }

        /// <summary>Stores the overrides currently on <paramref name="actions"/>.</summary>
        public void Capture(IInputActionCollection2 actions)
        {
            string json = actions.SaveBindingOverridesAsJson();
            Value = HasOverrides(json) ? json : "";
        }

        /// <summary>Replaces the overrides on <paramref name="actions"/> with the stored ones.</summary>
        public void ApplyTo(IInputActionCollection2 actions)
        {
            if (string.IsNullOrEmpty(Value))
                actions.RemoveAllBindingOverrides();
            else
                actions.LoadBindingOverridesFromJson(Value, removeExisting: true);
        }

        static bool HasOverrides(string json) =>
            !string.IsNullOrWhiteSpace(json) && json.IndexOf("\"bindings\":[]", System.StringComparison.Ordinal) < 0
            && json.IndexOf("\"bindings\": []", System.StringComparison.Ordinal) < 0;
    }
}
