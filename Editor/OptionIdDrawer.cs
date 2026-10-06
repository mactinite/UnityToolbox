using System;
using System.Collections.Generic;
using System.Linq;
using toolbox.Options;
using UnityEditor;
using UnityEngine;

namespace toolbox.Editor
{
    /// <summary>
    /// A text field with a menu of every known option id: the static fields of <c>[OptionsCatalog]</c> classes plus
    /// the standard packs' ids, grouped by their first segment. Unknown ids are shown in yellow but kept.
    /// </summary>
    [CustomPropertyDrawer(typeof(OptionIdAttribute))]
    public class OptionIdDrawer : PropertyDrawer
    {
        static string[] knownIds;

        /// <summary>All ids the catalogues and packs declare, sorted. Cached until the next domain reload.</summary>
        public static string[] KnownIds()
        {
            if (knownIds != null)
                return knownIds;

            var ids = new HashSet<string>(StandardOptions.PackIds);
            foreach (var type in OptionsCatalog.FindCatalogTypes())
            {
                try
                {
                    foreach (var option in OptionsCatalog.Collect(type))
                        ids.Add(option.Id);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Options] Could not read catalogue {type.FullName}: {e.Message}");
                }
            }

            knownIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            return knownIds;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ids = KnownIds();
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            var textRect = new Rect(position.x, position.y, position.width - 22f, position.height);
            var buttonRect = new Rect(position.xMax - 20f, position.y, 20f, position.height);

            bool known = ids.Length == 0 || string.IsNullOrEmpty(property.stringValue) || Array.IndexOf(ids, property.stringValue) >= 0;
            var previousColor = GUI.color;
            if (!known)
                GUI.color = new Color(1f, 0.9f, 0.5f);
            EditorGUI.BeginChangeCheck();
            string edited = EditorGUI.TextField(textRect, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = edited;
            GUI.color = previousColor;

            if (GUI.Button(buttonRect, "▾", EditorStyles.miniButton))
            {
                var menu = new GenericMenu();
                if (ids.Length == 0)
                    menu.AddDisabledItem(new GUIContent("No [OptionsCatalog] classes found"));
                foreach (var id in ids)
                {
                    string captured = id;
                    menu.AddItem(new GUIContent(captured.Replace('.', '/')), captured == property.stringValue, () =>
                    {
                        property.serializedObject.Update();
                        property.stringValue = captured;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.DropDown(buttonRect);
            }

            EditorGUI.EndProperty();
        }
    }
}
