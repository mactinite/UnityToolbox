using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>
    /// Plain uGUI versions of every menu element, built at runtime so a game needs no prefabs to start. Theme prefabs
    /// replace them piecemeal. Colours and sizes are static so a game can retune the built-ins in one place before
    /// the menu is built.
    /// </summary>
    public static class DefaultOptionsUI
    {
        public static class Palette
        {
            public static Color Panel = new Color(0.08f, 0.09f, 0.11f, 0.96f);
            public static Color Viewport = new Color(0f, 0f, 0f, 0.18f);
            public static Color Row = new Color(1f, 1f, 1f, 0.04f);
            public static Color Control = new Color(1f, 1f, 1f, 0.10f);
            public static Color Button = new Color(1f, 1f, 1f, 0.12f);
            public static Color Accent = new Color(0.35f, 0.75f, 1f);
            public static Color Text = new Color(0.92f, 0.94f, 0.96f);
            public static Color TextDim = new Color(0.60f, 0.65f, 0.70f);
            public static Color Overlay = new Color(0f, 0f, 0f, 0.6f);
        }

        public static class Metrics
        {
            public static float RowHeight = 40f;
            public static float FontSize = 18f;
            public static float SmallFontSize = 15f;
            public static float TitleFontSize = 26f;
            public static float ControlWidth = 260f;
            public static float ButtonHeight = 34f;
            public static float Spacing = 6f;
            /// <summary>Size of the built-in menu panel; a zero or negative component stretches the panel to its parent instead.</summary>
            public static Vector2 MenuSize = new Vector2(820f, 540f);
            public static RectOffset MenuPadding = new RectOffset(16, 16, 16, 16);
        }

        /// <summary>Font for every built-in text; null uses the TextMeshPro default.</summary>
        public static TMP_FontAsset Font;

        /// <summary>Sliced sprite for built-in buttons; null draws flat rectangles.</summary>
        public static Sprite ButtonSprite;

        // ---- primitives -------------------------------------------------------------------------------------------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color? color = null)
        {
            var rect = Rect(parent, name);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null)
                tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color ?? Palette.Text;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string name, string label, float width = -1f, float height = -1f)
        {
            var image = Panel(parent, name, Palette.Button);
            image.raycastTarget = true;
            if (ButtonSprite != null)
            {
                image.sprite = ButtonSprite;
                image.type = Image.Type.Sliced;
            }

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = TintColors();
            Size(image.gameObject, width, height > 0f ? height : Metrics.ButtonHeight);
            var text = Text(image.transform, "Label", label, Metrics.SmallFontSize, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 4f);
            return button;
        }

        /// <summary>Colours that brighten translucent white graphics: alpha above one multiplies up the base alpha.</summary>
        public static ColorBlock TintColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 2f);
            colors.selectedColor = new Color(1f, 1f, 1f, 3f);
            colors.pressedColor = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 4f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static LayoutElement Size(GameObject go, float width = -1f, float height = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null)
                element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static HorizontalLayoutGroup HStack(GameObject go, float spacing, RectOffset padding = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var group = go.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = alignment;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static VerticalLayoutGroup VStack(GameObject go, float spacing, RectOffset padding = null)
        {
            var group = go.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = TextAnchor.UpperLeft;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        // ---- rows -------------------------------------------------------------------------------------------------

        /// <summary>A complete row of the given kind with its <see cref="OptionRow"/> wired. Dropdown and keybind kinds use the stepper.</summary>
        public static GameObject CreateRow(RowKind kind, Transform parent)
        {
            var rowImage = Panel(parent, kind + " Row", Palette.Row);
            var go = rowImage.gameObject;
            Size(go, height: Metrics.RowHeight);
            HStack(go, Metrics.Spacing, new RectOffset(12, 12, 4, 4));

            var label = Text(go.transform, "Label", "", Metrics.FontSize, TextAlignmentOptions.MidlineLeft);
            Size(label.gameObject, flexibleWidth: 1f);

            TextMeshProUGUI value = null;
            Selectable primary;
            OptionRow row;
            switch (kind)
            {
                case RowKind.Toggle:
                {
                    var toggle = CreateToggle(go.transform);
                    primary = toggle;
                    var toggleRow = go.AddComponent<ToggleRow>();
                    toggleRow.Configure(toggle);
                    row = toggleRow;
                    break;
                }
                case RowKind.Slider:
                {
                    var slider = CreateSlider(go.transform, Metrics.ControlWidth - 64f);
                    value = Text(go.transform, "Value", "", Metrics.SmallFontSize, TextAlignmentOptions.MidlineRight, Palette.TextDim);
                    Size(value.gameObject, width: 58f);
                    primary = slider;
                    var sliderRow = go.AddComponent<SliderRow>();
                    sliderRow.Configure(slider);
                    row = sliderRow;
                    break;
                }
                case RowKind.Text:
                {
                    var input = CreateInputField(go.transform, Metrics.ControlWidth);
                    primary = input;
                    var textRow = go.AddComponent<TextRow>();
                    textRow.Configure(input);
                    row = textRow;
                    break;
                }
                default:
                {
                    primary = CreateStepper(go.transform, Metrics.ControlWidth, out var previous, out var next, out value);
                    var stepperRow = go.AddComponent<StepperRow>();
                    stepperRow.Configure(previous, next);
                    row = stepperRow;
                    break;
                }
            }

            var reset = Button(go.transform, "Reset", "Reset", 58f, Metrics.RowHeight - 12f);
            reset.GetComponentInChildren<TMP_Text>().fontSize = Metrics.SmallFontSize - 3f;
            row.ConfigureBase(label, value, reset, primary, rowImage);
            return go;
        }

        public static Toggle CreateToggle(Transform parent)
        {
            var background = Panel(parent, "Toggle", Palette.Control);
            background.raycastTarget = true;
            Size(background.gameObject, 28f, 28f);
            var toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.colors = TintColors();
            var checkmark = Panel(background.transform, "Checkmark", Palette.Accent);
            Stretch(checkmark.rectTransform, 6f);
            toggle.graphic = checkmark;
            toggle.isOn = false;
            return toggle;
        }

        public static Slider CreateSlider(Transform parent, float width)
        {
            var root = Rect(parent, "Slider");
            Size(root.gameObject, width, 24f);
            var slider = root.gameObject.AddComponent<Slider>();

            var background = Panel(root, "Background", Palette.Control);
            background.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            var fillArea = Rect(root, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.35f);
            fillArea.anchorMax = new Vector2(1f, 0.65f);
            fillArea.offsetMin = new Vector2(7f, 0f);
            fillArea.offsetMax = new Vector2(-7f, 0f);
            var fill = Panel(fillArea, "Fill", Palette.Accent);
            Stretch(fill.rectTransform);

            var handleArea = Rect(root, "Handle Slide Area");
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(7f, 0f);
            handleArea.offsetMax = new Vector2(-7f, 0f);
            var handle = Panel(handleArea, "Handle", Color.white);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(14f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            var colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = Palette.Accent;
            colors.selectedColor = Palette.Accent;
            colors.pressedColor = Color.Lerp(Palette.Accent, Color.white, 0.5f);
            slider.colors = colors;
            return slider;
        }

        /// <summary>A selectable bar with previous/next buttons and a value label between them.</summary>
        public static Selectable CreateStepper(Transform parent, float width, out Button previous, out Button next, out TextMeshProUGUI value)
        {
            var background = Panel(parent, "Stepper", Palette.Control);
            background.raycastTarget = true;
            // flexibleWidth 0: the inner layout group would otherwise report its label's flexible width and stretch the control.
            Size(background.gameObject, width, Metrics.RowHeight - 10f, flexibleWidth: 0f);
            HStack(background.gameObject, 2f, new RectOffset(2, 2, 2, 2));
            var selectable = background.gameObject.AddComponent<Selectable>();
            selectable.targetGraphic = background;
            selectable.colors = TintColors();

            previous = Button(background.transform, "Previous", "<", 30f, Metrics.RowHeight - 14f);
            value = Text(background.transform, "Value", "", Metrics.SmallFontSize, TextAlignmentOptions.Center);
            Size(value.gameObject, flexibleWidth: 1f);
            next = Button(background.transform, "Next", ">", 30f, Metrics.RowHeight - 14f);
            return selectable;
        }

        public static TMP_InputField CreateInputField(Transform parent, float width)
        {
            var background = Panel(parent, "Input", Palette.Control);
            background.raycastTarget = true;
            Size(background.gameObject, width, Metrics.RowHeight - 10f);
            var input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.colors = TintColors();

            var area = Rect(background.transform, "Text Area");
            Stretch(area, 6f);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(area, "Placeholder", "", Metrics.SmallFontSize, TextAlignmentOptions.MidlineLeft, Palette.TextDim);
            Stretch(placeholder.rectTransform);
            var text = Text(area, "Text", "", Metrics.SmallFontSize, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            text.overflowMode = TextOverflowModes.Overflow;

            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        public static GameObject CreateHeader(Transform parent, string text = "")
        {
            var header = Text(parent, "Header", text, Metrics.SmallFontSize, TextAlignmentOptions.BottomLeft, Palette.TextDim);
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 4f;
            Size(header.gameObject, height: 30f);
            return header.gameObject;
        }

        public static GameObject CreateSpacer(Transform parent, float height)
        {
            var spacer = Rect(parent, "Spacer");
            Size(spacer.gameObject, height: height);
            return spacer.gameObject;
        }

        public static Button CreateTabButton(Transform parent, string label) => Button(parent, "Tab " + label, label, 150f);

        // ---- chrome -----------------------------------------------------------------------------------------------

        public static DefaultConfirmPrompt CreateConfirmPrompt(Transform parent)
        {
            var overlay = Panel(parent, "Confirm Prompt", Palette.Overlay);
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);
            Size(overlay.gameObject).ignoreLayout = true;

            var panel = Panel(overlay.transform, "Panel", Palette.Panel);
            panel.raycastTarget = true;
            panel.rectTransform.sizeDelta = new Vector2(460f, 190f);
            VStack(panel.gameObject, 10f, new RectOffset(20, 20, 20, 20));

            var message = Text(panel.transform, "Message", "Keep these settings?", Metrics.FontSize, TextAlignmentOptions.Center);
            message.textWrappingMode = TextWrappingModes.Normal;
            Size(message.gameObject, height: 56f);
            var countdown = Text(panel.transform, "Countdown", "", Metrics.SmallFontSize, TextAlignmentOptions.Center, Palette.TextDim);
            Size(countdown.gameObject, height: 24f);

            var buttons = Rect(panel.transform, "Buttons");
            Size(buttons.gameObject, height: Metrics.ButtonHeight);
            HStack(buttons.gameObject, 12f, null, TextAnchor.MiddleCenter);
            var keep = Button(buttons, "Keep", "Keep", 150f);
            var revert = Button(buttons, "Revert", "Revert", 150f);

            var prompt = overlay.gameObject.AddComponent<DefaultConfirmPrompt>();
            prompt.Configure(message, countdown, keep, revert);
            overlay.gameObject.SetActive(false);
            return prompt;
        }

        /// <summary>A complete menu (title, tabs, scrolling rows, description, Back / Reset / Apply, confirm prompt) with an <see cref="OptionsMenu"/> wired.</summary>
        public static OptionsMenu CreateMenu(Transform parent, string title = "Options")
        {
            var root = Panel(parent, "Options Menu", Palette.Panel);
            root.raycastTarget = true;
            if (Metrics.MenuSize.x > 0f && Metrics.MenuSize.y > 0f)
                root.rectTransform.sizeDelta = Metrics.MenuSize;
            else
                Stretch(root.rectTransform);
            VStack(root.gameObject, Metrics.Spacing, Metrics.MenuPadding);

            var titleText = Text(root.transform, "Title", title, Metrics.TitleFontSize, TextAlignmentOptions.MidlineLeft);
            Size(titleText.gameObject, height: 40f);

            var tabBar = Rect(root.transform, "Tabs");
            Size(tabBar.gameObject, height: Metrics.ButtonHeight);
            HStack(tabBar.gameObject, 6f);

            var scrollRect = Rect(root.transform, "Scroll View");
            Size(scrollRect.gameObject, flexibleHeight: 1f);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = Panel(scrollRect, "Viewport", Palette.Viewport);
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VStack(content.gameObject, 4f, new RectOffset(8, 8, 8, 8));
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;

            var description = Text(root.transform, "Description", "", Metrics.SmallFontSize, TextAlignmentOptions.TopLeft, Palette.TextDim);
            description.textWrappingMode = TextWrappingModes.Normal;
            Size(description.gameObject, height: 48f);

            var footer = Rect(root.transform, "Footer");
            Size(footer.gameObject, height: Metrics.ButtonHeight);
            HStack(footer.gameObject, 8f);
            var back = Button(footer, "Back", "Back", 120f);
            var reset = Button(footer, "Reset", "Reset page", 140f);
            Size(Rect(footer, "Spacer").gameObject, flexibleWidth: 1f);
            var apply = Button(footer, "Apply", "Apply", 120f);

            var prompt = CreateConfirmPrompt(root.transform);

            // Add the component while inactive: OnEnable would otherwise open the menu before it is configured.
            root.gameObject.SetActive(false);
            var menu = root.gameObject.AddComponent<OptionsMenu>();
            menu.Configure(tabBar, content, scroll, titleText, description, back, reset, apply, prompt);
            root.gameObject.SetActive(true);
            return menu;
        }
    }
}
