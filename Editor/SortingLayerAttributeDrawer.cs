using UnityEditor;
using UnityEngine;
using System.Linq;
using toolbox.Atttributes;

namespace toolbox.Editor
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    class SortingLayerAttributeDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                // Integer is expected. Everything else is ignored.
                return;
            }
            EditorGUI.LabelField(position, label);

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;

            string[] sortingLayerNames = GetSortingLayerNames();
            int[] sortingLayerIDs = GetSortingLayerIDs();

            int sortingLayerIndex = Mathf.Max(0, System.Array.IndexOf<int>(sortingLayerIDs, property.intValue));
            sortingLayerIndex = EditorGUI.Popup(position, sortingLayerIndex, sortingLayerNames);
            property.intValue = sortingLayerIDs[sortingLayerIndex];
        }

        /**
         * Retrieves list of sorting layer names.
         *
         * @return List of sorting layer names.
         */
        private string[] GetSortingLayerNames()
        {
            return SortingLayer.layers.Select(layer => layer.name).ToArray();
        }

        /**
         * Retrieves list of sorting layer IDs.
         *
         * @return List of sorting layer IDs.
         */
        private int[] GetSortingLayerIDs()
        {
            return SortingLayer.layers.Select(layer => layer.id).ToArray();
        }
    }
}