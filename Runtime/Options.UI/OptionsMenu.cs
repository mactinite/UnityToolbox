using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>
    /// The ready-made settings screen: pages as tabs, scrolling rows, a description for the selected row, Back,
    /// Reset page and Apply (for staged <see cref="OptionFlags.Confirm"/> options, followed by the confirm prompt).
    /// Wire the fields on your own hierarchy, or build the plain version with <see cref="CreateDefault"/>.
    /// Hooks: <see cref="TextProvider"/>, <see cref="Feedback"/>, <see cref="ConfirmPrompt"/>, <see cref="Layout"/>,
    /// <see cref="Theme"/>, and the builder's events through <see cref="Builder"/> after <see cref="Open"/>.
    /// </summary>
    public sealed class OptionsMenu : MonoBehaviour
    {
        public enum SavePolicy
        {
            /// <summary>Save whenever a value is committed (toggle flipped, slider released).</summary>
            OnCommit,
            /// <summary>Save once when the menu closes or is disabled.</summary>
            OnClose,
            /// <summary>The game saves the store itself.</summary>
            Manual,
        }

        [Header("Content")]
        [SerializeField] OptionsTheme theme;
        [Tooltip("Arrangement; empty means one page per category. A layout set from code wins over this asset.")]
        [SerializeField] OptionsLayoutAsset layoutAsset;
        [SerializeField] string title = "Options";

        [Header("Hierarchy")]
        [SerializeField] RectTransform tabBar;
        [SerializeField] RectTransform content;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Button backButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button applyButton;
        [Tooltip("An object with an IConfirmPrompt. Empty means the built-in prompt.")]
        [SerializeField] GameObject confirmPromptObject;

        [Header("Behaviour")]
        [SerializeField] SavePolicy savePolicy = SavePolicy.OnCommit;
        [SerializeField] bool openOnEnable = true;
        [SerializeField] bool rememberPage = true;
        [SerializeField] float confirmSeconds = 15f;
        [SerializeField] string confirmMessage = "Keep these display settings?";

        readonly List<OptionRow> rows = new List<OptionRow>();
        readonly List<Button> tabButtons = new List<Button>();
        OptionsLayout activeLayout;
        OptionsStore activeStore;
        IConfirmPrompt activePrompt;
        bool refreshQueued;
        int lastPage;

        /// <summary>The store to show; the default store when null.</summary>
        public OptionsStore Store { get; set; }

        /// <summary>A layout built in code. Wins over the layout asset.</summary>
        public OptionsLayout Layout { get; set; }

        public OptionsTheme Theme
        {
            get => theme;
            set => theme = value;
        }

        public IOptionTextProvider TextProvider { get; set; }
        public IOptionsMenuFeedback Feedback { get; set; }
        public IConfirmPrompt ConfirmPrompt { get; set; }
        /// <summary>Whether dev-only options are shown; default: development builds and the editor.</summary>
        public Func<bool> ShowDevOnly { get; set; }

        public SavePolicy Saving
        {
            get => savePolicy;
            set => savePolicy = value;
        }

        public bool IsOpen { get; private set; }
        public int CurrentPage { get; private set; }
        public OptionsMenuContext Context { get; private set; }
        public OptionsTransaction Transaction { get; private set; }
        public OptionsMenuBuilder Builder { get; private set; }
        public IReadOnlyList<OptionRow> Rows => rows;
        public OptionsLayout ActiveLayout => activeLayout;

        public event Action Opened;
        public event Action Closed;
        public event Action<int> PageShown;

        /// <summary>The builder for this opening exists but no page is built yet: subscribe to its row and element events here.</summary>
        public event Action<OptionsMenuBuilder> BuilderCreated;

        /// <summary>The plain built-in menu under <paramref name="parent"/>, not yet opened.</summary>
        public static OptionsMenu CreateDefault(Transform parent, string title = "Options")
        {
            var menu = DefaultOptionsUI.CreateMenu(parent, title);
            menu.openOnEnable = false;
            return menu;
        }

        void OnEnable()
        {
            if (openOnEnable && !IsOpen)
                Open();
        }

        void OnDisable()
        {
            if (IsOpen)
                Teardown();
        }

        void LateUpdate()
        {
            if (!refreshQueued)
                return;
            refreshQueued = false;
            RefreshRows();
        }

        /// <summary>Builds the tabs and the remembered page. Call <see cref="Close"/> to tear down.</summary>
        public void Open()
        {
            if (IsOpen)
                return;
            activeStore = Store ?? OptionsStore.Default;
            if (!activeStore.IsLoaded)
                Debug.LogWarning("[Options] The store has not been loaded; the menu shows defaults.", this);

            Transaction = OptionsTransaction.ForFlagged(activeStore);
            Context = new OptionsMenuContext(activeStore, Transaction, TextProvider, Feedback, ShowDevOnly);
            Context.RowSelected += OnRowSelected;
            Context.ValueCommitted += OnValueCommitted;
            Context.StagedChanged += OnStagedChanged;
            Context.CloseRequested += Close;
            activeStore.OptionChanged += OnOptionChanged;

            activeLayout = Layout ?? (layoutAsset != null ? layoutAsset.ToLayout() : OptionsLayout.FromStore(activeStore));
            Builder = new OptionsMenuBuilder(Context, theme);
            BuilderCreated?.Invoke(Builder);

            if (titleText != null)
                titleText.text = title;
            Hook(backButton, Close);
            Hook(resetButton, ResetPage);
            Hook(applyButton, Apply);

            BuildTabs();
            IsOpen = true;
            ShowPage(rememberPage ? lastPage : 0);
            Opened?.Invoke();
        }

        /// <summary>Discards staged edits, reverts anything applied but unconfirmed, saves per policy, and clears the rows.</summary>
        public void Close()
        {
            if (!IsOpen)
                return;
            Teardown();
            Closed?.Invoke();
        }

        public void ShowPage(int index)
        {
            if (!IsOpen || activeLayout.Pages.Count == 0)
                return;
            index = Mathf.Clamp(index, 0, activeLayout.Pages.Count - 1);
            CurrentPage = index;
            lastPage = index;

            ClearRows();
            rows.AddRange(Builder.BuildPage(activeLayout.Pages[index], content));
            for (int i = 0; i < tabButtons.Count; i++)
                TintTab(tabButtons[i], i == index);
            OptionsMenuBuilder.WireNavigation(rows, ActiveTab(), backButton);
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
            SetDescription("");
            RefreshChrome();
            SelectFirstRow();

            PageShown?.Invoke(index);
            Feedback?.OnPageShown(index);
        }

        /// <summary>Applies the staged display-style changes and asks whether to keep them.</summary>
        public void Apply()
        {
            if (!IsOpen || Transaction == null || !Transaction.HasChanges)
                return;
            Transaction.Apply();
            RefreshRows();

            var prompt = ResolvePrompt();
            if (prompt == null)
            {
                Transaction.Commit();
                RefreshRows();
                return;
            }

            prompt.Show(confirmMessage, confirmSeconds, keep: () =>
            {
                Transaction.Commit();
                RefreshRows();
            }, revert: () =>
            {
                Transaction.Revert();
                RefreshRows();
            });
        }

        /// <summary>Resets the options on the current page. Staged options are staged at their default, so Apply still confirms them.</summary>
        public void ResetPage()
        {
            if (!IsOpen)
                return;
            bool committed = false;
            foreach (var row in rows)
            {
                if (row == null)
                    continue;
                var option = row.Option;
                if (Context.IsStaged(option))
                {
                    Transaction.Set(option, option.BoxedDefault);
                }
                else if (!option.IsDefault)
                {
                    option.Reset();
                    committed = true;
                }
            }

            if (committed && savePolicy == SavePolicy.OnCommit)
                activeStore.Save();
            RefreshRows();
        }

        /// <summary>Puts focus on the first row of the page, for gamepads and keyboards.</summary>
        public void SelectFirstRow()
        {
            var first = OptionsMenuBuilder.FirstSelectable(rows);
            if (first != null)
                first.Select();
            else if (EventSystem.current != null && backButton != null)
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        void Teardown()
        {
            IsOpen = false;
            activePrompt?.Hide();
            if (Transaction != null)
            {
                Transaction.Discard();
                if (Transaction.IsApplied)
                    Transaction.Revert();
            }

            if (savePolicy != SavePolicy.Manual)
                activeStore?.SaveIfDirty();

            if (activeStore != null)
                activeStore.OptionChanged -= OnOptionChanged;
            if (Context != null)
            {
                Context.RowSelected -= OnRowSelected;
                Context.ValueCommitted -= OnValueCommitted;
                Context.StagedChanged -= OnStagedChanged;
                Context.CloseRequested -= Close;
            }

            Unhook(backButton, Close);
            Unhook(resetButton, ResetPage);
            Unhook(applyButton, Apply);
            ClearRows();
            ClearTabs();
            Transaction = null;
            Context = null;
        }

        void BuildTabs()
        {
            ClearTabs();
            if (tabBar == null)
                return;
            bool several = activeLayout.Pages.Count > 1;
            tabBar.gameObject.SetActive(several);
            if (!several)
                return;

            for (int i = 0; i < activeLayout.Pages.Count; i++)
            {
                var page = activeLayout.Pages[i];
                string label = Context.Text.PageLabel(page);
                var button = theme != null && theme.tabButton != null
                    ? Instantiate(theme.tabButton, tabBar).GetComponent<Button>()
                    : DefaultOptionsUI.CreateTabButton(tabBar, label);
                if (button == null)
                {
                    Debug.LogError("[Options] The theme's tab button prefab has no Button.", theme);
                    continue;
                }

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = label;
                int pageIndex = i;
                button.onClick.AddListener(() => ShowPage(pageIndex));
                tabButtons.Add(button);
            }

            for (int i = 0; i < tabButtons.Count; i++)
            {
                var navigation = tabButtons[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = i > 0 ? tabButtons[i - 1] : null;
                navigation.selectOnRight = i < tabButtons.Count - 1 ? tabButtons[i + 1] : null;
                navigation.selectOnUp = null;
                tabButtons[i].navigation = navigation;
            }
        }

        Selectable ActiveTab() => CurrentPage >= 0 && CurrentPage < tabButtons.Count ? tabButtons[CurrentPage] : null;

        void TintTab(Button button, bool active)
        {
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.color = active ? DefaultOptionsUI.Palette.Accent : DefaultOptionsUI.Palette.Text;
            if (button.targetGraphic != null)
                button.targetGraphic.color = active ? new Color(1f, 1f, 1f, 0.3f) : DefaultOptionsUI.Palette.Button;
        }

        void RefreshRows()
        {
            if (!IsOpen)
                return;
            OptionsMenuBuilder.RefreshRows(rows, ActiveTab(), backButton);
            RefreshChrome();
        }

        void RefreshChrome()
        {
            if (applyButton != null)
                applyButton.gameObject.SetActive(Transaction != null && Transaction.HasChanges);
            if (resetButton != null)
            {
                bool anyChanged = false;
                foreach (var row in rows)
                {
                    if (row != null && row.gameObject.activeSelf && !Context.ShowsDefault(row.Option))
                    {
                        anyChanged = true;
                        break;
                    }
                }

                resetButton.interactable = anyChanged;
            }
        }

        void OnOptionChanged(Option option) => refreshQueued = true;

        void OnStagedChanged(Option option) => refreshQueued = true;

        void OnValueCommitted(Option option)
        {
            if (savePolicy == SavePolicy.OnCommit)
                activeStore.Save();
        }

        void OnRowSelected(OptionRow row)
        {
            SetDescription(Context.Text.Description(row.Option, row.Entry));
            ScrollTo(row);
        }

        void SetDescription(string text)
        {
            if (descriptionText != null)
                descriptionText.text = text ?? "";
        }

        /// <summary>Scrolls the content so the row is inside the viewport.</summary>
        public void ScrollTo(OptionRow row)
        {
            if (scrollRect == null || scrollRect.viewport == null || row == null)
                return;
            Canvas.ForceUpdateCanvases();
            var viewport = scrollRect.viewport;
            var contentRect = scrollRect.content;
            var rowRect = (RectTransform)row.transform;

            float contentHeight = contentRect.rect.height;
            float viewHeight = viewport.rect.height;
            if (contentHeight <= viewHeight)
                return;

            // Row bounds in content space, measured from the top.
            var corners = new Vector3[4];
            rowRect.GetWorldCorners(corners);
            float rowTop = -contentRect.InverseTransformPoint(corners[1]).y;
            float rowBottom = -contentRect.InverseTransformPoint(corners[0]).y;

            float scrollable = contentHeight - viewHeight;
            float viewTop = (1f - scrollRect.verticalNormalizedPosition) * scrollable;
            float viewBottom = viewTop + viewHeight;

            float target = viewTop;
            if (rowTop < viewTop)
                target = rowTop - 8f;
            else if (rowBottom > viewBottom)
                target = rowBottom - viewHeight + 8f;
            else
                return;

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - target / scrollable);
        }

        IConfirmPrompt ResolvePrompt()
        {
            if (ConfirmPrompt != null)
                return activePrompt = ConfirmPrompt;
            if (activePrompt != null)
                return activePrompt;
            if (confirmPromptObject != null)
                return activePrompt = confirmPromptObject.GetComponent<IConfirmPrompt>();
            if (theme != null && theme.confirmPrompt != null)
                return activePrompt = Instantiate(theme.confirmPrompt, transform).GetComponent<IConfirmPrompt>();
            return activePrompt = DefaultOptionsUI.CreateConfirmPrompt(transform);
        }

        void ClearRows()
        {
            rows.Clear();
            if (content == null)
                return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        void ClearTabs()
        {
            foreach (var button in tabButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            tabButtons.Clear();
        }

        static void Hook(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        static void Unhook(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        internal void Configure(RectTransform tabs, RectTransform rowsContent, ScrollRect scroll, TMP_Text titleLabel, TMP_Text description,
            Button back, Button reset, Button apply, DefaultConfirmPrompt prompt)
        {
            tabBar = tabs;
            content = rowsContent;
            scrollRect = scroll;
            titleText = titleLabel;
            descriptionText = description;
            backButton = back;
            resetButton = reset;
            applyButton = apply;
            confirmPromptObject = prompt != null ? prompt.gameObject : null;
            openOnEnable = false;
        }
    }
}
