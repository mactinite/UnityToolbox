using toolbox.AbilitySystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace toolbox.Editor
{
    /// <summary>
    /// One-line drawer for <see cref="EffectModifierDefinition"/>: attribute | channel | magnitude.
    /// The inline magnitude hides when a custom <see cref="MagnitudeCalculation"/> asset is assigned.
    /// </summary>
    [CustomPropertyDrawer(typeof(EffectModifierDefinition))]
    public class EffectModifierDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var attribute = new PropertyField(property.FindPropertyRelative("attribute"), string.Empty);
            attribute.style.flexGrow = 1f;
            attribute.style.flexBasis = 0f;
            row.Add(attribute);

            var channel = new PropertyField(property.FindPropertyRelative("channel"), string.Empty);
            channel.style.width = 100f;
            row.Add(channel);

            var customProperty = property.FindPropertyRelative("customMagnitude");

            var magnitude = new PropertyField(property.FindPropertyRelative("magnitude"), string.Empty);
            magnitude.style.flexGrow = 1f;
            magnitude.style.flexBasis = 0f;
            magnitude.style.display = customProperty.objectReferenceValue == null ? DisplayStyle.Flex : DisplayStyle.None;
            row.Add(magnitude);

            var custom = new PropertyField(customProperty, string.Empty)
            {
                tooltip = "Optional calculation asset; replaces the inline magnitude when set.",
            };
            custom.style.flexGrow = 1f;
            custom.style.flexBasis = 0f;
            row.Add(custom);

            custom.RegisterValueChangeCallback(evt =>
                magnitude.style.display = evt.changedProperty.objectReferenceValue == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None);

            return row;
        }
    }
}
