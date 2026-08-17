using toolbox.AbilitySystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace toolbox.Editor
{
    /// <summary>
    /// One-line drawer for <see cref="ScalableFloat"/>: value, a "scale with level" toggle, and the
    /// level curve only when scaling is on.
    /// </summary>
    [CustomPropertyDrawer(typeof(ScalableFloat))]
    public class ScalableFloatDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var value = new PropertyField(property.FindPropertyRelative("baseValue"), property.displayName);
            value.style.flexGrow = 1f;
            row.Add(value);

            var scaleProperty = property.FindPropertyRelative("scaleWithLevel");
            var scale = new PropertyField(scaleProperty, "×Level")
            {
                tooltip = "Multiply by a curve evaluated at the owner's level.",
            };
            row.Add(scale);

            var curve = new PropertyField(property.FindPropertyRelative("levelCurve"), string.Empty);
            curve.style.flexGrow = 1f;
            curve.style.display = scaleProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            row.Add(curve);

            scale.RegisterValueChangeCallback(evt =>
                curve.style.display = evt.changedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None);

            return row;
        }
    }
}
