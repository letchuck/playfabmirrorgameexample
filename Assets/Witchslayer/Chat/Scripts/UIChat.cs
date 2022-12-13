using FishNet;

using System.Collections.Generic;

using TMPro;

using UnityEngine;

using Newtonsoft.Json;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Chat.MuteCheckers;
using Witchslayer.UI.Controls.Tabs;
using Witchslayer.Utilities;
using Witchslayer.Chat.ScriptableObjects;
using Witchslayer.Chat.Commands;

namespace Witchslayer.Chat.UI
{

    public class UIChat : MonoBehaviour
    {

        private const byte MAX_CHAT_MESSAGES = 200;
        private const byte MAX_SENT_MESSAGES = 200;
        private const string TAB_KEY = "ChatTabs";
        private const string SHOW_TIMESTAMPS_KEY = "ShowTimestamps";

        [Header("Data Refs")]
        [Tooltip("List of default chat tabs and their channels to assign to new chatters.")]
        [SerializeField] private DefaultTabChannels _defaultTabChannels = null;
        [Tooltip("List of data used for UI.")]
        [SerializeField] private ChatChannelData _chatChannelData = null;

        [Header("UI")]
        [Tooltip("Local client's chat input field.")]
        [SerializeField] private TMP_InputField _input = null;
        [Tooltip("The text which holds all chat messages received by server.")]
        [SerializeField] private TMP_Text _text = null;
        [Tooltip("Dropdown list for changing chat channels.")]
        [SerializeField] private TMP_Dropdown _channelDropdown = null;
        [Tooltip("The tab panel UI.")]
        [SerializeField] private TabPanel _tabs = null;
        [Tooltip("The right click menu for assigning chat channels to a given tab.")]
        [SerializeField] private UITabChannelMenu _tabChannelMenu = null;

        /// <summary>
        /// Retain a list of chat messages that can be filtered when changing chat channels.
        /// </summary>
        private List<ChatMessage> _messages = new List<ChatMessage>(MAX_CHAT_MESSAGES);
        /// <summary>
        /// History of messages sent by the client. Used for history navigation.
        /// </summary>
        private List<string> _sentMessages = new List<string>(MAX_SENT_MESSAGES);
        /// <summary>
        /// Current history navigation index.
        /// </summary>
        private int _curHistoryIndex;
        /// <summary>
        /// List of current used tab channels. Visually represented by the tab panel.
        /// </summary>
        private List<TabChannels> _tabChannels = new List<TabChannels>();
        /// <summary>
        /// Cached list of tab properties to draw the tabs and the corresponding tab's onClick callbacks.
        /// This assumes you are using TabPanel.
        /// </summary>
        private List<TabAction> _actions = new List<TabAction>();
        /// <summary>
        /// Cache for the name of last player whispered to.
        /// </summary>
        private string _lastWhisperName;

        /// <summary>
        /// Canvas group for adjusting alpha of the entire UI.
        /// </summary>
        private CanvasGroup _canvasGroup;
        public float Alpha
        {
            get => _canvasGroup.alpha;
            set => _canvasGroup.alpha = value;
        }

        /// <summary>
        /// True to show timestamps in chat. Use ShowTimestamps property instead.
        /// </summary>
        private bool _showTimestamps;
        /// <summary>
        /// True to show timestamps in chat. Setting this property saves the bool to PlayerPrefs and
        /// triggers a redraw of chat messages.
        /// </summary>
        public bool ShowTimestamps
        {
            get => _showTimestamps;
            set
            {
                if (_showTimestamps == value) return;

                _showTimestamps = value;
                PlayerPrefs.SetInt(SHOW_TIMESTAMPS_KEY, value ? 1 : 0);
                RedrawMessages(_tabs.CurTabIndex);
            }
        }

        /// <summary>
        /// The current channel to send outgoing messages on.
        /// </summary>
        private ChatChannel _outgoingChannel => ChatUtils.IndexToFlag(_channelDropdown.value);

        private TabChannels _curTabChannel => _tabChannels[_tabs.CurTabIndex];

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            ShowTimestamps = PlayerPrefs.GetInt(SHOW_TIMESTAMPS_KEY, 1) == 1;

