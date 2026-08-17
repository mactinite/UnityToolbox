using System.Collections.Generic;
using System.Text;
using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>
    /// Debug overlay showing the ability system's live state: attributes, owned tags, active effects
    /// (with remaining time, stacks, inhibition) and the jump cooldown. Immediate-mode on purpose —
    /// it's diagnostic UI for the sample, not a UI framework recommendation.
    /// </summary>
    public class SampleHud : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilities;
        [SerializeField] private AttributeDefinition staminaAttribute;
        [SerializeField] private AttributeDefinition moveSpeedAttribute;
        [SerializeField] private GameplayAbilityDefinition jumpAbility;
        [SerializeField] private SampleAvatar avatar;

        private readonly List<GameplayTag> tagScratch = new();
        private readonly StringBuilder text = new();

        private void OnGUI()
        {
            if (abilities == null)
                return;

            var core = abilities.Core;

            GUILayout.BeginArea(new Rect(10f, 10f, 340f, Screen.height - 20f), GUI.skin.box);
            GUILayout.Label("<b>Ability System Demo</b>  —  WASD move · Space jump · hold Shift sprint", Rich());

            var stamina = core.Attributes.GetValue(staminaAttribute);
            var staminaMax = staminaAttribute.HasMax ? staminaAttribute.MaxValue : 100f;
            DrawBar($"Stamina  {stamina:0} / {staminaMax:0}", stamina / staminaMax);

            GUILayout.Label($"MoveSpeed attribute: {core.Attributes.GetValue(moveSpeedAttribute):0.0}   (actual: {(avatar != null ? avatar.CurrentSpeed : 0f):0.0} m/s)");

            var jumpSpec = core.FindAbility(jumpAbility);
            if (jumpSpec != null)
            {
                var cooldown = jumpSpec.GetCooldown();
                GUILayout.Label(cooldown.IsActive
                    ? $"Jump cooldown: {cooldown.Remaining:0.0}s / {cooldown.Total:0.0}s"
                    : "Jump ready" + (jumpSpec.CheckCost() ? string.Empty : "  (can't afford)"));
            }

            core.Tags.GetExplicitTags(tagScratch);
            text.Clear();
            text.Append("<b>Tags:</b> ");
            if (tagScratch.Count == 0)
                text.Append("—");
            for (var i = 0; i < tagScratch.Count; i++)
                text.Append(i > 0 ? ", " : string.Empty).Append(tagScratch[i].GetFullPath());
            GUILayout.Label(text.ToString(), Rich());

            GUILayout.Label("<b>Active effects:</b>", Rich());
            if (core.ActiveEffects.Count == 0)
                GUILayout.Label("  —");
            for (var i = 0; i < core.ActiveEffects.Count; i++)
            {
                var active = core.ActiveEffects[i];
                var remaining = float.IsPositiveInfinity(active.RemainingDuration)
                    ? "∞"
                    : $"{active.RemainingDuration:0.0}s";
                var stacks = active.StackCount > 1 ? $" ×{active.StackCount}" : string.Empty;
                var inhibited = active.IsInhibited ? "  [inhibited]" : string.Empty;
                GUILayout.Label($"  {active.Spec.Definition.name}{stacks}  ({remaining}){inhibited}");
            }

            GUILayout.EndArea();
        }

        private static GUIStyle Rich()
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true };
            return style;
        }

        private static void DrawBar(string label, float fill)
        {
            GUILayout.Label(label);
            var rect = GUILayoutUtility.GetRect(320f, 14f);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.8f, 0.35f);
            rect.width *= Mathf.Clamp01(fill);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
