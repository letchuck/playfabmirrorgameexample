using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Witchslayer.Attributes;

namespace Witchslayer.Chat.UI
{

    /// <summary>
    /// UI element responsible for managing chat channel toggles and chat tab save/removal functionality.
    /// </summary>
    public class UITabChannelMenu : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IDeselectHandler
    {

        /// <summary>
        /// Gather current TabChannels.
        /// </summary>
        public TabChannels TabChannels => new TabChannels() 
        { 
            Channels = this.Channels, 
            TabName = this.TabName 
        };

        /// <summary>
        /// Desired chat tab name.
        /// </summary>
        public string TabName => _tabNameInput.text;

        /// <summary>
        /// Desired chat tab channels.
        /// </summary>
        public ChatChannel Channels
        {
            get
            {
                ChatChannel channels = 0;
                for (int i = 0; i < _channelToggles.Count; i++)
                {
                    if (_channelToggles[i].isOn)
                        channels |= ChatUtils.IndexToFlag(i);
                }
                return channels;
            }
        }

        [Header("UI")]
        [Tooltip("Child panel that holds all the UI.")]
        [SerializeField] private GameObject _panel = null;
        [Tooltip("Input field that designates the tab name.")]
        [SerializeField] private TMP_InputField _tabNameInput = null;
        [Tooltip("Button used for saving callbacks.")]
        [SerializeField] private Button _saveButton = null;
        [Tooltip("Button used for removal callbacks.")]
        [SerializeField] private Button _removeButton = null;

        [Header("Chat Channel Toggles")]
        [Tooltip("Custom UI toggle prefab.")]
        [SerializeField] private Toggle _togglePrefab = null;
        [Tooltip("Layout group for the list of toggle flags.")]
        [SerializeField] private GridLayoutGroup _toggleGrid = null;
        [Tooltip("List of toggles in order of ChatChannels flags for better organization.")]
        [EnumNameArray(typeof(ChatChannel))]
        [SerializeField] private List<Toggle> _channelToggles = new List<Toggle>();

        /// <summary>
        /// Cached values of the ChatChannels enum for optimization.
        /// </summary>
        public System.Array EnumValues;
        /// <summary>
        /// True if mouse entered the menu. Used for knowing if the mouse is clicking off the menu to close it.
        /// </summary>
        private bool _mouseOver = false;

        private void Awake()
        {
            EnumValues = System.Enum.GetValues(typeof(ChatChannel));

            // init toggles
            foreach (var item in EnumValues)
            {
                Toggle toggle = Instantiate(_togglePrefab, _toggleGrid.transform);
                TMP_Text text = toggle.GetComponentInChildren<TMP_Text>();
                text.text = item.ToString();
                text.fontSize = _tabNameInput.pointSize;
                _channelToggles.Add(toggle);
            }

            // move save button last
            _saveButton.transform.SetAsLastSibling();
        }

        private void Start()
        {
            // hide on start if left open in editor
            Close();
        }

        public void Open(Vector2 pos, TabChannels tabChannels,
            System.Action saveAction = null, System.Action removeAction = null)
        {
            _panel.transform.position = pos;
            _panel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(gameObject);

            // init save button
            _saveButton.onClick.RemoveAllListeners();
            _saveButton.gameObject.SetActive(saveAction != null);
            if (saveAction != null)
                _saveButton.onClick.AddListener(() => saveAction.Invoke());
            _saveButton.transform.SetAsLastSibling();

            // init remove button
            _removeButton.onClick.RemoveAllListeners();
            _removeButton.gameObject.SetActive(removeAction != null);
            if (removeAction != null)
                _removeButton.onClick.AddListener(() => removeAction.Invoke());
            _removeButton.transform.SetAsLastSibling();

            // init tab name input field
            _tabNameInput.text = tabChannels.TabName;

            // init toggles with existing flag values
            for (int i = 0; i < _channelToggles.Count; i++)
                _channelToggles[i].isOn = tabChannels.Channels.HasFlag(ChatUtils.IndexToFlag(i));
        }

        public void Close()
        {
            _panel.SetActive(false);
            _saveButton.onClick.RemoveAllListeners();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            // close if clicked outside this menu
            if (!_mouseOver)
                Close();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOver = false;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOver = true;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

    }

}
