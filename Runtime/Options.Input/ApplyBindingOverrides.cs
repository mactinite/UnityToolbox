using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace toolbox.Options.Input
{
    /// <summary>
    /// Keeps a <see cref="PlayerInput"/>'s private copy of the actions in step with the stored key bindings. Put it
    /// next to the PlayerInput; it finds the option in <see cref="OptionsStore.Default"/>. Games that create the pack
    /// in code can call <see cref="ControlsOptionsPack.Track"/> instead.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class ApplyBindingOverrides : MonoBehaviour
    {
        [SerializeField] PlayerInput playerInput;
        [OptionId, SerializeField] string optionId = "controls.bindings";

        IDisposable tracking;

        void OnEnable()
        {
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();
            if (playerInput == null || playerInput.actions == null || !OptionsStore.HasDefault)
                return;
            var option = OptionsStore.Default.Get<BindingOverridesOption>(optionId);
            if (option == null)
            {
                Debug.LogWarning($"[Options] No binding overrides option '{optionId}' is registered.", this);
                return;
            }

            var actions = playerInput.actions;
            tracking = option.Bind(_ => option.ApplyTo(actions));
        }

        void OnDisable()
        {
            tracking?.Dispose();
            tracking = null;
        }
    }
}
