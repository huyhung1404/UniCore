using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniCore.Editor.AttributesDrawer
{
    public class InterfaceSelectorWindow : EditorWindow
    {
        public class Tab : Toggle
        {
            public Tab(string text)
            {
                this.text = text;
                RemoveFromClassList(ussClassName);
                AddToClassList(ussClassName);
            }
        }

        public class ToggleGroup
        {
            private readonly List<Toggle> _toggles = new List<Toggle>();

            public event EventHandler<Toggle> OnToggleChanged;

            public void RegisterToggle(Toggle toggle)
            {
                if (toggle == null || _toggles.Contains(toggle)) return;

                _toggles.Add(toggle);
                toggle.RegisterValueChangedCallback(ToggleValueChanged);
            }

            public void UnregisterToggle(Toggle toggle)
            {
                if (!_toggles.Remove(toggle)) return;

                toggle.UnregisterValueChangedCallback(ToggleValueChanged);
            }

            public void Validate()
            {
                if (_toggles.Count == 0) return;

                var activeToggle = GetFirstActiveToggle();
                if (activeToggle == null)
                {
                    activeToggle = _toggles[0];
                    activeToggle.value = true;
                }

                foreach (var toggle in _toggles.Where(toggle => toggle.value))
                {
                    toggle.SetValueWithoutNotify(false);
                }
            }

            public Toggle GetFirstActiveToggle()
            {
                return _toggles.Find(x => x.value);
            }

            public bool IsAnyOn()
            {
                return GetFirstActiveToggle() != null;
            }

            private void ToggleValueChanged(ChangeEvent<bool> evt)
            {
                HandleToggleChanged(evt.target as Toggle);
            }

            private void HandleToggleChanged(Toggle targetToggle)
            {
                ValidateToggleIsInGroup(targetToggle);

                foreach (var toggle in _toggles.Where(toggle => toggle != targetToggle))
                {
                    toggle.SetValueWithoutNotify(false);
                }

                if (targetToggle.value)
                    OnToggleChanged?.Invoke(this, targetToggle);
                else
                    targetToggle.value = true;
            }

            private void ValidateToggleIsInGroup(Toggle toggle)
            {
                if (toggle == null || !_toggles.Contains(toggle))
                    throw new ArgumentException(string.Format("Toggle {0} is not part of ToggleGroup {1}", new object[] { toggle, this }));
            }
        }

        public class ItemInfo
        {
            public Texture Icon;
            public int? InstanceID;
            public string Label;
        }

        internal static InterfaceSelectorWindow s_instance { get; private set; }
        private static readonly ItemInfo s_nullItem = new ItemInfo() { InstanceID = null, Label = "None" };
        private Action<Object> _selectionChangedCallback;
        private Action<Object, bool> _selectorClosedCallback;
        private ObjectSelectorFilter _filter;
        private SerializedProperty _editingProperty;
        private List<ItemInfo> _allItems;
        private List<ItemInfo> _filteredItems;
        private ItemInfo _currentItem;
        private string _searchText;
        private bool _userCanceled;
        private bool _showSceneObjects = true;
        private int _undoGroup;
        private ToolbarSearchField _searchBox;
        private ListView _listView;
        private Label _detailsLabel;
        private Label _detailsIndexLabel;
        private Label _detailsTypeLabel;
        private Tab _sceneTab;
        private Tab _assetsTab;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterItems();
            }
        }

        public static void Show(SerializedProperty property,
            Action<Object> onSelectionChanged,
            Action<Object, bool> onSelectorClosed,
            ObjectSelectorFilter filter,
            Type objectType)
        {
            if (s_instance == null) s_instance = CreateInstance<InterfaceSelectorWindow>();
            var isScriptableObject = objectType.IsSubclassOf(typeof(ScriptableObject)) || objectType == typeof(ScriptableObject);
            s_instance._showSceneObjects = !isScriptableObject;
            s_instance._editingProperty = property;
            s_instance._selectionChangedCallback = onSelectionChanged;
            s_instance._selectorClosedCallback = onSelectorClosed;
            s_instance._filter = filter;
            s_instance.Init();
            s_instance.ShowAuxWindow();
        }

        private void Init()
        {
            InitData();
            InitVisualElements();
            BindVisualElements();
            FinishInit();
        }

        private void InitData()
        {
            _undoGroup = Undo.GetCurrentGroup();
            _searchText = "";
            _allItems = new List<ItemInfo>();
            _filteredItems = new List<ItemInfo>();

            var target = _editingProperty.objectReferenceValue;
            if (target != null) _showSceneObjects = !AssetDatabase.Contains(target);

            PopulateItems();
            FilterItems();
        }

        private void InitVisualElements()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.huyhung1404.unicore/Editor/AttributesDrawer/InterfaceReference/Style.uss");

            rootVisualElement.styleSheets.Add(styleSheet);

            _searchBox = new ToolbarSearchField();
            _searchBox.RegisterValueChangedCallback(SearchFilterChanged);
            rootVisualElement.Add(_searchBox);

            var tabContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            _assetsTab = new Tab("Assets");
            _sceneTab = new Tab("Scene");
            tabContainer.Add(_assetsTab);
            tabContainer.Add(_sceneTab);
            rootVisualElement.Add(tabContainer);

            _listView = new ListView(_filteredItems, 16, MakeItem, BindItem);
            _listView.selectionChanged += ItemSelectionChanged;
            _listView.itemsChosen += ItemsChosen;
            rootVisualElement.Add(_listView);

            _detailsLabel = new Label();
            _detailsTypeLabel = new Label();
            _detailsIndexLabel = new Label();

            var details = new VisualElement();
            details.AddToClassList("details");
            details.Add(_detailsLabel);
            details.Add(_detailsIndexLabel);
            details.Add(_detailsTypeLabel);
            rootVisualElement.Add(details);
        }

        private void BindVisualElements()
        {
            var activeTab = _showSceneObjects ? _sceneTab : _assetsTab;
            activeTab.SetValueWithoutNotify(true);

            var toggleGroup = new ToggleGroup();
            toggleGroup.RegisterToggle(_assetsTab);
            toggleGroup.RegisterToggle(_sceneTab);
            toggleGroup.OnToggleChanged += HandleGroupChanged;

            if (GetIndexOfEditingPropertyValue(out var index))
                _listView.selectedIndex = index;
        }

        private void FinishInit()
        {
            EditorApplication.delayCall += () => { _listView.Focus(); };
        }

        private bool GetIndexOfEditingPropertyValue(out int index)
        {
            index = -1;
            var targetObject = _editingProperty.objectReferenceValue;
            if (targetObject)
            {
                var instanceID = targetObject.GetInstanceID();
                index = _filteredItems.FindIndex(x => x.InstanceID == instanceID);
            }

            return index >= 0;
        }

        private bool GetIndexOfCurrentItem(out int index)
        {
            index = -1;
            if (_currentItem != null)
                index = _filteredItems.FindIndex(0, x => x.InstanceID == _currentItem.InstanceID);
            return index >= 0;
        }

        private void HandleGroupChanged(object sender, Toggle toggle)
        {
            if (_showSceneObjects && toggle == this._sceneTab) return;
            _showSceneObjects = !_showSceneObjects;
            PopulateItems();
            FilterItems();
            var list = new List<int>();
            if (GetIndexOfCurrentItem(out var index)) list.Add(index);
            _listView.SetSelectionWithoutNotify(list);
            _listView.Focus();
        }

        private void OnDisable()
        {
            _selectorClosedCallback?.Invoke(GetCurrentObject(), _userCanceled);
            if (_userCanceled)
                Undo.RevertAllDownToGroup(_undoGroup);
            else
                Undo.CollapseUndoOperations(_undoGroup);
            s_instance = null;
        }

        private void PopulateItems()
        {
            _allItems.Clear();
            _filteredItems.Clear();
            _allItems.AddRange(_showSceneObjects ? FetchAllComponents() : FetchAllAssets());
            _allItems.Sort((item, other) => string.Compare(item.Label, other.Label, StringComparison.Ordinal));
        }

        private void SearchFilterChanged(ChangeEvent<string> evt)
        {
            SearchText = evt.newValue;
        }

        private void FilterItems()
        {
            _filteredItems.Clear();
            _filteredItems.Add(s_nullItem);
            _filteredItems.AddRange(_allItems.Where(item =>
                string.IsNullOrEmpty(SearchText) || item.Label.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0));

            _listView?.Rebuild();
        }

        private void BindItem(VisualElement listItem, int index)
        {
            if (index < 0 || index >= _filteredItems.Count)
                return;

            var label = listItem.Q<Label>();
            if (label != null)
                label.text = _filteredItems[index].Label;
            var image = listItem.Q<Image>();
            image.image = _filteredItems[index].Icon;
        }

        private static VisualElement MakeItem()
        {
            var ve = new VisualElement();
            var image = new Image();
            var label = new Label();
            ve.Add(image);
            ve.Add(label);

            ve.AddToClassList("list-item");
            label.AddToClassList("list-item__text");
            image.AddToClassList("list-item__icon");

            return ve;
        }

        private void ItemSelectionChanged(IEnumerable<object> selectedItems)
        {
            _currentItem = selectedItems.FirstOrDefault() as ItemInfo;
            UpdateDetails();
            _selectionChangedCallback?.Invoke(GetCurrentObject());
        }

        private void ItemsChosen(IEnumerable<object> selectedItems)
        {
            _currentItem = selectedItems.FirstOrDefault() as ItemInfo;
            _userCanceled = false;
            Close();
        }

        private void UpdateDetails()
        {
            GetText(_currentItem, out var infoText, out var indexText, out var typeText);

            void SetText(Label label, string text)
            {
                label.text = String.IsNullOrEmpty(text) ? "" : text;
            }

            SetText(_detailsLabel, infoText);
            SetText(_detailsIndexLabel, indexText);
            SetText(_detailsTypeLabel, typeText);
        }

        private static void GetText(ItemInfo itemInfo, out string text, out string indexText, out string typeText)
        {
            text = null;
            indexText = null;
            typeText = null;

            if (itemInfo == null) return;
            if (itemInfo.InstanceID == null)
            {
                text = itemInfo.Label;
                return;
            }

            var obj = EditorUtility.InstanceIDToObject((int)itemInfo.InstanceID);
            if (AssetDatabase.Contains(obj))
            {
                text = AssetDatabase.GetAssetPath(obj);
            }
            else
            {
                var transform = obj is GameObject go ? go.transform : (obj as Component)?.transform;
                // ReSharper disable once CoVariantArrayConversion
                // ReSharper disable once PossibleNullReferenceException
                var compIndex = Array.IndexOf(transform.gameObject.GetComponents(typeof(Component)), obj);
                text = $"{GetTransformPath(transform)}";
                indexText = $"[{compIndex}]";
            }

            typeText = $"({obj.GetType().Name})";
        }

        private static string GetTransformPath(Transform transform)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(transform.name);
            while (transform.parent != null)
            {
                var parent = transform.parent;
                sb.Insert(0, parent.name + "/");
                transform = parent;
            }

            return sb.ToString();
        }

        private IEnumerable<ItemInfo> FetchAllAssets()
        {
            var property = new HierarchyProperty(HierarchyType.Assets, false);
            property.SetSearchFilter(_filter.AssetSearchFilter, 0);

            while (property.Next(null))
            {
                yield return new ItemInfo { Icon = property.icon, InstanceID = property.instanceID, Label = property.name };
            }
        }

        private IEnumerable<ItemInfo> FetchAllComponents()
        {
            var property = new HierarchyProperty(HierarchyType.GameObjects, false);

            while (property.Next(null))
            {
                var go = property.pptrValue as GameObject;
                if (go == null) continue;

                if (CheckFilter(go))
                    yield return new ItemInfo { Icon = property.icon, InstanceID = property.instanceID, Label = property.name };

                foreach (var comp in go.GetComponents(typeof(Component)))
                {
                    if (CheckFilter(comp))
                        yield return new ItemInfo
                            { Icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image, InstanceID = comp.GetInstanceID(), Label = property.name };
                }
            }
        }

        private bool CheckFilter(Object obj)
        {
            var matchFilterConstraint = _filter.SceneFilterCallback?.Invoke(obj);
            return (!matchFilterConstraint.HasValue || matchFilterConstraint.Value);
        }

        private Object GetCurrentObject()
        {
            if (_currentItem == null || _currentItem.InstanceID == null) return null;
            return EditorUtility.InstanceIDToObject((int)_currentItem.InstanceID);
        }
    }

    public class ObjectSelectorFilter
    {
        public readonly string AssetSearchFilter;
        public readonly Func<Object, bool> SceneFilterCallback;

        public ObjectSelectorFilter() : this("", _ => true)
        {
        }

        public ObjectSelectorFilter(string assetSearchFilter, Func<Object, bool> sceneFilterCallback)
        {
            AssetSearchFilter = assetSearchFilter;
            SceneFilterCallback = sceneFilterCallback;
        }
    }
}