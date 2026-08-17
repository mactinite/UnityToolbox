using System.IO;
using toolbox.AbilitySystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace toolbox.Editor
{
    /// <summary>
    /// Inspector for <see cref="GameplayTag"/>: shows the computed full path, warns on parent cycles,
    /// and creates pre-parented child tag assets next to this one.
    /// </summary>
    [CustomEditor(typeof(GameplayTag))]
    public class GameplayTagEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var parent = new PropertyField(serializedObject.FindProperty("parent"));
            root.Add(parent);

            var path = new Label { style = { marginTop = 4f, marginBottom = 4f, unityFontStyleAndWeight = FontStyle.Bold } };
            root.Add(path);

            var cycleWarning = new HelpBox("This tag's parent chain loops back on itself.", HelpBoxMessageType.Error);
            root.Add(cycleWarning);

            root.Add(new Button(CreateChildTag)
            {
                text = "Create Child Tag",
                tooltip = "Creates a new tag asset beside this one with its parent already set.",
            });

            void Refresh()
            {
                var tag = (GameplayTag)target;
                path.text = tag.GetFullPath();
                cycleWarning.style.display = HasParentCycle(tag) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Refresh();
            parent.RegisterValueChangeCallback(_ => Refresh());
            return root;
        }

        private static bool HasParentCycle(GameplayTag tag)
        {
            var node = tag.Parent;
            for (var depth = 0; depth < GameplayTag.MaxDepth && node != null; depth++)
            {
                if (node == tag)
                    return true;

                node = node.Parent;
            }

            return node != null;
        }

        private void CreateChildTag()
        {
            var tag = (GameplayTag)target;
            var assetPath = AssetDatabase.GetAssetPath(tag);
            if (string.IsNullOrEmpty(assetPath))
                return;

            var child = CreateInstance<GameplayTag>();
            child.parent = tag;

            var childPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(Path.GetDirectoryName(assetPath)!, $"{tag.name}_Child.asset"));
            ProjectWindowUtil.CreateAsset(child, childPath);
        }
    }
}
