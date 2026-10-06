using System.Collections.Generic;
using System.Linq;
using toolbox.Options;
using UnityEditor;
using UnityEngine;

namespace toolbox.Options.UI.Editor
{
    /// <summary>Default inspector plus a validation pass against every catalogue and the standard packs' ids.</summary>
    [CustomEditor(typeof(OptionsLayoutAsset))]
    public class OptionsLayoutAssetEditor : UnityEditor.Editor
    {
        List<string> issues;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate against catalogues"))
                issues = Validate((OptionsLayoutAsset)target);

            if (issues == null)
                return;
            if (issues.Count == 0)
                EditorGUILayout.HelpBox("No issues: every placed id is known and every known option is placed.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }

        /// <summary>Unknown ids, duplicates, unplaced options and pages without ids, using the ids the editor can see.</summary>
        public static List<string> Validate(OptionsLayoutAsset asset)
        {
            var known = new HashSet<string>(toolbox.Editor.OptionIdDrawer.KnownIds());
            var layout = asset.ToLayout();
            var result = new List<string>();

            foreach (var id in layout.OptionEntries.Select(entry => entry.OptionId).Distinct())
            {
                if (string.IsNullOrEmpty(id))
                    result.Add("An option entry has no id.");
                else if (!known.Contains(id))
                    result.Add($"Unknown option id '{id}'.");
            }

            foreach (var id in layout.DuplicateIds())
                result.Add($"Option '{id}' is placed more than once.");

            var placed = new HashSet<string>(layout.OptionEntries.Select(entry => entry.OptionId));
            foreach (var id in known.Where(id => !placed.Contains(id)).OrderBy(id => id))
                result.Add($"Option '{id}' is not placed (fine if it is hidden or belongs to another menu).");

            for (int i = 0; i < asset.pages.Count; i++)
            {
                if (string.IsNullOrEmpty(asset.pages[i].id))
                    result.Add($"Page {i} has no id.");
            }

            return result;
        }
    }
}
