using System.Collections.Generic;
using System.Globalization;
using toolbox.Options;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.UI;

namespace toolbox.Samples.OptionsDemo
{
    /// <summary>
    /// A custom widget written from <see cref="OptionRow"/>: ten blocks that fill with the value of an
    /// <see cref="IRangeOption"/>. Left/right and clicks change it. Built procedurally by <see cref="Create"/> so the
    /// sample needs no prefab; a game would make a prefab with this component and wire the fields.
    /// </summary>
    public class BlockMeterRow : OptionRow
    {
        [SerializeField] List<Image> blocks = new List<Image>();
        [SerializeField] Color filled = new Color(0.35f, 0.75f, 1f);
        [SerializeField] Color empty = new Color(1f, 1f, 1f, 0.12f);

        IRangeOption range;

        protected override void OnBind()
        {
            range = Option as IRangeOption;
            for (int i = 0; i < blocks.Count; i++)
            {
                var button = blocks[i].GetComponent<Button>();
                if (button == null)
                    continue;
                int index = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SetBlocks(index + 1));
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
            }
        }

        protected override void OnRefresh()
        {
            int lit = FilledBlocks();
            for (int i = 0; i < blocks.Count; i++)
                blocks[i].color = i < lit ? filled : empty;
        }

        int FilledBlocks()
        {
            if (range == null || blocks.Count == 0)
                return 0;
            float current = System.Convert.ToSingle(Context.GetValue(Option), CultureInfo.InvariantCulture);
            return Mathf.RoundToInt(Mathf.InverseLerp(range.MinValue, range.MaxValue, current) * blocks.Count);
        }

        void SetBlocks(int count)
        {
            if (range == null)
                return;
            count = Mathf.Clamp(count, 0, blocks.Count);
            float value = Mathf.Lerp(range.MinValue, range.MaxValue, count / (float)blocks.Count);
            Write(range.IsInteger ? (object)Mathf.RoundToInt(value) : value);
        }

        public override void Adjust(int direction)
        {
            int current = FilledBlocks();
            if ((direction < 0 && current == 0) || (direction > 0 && current == blocks.Count))
            {
                Deny();
                return;
            }

            SetBlocks(current + direction);
        }

        protected override string ValueLabel() => FilledBlocks() + " / " + blocks.Count;

        /// <summary>A complete row with this widget, in the built-in style.</summary>
        public static GameObject Create(Transform parent, int blockCount = 10)
        {
            var rowImage = DefaultOptionsUI.Panel(parent, "Meter Row", DefaultOptionsUI.Palette.Row);
            var go = rowImage.gameObject;
            DefaultOptionsUI.Size(go, height: DefaultOptionsUI.Metrics.RowHeight);
            DefaultOptionsUI.HStack(go, DefaultOptionsUI.Metrics.Spacing, new RectOffset(12, 12, 4, 4));

            var label = DefaultOptionsUI.Text(go.transform, "Label", "", DefaultOptionsUI.Metrics.FontSize, TMPro.TextAlignmentOptions.MidlineLeft);
            DefaultOptionsUI.Size(label.gameObject, flexibleWidth: 1f);

            var meter = DefaultOptionsUI.Panel(go.transform, "Meter", DefaultOptionsUI.Palette.Control);
            meter.raycastTarget = true;
            DefaultOptionsUI.Size(meter.gameObject, DefaultOptionsUI.Metrics.ControlWidth - 64f, DefaultOptionsUI.Metrics.RowHeight - 10f, flexibleWidth: 0f);
            DefaultOptionsUI.HStack(meter.gameObject, 3f, new RectOffset(4, 4, 6, 6));
            var selectable = meter.gameObject.AddComponent<Selectable>();
            selectable.targetGraphic = meter;
            selectable.colors = DefaultOptionsUI.TintColors();

            var row = go.AddComponent<BlockMeterRow>();
            for (int i = 0; i < blockCount; i++)
            {
                var block = DefaultOptionsUI.Panel(meter.transform, "Block " + i, row.empty);
                block.raycastTarget = true;
                // Both flexible: an Image without a sprite has no preferred size, so the layout group would collapse it.
                DefaultOptionsUI.Size(block.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);
                var button = block.gameObject.AddComponent<Button>();
                button.targetGraphic = block;
                button.transition = Selectable.Transition.None;
                row.blocks.Add(block);
            }

            var value = DefaultOptionsUI.Text(go.transform, "Value", "", DefaultOptionsUI.Metrics.SmallFontSize, TMPro.TextAlignmentOptions.MidlineRight, DefaultOptionsUI.Palette.TextDim);
            DefaultOptionsUI.Size(value.gameObject, width: 58f);
            var reset = DefaultOptionsUI.Button(go.transform, "Reset", "Reset", 58f, DefaultOptionsUI.Metrics.RowHeight - 12f);
            row.ConfigureRow(label, value, reset, selectable, rowImage);
            return go;
        }

        void ConfigureRow(TMPro.TMP_Text labelText, TMPro.TMP_Text valueLabel, Button reset, Selectable primarySelectable, Graphic rowBackground)
        {
            label = labelText;
            valueText = valueLabel;
            resetButton = reset;
            primary = primarySelectable;
            background = rowBackground;
        }
    }
}
