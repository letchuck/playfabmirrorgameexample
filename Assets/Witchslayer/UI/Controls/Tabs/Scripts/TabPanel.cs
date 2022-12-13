using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using Witchslayer.Utilities;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Witchslayer.UI.Controls.Tabs
{

    public class TabPanel : MonoBehaviour
    {

        [Header("Tabs")]
        [SerializeField] private Transform _tabHolder = null;
        [SerializeField] private TabButton _tabPrefab = null;
        [SerializeField] private Color _baseTabColor = Color.white;

        [SerializeField, Range(1, 10)]
        private int _numTabs = 3;
        public int NumTabs
        {
            get => _numTabs;
            set => _numTabs = value;
        }

        /// <summary>
        /// Current tab index cache. Use CurTabIndex when setting.
        /// </summary>
        private int _curTabIndex = 0;
        /// <summary>
        /// Sanitizes the current tab index and regenerates tab colors on set 
        /// so it's never out of range according to _numTabs.
        /// </summary>
        public int CurTabIndex
        {
            get => _curTabIndex;
            set
            {
                // sanitize index
                _curTabIndex = Mathf.Clamp(value, 0, _numTabs - 1);
                SetTabColors();
            }
        }

        [SerializeField] private TabButton[] _tabButtons = null;
        public TabButton[] TabButtons => _tabButtons;

        [Header("Add Tab")]
        [SerializeField] private bool _showAddTab = true;
        public bool ShowAddTab
        {
            get => _showAddTab;
            set
            {
                // recreate tabs dynamically
                _showAddTab = value;
                StartCoroutine(RecreateTabs());
            }
        }
        private TabButton _addTab;
        public TabButton AddTab => _addTab;

        private void SetTabColors()
        {
            if (_tabHolder.childCount == 0) return;

            for (int i = 0; i < _tabHolder.childCount; i++)
            {
                Image tab = _tabHolder.GetChild(i).GetComponent<Image>();
                if (tab == null)
                {
                    Debug.LogError("Tab was null at index " + i);
                    continue;
                }

                if (CurTabIndex == i)
                    tab.color = _baseTabColor;
                else
                    tab.color = _baseTabColor / 1.2f;
            }

            // set the add tab to base color
            if (_showAddTab)
                _tabHolder.GetChild(_tabHolder.childCount-1).GetComponent<Image>().color = _baseTabColor;

            // also set the chat box color to match base
            GetComponent<Image>().color = _baseTabColor;
        }

        /// <summary>
        /// Recreates the tabs asynchronously (due to how Destroy works).
        /// Make sure to set NumTabs to the amount of tabs you want created before starting this coroutine.
        /// </summary>
        /// <param name="maintainIndex"></param>
        /// <param name="cb"></param>
        /// <returns></returns>
        public IEnumerator RecreateTabs(System.Action cb=null)
        {
            _tabHolder.DestroyChildren(_numTabs, _showAddTab && _addTab != null ? _addTab.transform : null);
            // must yield a frame so the children are completely destroyed
            // you may use `new WaitForEndOfFrame()` instead of `null` but it doesn't work in editor
            yield return null;

            // recache and recreate the tabs
            System.Array.Resize(ref _tabButtons, _numTabs);
            for (int i = 0; i < _numTabs; i++)
            {
                int index = i;
                if (_tabButtons[index] == null)
                    _tabButtons[index] = SafeInstantiateTab();

                // clear listeners
                _tabButtons[index].RemoveAllListeners();

                // callback for setting the tab index
                _tabButtons[index].Button.onClick.AddListener(() => CurTabIndex = index);
            }

            // recreate add tab?
            if (_showAddTab)
            {
                if (_addTab == null)
                    _addTab = SafeInstantiateTab();
                _addTab.RemoveAllListeners();
                _addTab.name = "AddTabButton";
                _addTab.Text.text = "+";
                _addTab.transform.SetAsLastSibling();
            }
            else _addTab = null;

            // dirty the tab index so it sanitizes it and recolors the tabs
            CurTabIndex = _curTabIndex;

            cb?.Invoke();
        }

        /// <summary>
        /// Safely instantiates the tab based on if in editor or not.
        /// </summary>
        /// <returns></returns>
        private TabButton SafeInstantiateTab()
        {
            if (Application.isPlaying)
            {
                return Instantiate(_tabPrefab, _tabHolder);
            }

#if UNITY_EDITOR
            return PrefabUtility.InstantiatePrefab(_tabPrefab, _tabHolder) as TabButton;
#else
            return null;
#endif
        }

        /// <summary>
        /// Selects the last tab that isn't the add tab.
        /// </summary>
        public void SelectLastTab()
        {
            CurTabIndex = _tabButtons.Length;
        }


#if UNITY_EDITOR

        private void OnValidate()
        {
            // allow recreation in editor
            if (!Application.isPlaying)
            {
                _tabButtons = _tabHolder.GetComponentsInChildren<TabButton>().Where(x=>x!= _addTab).ToArray();

                // same amount of tabs?
                int count = _numTabs;
                if (_showAddTab)
                    count++;
                if (_tabHolder.childCount != count)
                    EditorApplication.delayCall = () => StartCoroutine(RecreateTabs());
                else
                    SetTabColors();
            }
        }

#endif

    }

}