            _input.characterLimit = ChatUtils.MESSAGE_MAX_CHARS;
        }

        private void Start()
        {
            InitDropdown();
            InitTabs();
            _text.text = "";
        }

        private void OnEnable()
        {
            // press enter to submit and send chat message
            _input.onSubmit.AddListener(OnInputSubmit);
            // listen for input to detect if a chat channel hint was typed
            _input.onValueChanged.AddListener(OnInputValueChanged);
            // listen for channel changes just to catch whisper errors
            _channelDropdown.onValueChanged.AddListener(OnChannelChanged);

            // if not unregistering in OnDisable, make sure to move this line to Awake/Start so there are no leaks
            InstanceFinder.ClientManager.RegisterBroadcast<ChatBroadcast>(ClientHandleChatMessage);

            // set chat as first selected game object so players can immediately press enter and chat
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_input.gameObject);
        }

        private void OnDisable()
        {
            try
            {
                _input.onSubmit.RemoveListener(OnInputSubmit);
                _input.onValueChanged.RemoveListener(OnInputValueChanged);

                _channelDropdown.onValueChanged.RemoveListener(OnChannelChanged);

                // you may not wish to unregister here to keep receiving broadcasts when disabled
                // if so, make sure move the RegisterBroadcast line in OnEnable to Awake/Start so there are no leaks
                InstanceFinder.ClientManager.UnregisterBroadcast<ChatBroadcast>(ClientHandleChatMessage);
            }
            catch { }
        }

        private void OnChannelChanged(int index)
        {
            if (!IsValidWhisper()) return;
        }

        /// <summary>
        /// Sets the tab channel array to designated defaults from _defaultTabChannels
        /// or, if it doesn't exist, an internal default (one tab, all channels).
        /// </summary>
        private void SetDefaultTabChannels()
        {
            if (_defaultTabChannels != null)
                _tabChannels = _defaultTabChannels.TabChannels;
            else
                _tabChannels = new List<TabChannels>() 
                { 
                    new TabChannels() 
                    { 
                        Channels = (ChatChannel)~0, 
                        TabName = "All" 
                    } 
                };
        }

        /// <summary>
        /// Load in saved tabs from PlayerPrefs. You may wish to save and send these on the server as well
        /// so it persists on different computers for this player.
        /// </summary>
        private void InitTabs()
        {
            _tabChannels = new List<TabChannels>();
            string tabList = PlayerPrefs.GetString(TAB_KEY, null);

            // default or load
            if (string.IsNullOrEmpty(tabList))
                SetDefaultTabChannels();
            else
                _tabChannels = JsonConvert.DeserializeObject<List<TabChannels>>(tabList);

            // this shouldnt happen
            if (_tabChannels == null || _tabChannels.Count == 0)
            {
                Debug.LogError("No tab channels. Setting defaults");
                SetDefaultTabChannels();
            }

            SaveTabs();

            PopulateTabActionsAndRecreateTabs();
        }

        /// <summary>
        /// Creates a TabAction for the tab at index. The callback simply filters messages in the given channel.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private TabAction GenerateTabAction(int index)
        {
            return new TabAction
            {
                TabName = _tabChannels[index].TabName,
                OnClick = () => RedrawMessages(index)
            };
        }

        /// <summary>
        /// Redraws the messages of the tab channel at index.
        /// </summary>
        /// <param name="index"></param>Index of the tab channel to redraw chat from.
        private void RedrawMessages(int index)
        {
            _text.text = "";

            // is there a more efficient way?
            // get all messages in any of these channels
            _messages.FindAll(x => _tabChannels[index].Channels.HasFlag(x.Broadcast.Channel))
                     .ForEach(x => _text.text += x.GetMessageString(ShowTimestamps));
        }

        /// <summary>
        /// Recreates each TabAction, the tabs themselves, and the right-click menu callback.
        /// Note that this removes all listeners on tab buttons.
        /// </summary>
        /// <param name="maintainIndex"></param>
        private void PopulateTabActionsAndRecreateTabs()
        {
            _actions.Clear();
            for (int i = 0; i < _tabChannels.Count; i++)
            {
                int index = i;
                _actions.Add(GenerateTabAction(index));
            }

            // adjust the amount of tabs we need
            _tabs.NumTabs = _actions.Count;

            // create the tabs before setting them up
            StartCoroutine(_tabs.RecreateTabs(() =>
            {
                // setup right click menu for tab buttons
                for (int i = 0; i < _tabs.TabButtons.Length; i++)
                {
                    int index = i;
                    var tab = _tabs.TabButtons[index];

                    // setup right click listener
                    tab.OnRightClick.AddListener((evt) => OpenEditTabMenu(evt.position, index));

                    // set tab text
                    tab.Text.text = _actions[index].TabName;

                    // setup button listener
                    tab.Button.onClick.AddListener(() => _actions[index].OnClick.Invoke());
                }

                // set up add button
                if (_tabs.AddTab != null)
                    _tabs.AddTab.Button.onClick.AddListener(() => OpenAddTabMenu());

                // reset to first tab
                _tabs.CurTabIndex = 0;
                _actions[0].OnClick?.Invoke();

            }));
        }

        /// <summary>
        /// Opens the right-click menu at position for the tab at the given index.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="index"></param>
        private void OpenEditTabMenu(Vector2 position, int index)
        {
            // populate remove action if valid, so they cant delete the only tab
            System.Action removeAction = _tabs.TabButtons.Length == 1 
                ? null 
                : () =>
                {
                    // cant delete the only tab
                    if (_tabs.TabButtons.Length == 1) return;

                    _tabChannels.RemoveAt(index);
                    _tabChannelMenu.Close();

                    PopulateTabActionsAndRecreateTabs();
                    SaveTabs();
                };

            // open the right-click menu for editing
            _tabChannelMenu.Open(
                position,
                _tabChannels[index],
                saveAction: () =>
                {
                    _tabChannels[index] = _tabChannelMenu.TabChannels;
                    _tabChannelMenu.Close();

                    PopulateTabActionsAndRecreateTabs();

                    // call the action to repopulate the chat with new channel filter
                    _actions[index].OnClick?.Invoke();

                    SaveTabs();
                },
                removeAction
            );
        }

        /// <summary>
        /// Opens the right-click menu with the intent to add a new tab.
        /// </summary>
        private void OpenAddTabMenu()
        {
            _tabChannelMenu.Open(
                _tabs.AddTab.transform.position,
                new TabChannels()
                {
                    TabName = "Add Tab",
                    Channels = 0,
                },
                saveAction: () =>
                {
                    _tabChannels.Add(_tabChannelMenu.TabChannels);
                    _tabChannelMenu.Close();

                    PopulateTabActionsAndRecreateTabs();
                    SaveTabs();

                    // select the newly created tab by default
                    _tabs.SelectLastTab();
                }
            );
        }

        /// <summary>
        /// Saves tabs to PlayerPrefs. You may also save them to a file or to a server 
        /// so it remains persistent across different computers.
        /// </summary>
        private void SaveTabs()
        {
            PlayerPrefs.SetString(TAB_KEY, JsonConvert.SerializeObject(_tabChannels));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Initialize the channel dropdown with names, hints, and icons.
        /// </summary>
        private void InitDropdown()
        {
            _channelDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (ChatChannel channel in _tabChannelMenu.EnumValues)
            {
                string text = channel.ToString();
                Sprite sprite = null;

                if (_chatChannelData!= null)
                {
                    var data = _chatChannelData.GetChannelData(channel);
                    if (data != null)
                    {
                        // do not add non-chattable channels, such as System
                        if (!data.Chattable) continue;

                        text += $" ({data.ChannelHint})";
                        sprite = data.Icon;
                    }
                }

                options.Add(new TMP_Dropdown.OptionData(text, sprite));
            }
            _channelDropdown.AddOptions(options);

            // set default channel to local
            _channelDropdown.value = 0;
        }


#region Channel Hint Input Detection

        private void HandleWhisper()
        {
            // [0] = /w
            // [1] = name of player to whisper
            // [2] = rest of msg
            // must normalize just in case they type something like "/w    name"
            string[] splits = ChatUtils.NormalizeWhiteSpace(_input.text).Split(' ');

            if (splits != null && splits.Length > 2)
            {
                // change to whisper channel
                // SetValueWithoutNotify to skip the callback because _lastWhisperName is not set yet
                _channelDropdown.SetValueWithoutNotify(ChatUtils.FlagToIndex(ChatChannel.Whisper));

                // set the "whisper" tag to the player's name
                _channelDropdown.captionText.text = _lastWhisperName = splits[1];

                // clear input
                _input.text = "";
            }
        }

        /// <summary>
        /// Detects channel hints and switches to the corresponding one.
        /// </summary>
        private void DetectChannelHints()
        {
            string text = _input.text;

            if (text[0] == '/')
            {
                if (text.Length > 1)
                {
                    // get the hint substring, should only be two chars with this spec
                    string hintInput = text.Substring(0, 2);

                    // trying to whisper?
                    if (hintInput == "/w")
                    {
                        HandleWhisper();
                    }
                    else
                    {
                        var data = _chatChannelData.GetChannelDataWithHint(hintInput);
                        if (data != null)
                        {
                            // set the channel and clear input
                            _channelDropdown.value = ChatUtils.FlagToIndex(data.Channel);
                            _input.text = "";
                        }
                    }
                }
            }
        }

        private void OnInputValueChanged(string text)
        {
            if (text.Length > 0)
            {
                DetectChannelHints();
            }
        }

#endregion


#region Sent Chat History Navigation

        private void Update()
        {
            // legacy input for navigating sent chat history
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow))
                NavigateHistory(true);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                NavigateHistory(false);
#endif

            // new input system untested example
            // However, you might want a more robust way of gathering this input
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    NavigateHistory(true);
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    NavigateHistory(false);
            }
#endif
        }

        /// <summary>
        /// Allows use of arrow keys to navigate chat messages sent this session.
        /// </summary>
        /// <param name="up"></param>True if navigating "up" in history, i.e. older messages.
        private void NavigateHistory(bool up)
        {
            if (_sentMessages.Count == 0) return;

            if (up)
            {
                if (_curHistoryIndex - 1 >= 0)
                    _curHistoryIndex--;
            }
            else
            {
                _curHistoryIndex++;
                if (_curHistoryIndex >= _sentMessages.Count)
                {
                    _curHistoryIndex = _sentMessages.Count;
                    ClearInput();
                    _input.ActivateInputField();
                    return;
                }
            }

            _input.text = _sentMessages[_curHistoryIndex];

            // activate input field and set caret last
            _input.ActivateInputField();
            _input.caretPosition = _input.text.Length;
        }

#endregion


        private bool IsValidWhisper()
        {
            if (_outgoingChannel == ChatChannel.Whisper && string.IsNullOrWhiteSpace(_lastWhisperName))
            {
                _channelDropdown.value = 0;                
                ClearInput();
                Debug.LogWarning("Invalid whisper");
                ClientHandleChatMessage(new ChatBroadcast
                {
                    Channel = ChatChannel.System,
                    Text = "You must indicate a player to whisper first by typing '/w name'."
                });

                return false;
            }

            return true;
        }

        void ClearInput()
        {
            _input.text = "";
            _input.caretPosition = 0;
        }

        private void OnInputSubmit(string text)
        {
            // Optional: place offline client commands here before everything else

            // cant chat without connection/auth
            if (InstanceFinder.ClientManager == null 
                || !InstanceFinder.ClientManager.Started
                || InstanceFinder.ClientManager.Connection == null
                || !InstanceFinder.ClientManager.Connection.Authenticated)
            {
                Debug.LogWarning("Must be connected to chat");
                return;
            }

            // null chat - can be triggered when input is selected by event system and pressing submit button to focus the chat box
            if (string.IsNullOrWhiteSpace(text))
            {
                ClearInput();
                //Debug.LogWarning("Tried to send null/whitespace chat");
                return;
            }

            // cache message to for up/down navigation
            _sentMessages.AddOrReplaceLast(text);
            _curHistoryIndex = _sentMessages.Count;

            // trying to do a client command?
            // no need to send it to server.
            // and doing it before channel check allows it be done in any channel
            if (ChatCommands.ClientParseCommand(text, 
                                                msg => ClientHandleChatMessage(new ChatBroadcast(msg, ChatChannel.System))))
            {
                ClearInput();
                return;
            }

            // no channel selected
            if (_outgoingChannel == 0)
            {
                Debug.LogWarning("Invalid channel");
                return;
            }

            // sanitize whisper if whispering
            // and check if valid before pinging playfab
            // allows whisper mutes to be global so they cant whisper anyone
            if (!IsValidWhisper()) return;

            // send the chat text
            BroadcastChat(text);

            // clear input 
            ClearInput();
        }

        /// <summary>
        /// Broadcast the chat and cache it in history.
        /// </summary>
        /// <param name="text"></param>
        private void BroadcastChat(string text)
        {
            // trim whitespace
            text = text.Trim();

            // trim string length if needed
            if (text.Length > ChatUtils.MESSAGE_MAX_CHARS)
                text = text.Substring(0, ChatUtils.MESSAGE_MAX_CHARS);

            // is the player trying to whisper someone?
            if (_outgoingChannel == ChatChannel.Whisper)
            {
                // check if player exists
#if USE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
                PlayFab.PlayFabClientAPI.GetAccountInfo(
                    new PlayFab.ClientModels.GetAccountInfoRequest
                    {
                        Username = _lastWhisperName
                    },
                    result =>
                    {
#endif

                        // log whisper in chatbox
                        ClientHandleChatMessage(new ChatBroadcast
                        {
                            Channel = ChatChannel.Whisper,
                            Text = $"To {_lastWhisperName}: {text}"
                        });

                        // you may check if the other player muted this player before sending
                        InstanceFinder.ClientManager.Broadcast(new WhisperBroadcast
                        {
                            PlayerName = _lastWhisperName,
                            Text = text,
                        });

#if USE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
                    },
                    err =>
                    {
                        ClientHandleChatMessage(new ChatBroadcast
                        {
                            Channel = ChatChannel.System,
                            Text = err.ErrorMessage
                        });
                    }
                );
#endif
            }
            else
            {
                InstanceFinder.ClientManager.Broadcast(new ChatBroadcast
                {
                    Channel = _outgoingChannel,
                    Text = text,
                });
            }
        }

        /// <summary>
        /// Client-side chat message handler.
        /// </summary>
        /// <param name="msg"></param>
        private void ClientHandleChatMessage(ChatBroadcast msg)
        {
            ChatMessage message = new ChatMessage(msg, _chatChannelData);
            _messages.AddOrReplaceLast(message);

            // if this user has muted the sender, dont add it to text box
            string username = msg.Text.Split(':')[0];
            CheckBlock(username, (isBlocked) =>
            {
                if (isBlocked) return;

                // add message to chat box if the current tab is displaying the channel
                if (_curTabChannel.Channels.HasFlag(msg.Channel))
                    _text.text += message.GetMessageString(ShowTimestamps);

            });

        }

        /// <summary>
        /// Checks if the user has been blocked by the local client using PlayFab data. 
        /// Invokes true if blocked.
        /// </summary>
        /// <param name="user"></param>Username to check.
        /// <param name="onBlocked"></param>Callback invoked indicating if user is blocked.
        private void CheckBlock(string user, System.Action<bool> onBlocked)
        {
            PlayFab.PlayFabClientAPI.GetUserData(
                new PlayFab.ClientModels.GetUserDataRequest
                {
                    Keys = new List<string> { ChatUtils.BLOCK_LIST_KEY }
                },
                getResult =>
                {
                    // deserialize json into list
                    List<string> blockList = new List<string>();
                    if (getResult.Data.TryGetValue(ChatUtils.BLOCK_LIST_KEY, out PlayFab.ClientModels.UserDataRecord record))
                        blockList = JsonConvert.DeserializeObject<List<string>>(record.Value);

                    onBlocked?.Invoke(blockList.Contains(user));

                },
                err =>
                {
                    Debug.LogError(err.GenerateErrorReport());
                    onBlocked?.Invoke(false);
                }
            );
        }

        private void OnValidate()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

    }

}
