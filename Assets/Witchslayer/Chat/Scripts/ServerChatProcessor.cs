using FishNet;
using FishNet.Connection;

using UnityEngine;
using UnityEngine.SceneManagement;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Chat.MuteCheckers;
using Witchslayer.Chat.ProfanityFilters;
using Witchslayer.Chat.ConnectionInfos;
using Witchslayer.Chat.Commands;

namespace Witchslayer.Chat
{

    public class ServerChatProcessor : MonoBehaviour
    {

        [Header("Settings")]
        [Tooltip("True to log all info about chat coming in and being sent out.")]
        [SerializeField] private bool _verboseLogging = true;
        [Tooltip("Don't destroy on load.")]
        [SerializeField] private bool _ddol = true;

        [Header("Components")]
        [Tooltip("Profanity filter to use.")]
        [SerializeField] private ProfanityFilterBase _profanityFilter = null;
        [Tooltip("Channel mute checker to use. " +
            "This checks if the player is muted in the entire channel, i.e. global, all whispers, etc.")]
        [SerializeField] private MuteCheckerBase _channelMuteChecker = null;
        [Tooltip("Mute checker to use for whispers. " +
            "This specifically checks if other players have muted the sending player for whispers.")]
        [SerializeField] private WhisperMuteCheckerBase _whisperMuteChecker = null;

        [Tooltip("Component used for getting info about a connection, " +
            "such as username, playfab ID, or the conn itself.")]
        [SerializeField] private ConnectionInfoBase _connectionInfoGetter = null;
        internal ConnectionInfoBase ConnectionInfo => _connectionInfoGetter;

        /// <summary>
        /// Create a server chat processor. 
        /// Note this will not add default components like the profanity filter and mute checker.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if (FindObjectOfType<ServerChatProcessor>() == null)
            {
                GameObject go = new GameObject();
                go.name = "ServerChatProcessor";
                go.AddComponent<ServerChatProcessor>();
            }
        }

        private void Awake()
        {
            if (_ddol)
                DontDestroyOnLoad(gameObject);

            if (_profanityFilter == null)
                _profanityFilter = GetComponent<ProfanityFilterBase>();
            if (_channelMuteChecker == null)
                _channelMuteChecker = GetComponent<MuteCheckerBase>();
        }

        private void OnEnable()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<ChatBroadcast>(HandleChatBroadcast);
            InstanceFinder.ServerManager.RegisterBroadcast<WhisperBroadcast>(HandleWhisperBroadcast);
        }

        private void OnDisable()
        {
            try
            {
                InstanceFinder.ServerManager.UnregisterBroadcast<ChatBroadcast>(HandleChatBroadcast);
                InstanceFinder.ServerManager.UnregisterBroadcast<WhisperBroadcast>(HandleWhisperBroadcast);
            }
            catch { }
        }

        /// <summary>
        /// Remove unwanted characters, words, and symbols from a text so it does not
        /// break other client's chat box or create spam.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string SanitizeMessage(string text)
        {
            // trim html tags using HtmlAgilityPack (you will need to download it and stick it in a Plugins folder)
            //HtmlDocument doc = new HtmlDocument();
            //doc.LoadHtml(text);
            //string result = doc.DocumentNode.InnerText;
            // or use regex to trim html tags
            // Credit: https://stackoverflow.com/a/2334958/13668816
            string result = Regex.Replace(text, @"<(.|\n)*?>", string.Empty);

            // remove HTML entities
            result = WebUtility.HtmlDecode(result);

            // remove troublesome symbols
            result = Regex.Replace(result, @"\n|\t|\v|\0|\r|\u200B", "");

            // remove extra whitespace between words
            result = ChatUtils.NormalizeWhiteSpace(result);

            return result;
        }

        private void HandleWhisperBroadcast(NetworkConnection conn, WhisperBroadcast msg)
        {
            if (!PreprocessMessage(conn, msg.Text)) return;

            if (_verboseLogging)
                Debug.Log($"Server got chat from on channel {ChatChannel.Whisper} to {msg.PlayerName}:\n{msg.Text}");

            // STEP ONE: ==========================================================================================
            // get the connection of the person they are trying to whisper
            RunConnectionGetter(msg.PlayerName, otherConn =>
            {
                // couldnt find conn with given username
                if (otherConn == null)
                {
                    conn.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.System,
                        Text = $"{msg.PlayerName} is not online."
                    });
                    return;
                }

                // STEP TWO: ==========================================================================================
                // run whisper mute checker to see if the receiver has muted the sender
                RunWhisperMuteChecker(conn, otherConn, (isMutedWhisper) =>
                {
                    // receiver has muted the sender
                    if (isMutedWhisper)
                    {
                        conn.Broadcast(new ChatBroadcast
                        {
                            Text = "This user has muted you.",
                            Channel = ChatChannel.System,
                        });
                        return;
                    }

                    // STEP THREE: ==========================================================================================
                    // run channel mute checker to see if the sender has been muted in the whisper channel
                    RunChannelMuteChecker(conn, ChatChannel.Whisper, (isMutedChannel) =>
                    {
                        // sender has been muted on this channel
                        if (isMutedChannel)
                        {
                            conn.Broadcast(ChatUtils.GenerateMuteMsg(ChatChannel.Whisper));
                            return;
                        }

                        // STEP FOUR: ==========================================================================================
                        // run profanity filter to censor text
                        RunProfanityFilter(msg.Text, (filteredText) =>
                        {

                            // STEP FIVE: ==========================================================================================
                            // get the player's username to add it to the message
                            RunUsernameGetter(conn, username =>
                            {
                                // create msg to broadcast
                                ChatBroadcast chatMsg = new ChatBroadcast
                                {
                                    Text = $"From {username}: {filteredText}",
                                    Channel = ChatChannel.Whisper,
                                };

                                // send it to the receiver
                                otherConn.Broadcast(chatMsg);

                            });

                        });

                    });

                });

            });

        }

        /// <summary>
        /// Invokes if the player has been muted in the designated channel.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="channel"></param>
        /// <param name="cb"></param>
        private void RunChannelMuteChecker(NetworkConnection conn, ChatChannel channel, Action<bool> cb)
        {
            if (_channelMuteChecker != null)
                _channelMuteChecker.IsMuted(conn, channel, cb);
            else
                cb.Invoke(false);
        }

        private void RunConnectionGetter(string username, Action<NetworkConnection> cb)
        {
            if (_connectionInfoGetter != null)
                _connectionInfoGetter.GetConnection(username, cb);
            else
                cb.Invoke(null);
        }

        private void RunUsernameGetter(NetworkConnection conn, Action<string> cbUsername)
        {
            if (_connectionInfoGetter != null)
                _connectionInfoGetter.GetUsername(conn, cbUsername);
            else
                cbUsername.Invoke(null);
        }

        /// <summary>
        /// Invokes the filtered text if there is a profanity filter,
        /// otherwise returns the passed text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="cb"></param>
        private void RunProfanityFilter(string text, Action<string> cb)
        {
            if (_profanityFilter != null)
                _profanityFilter.Filter(text, cb);
            else
                cb.Invoke(text);
        }

        /// <summary>
        /// Invokes if the player has been muted by the receiving player.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="otherConn"></param>
        /// <param name="cb"></param>
        private void RunWhisperMuteChecker(NetworkConnection conn, NetworkConnection otherConn, Action<bool> cb)
        {
            if (_whisperMuteChecker != null)
                _whisperMuteChecker.IsMuted(conn, otherConn, cb);
            else
                cb.Invoke(false);
        }

        private bool PreprocessMessage(NetworkConnection conn, string text)
        {
            // sent empty/null chat
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning($"(IP={conn.GetAddress()}) sending null/ws chat");
                conn.Disconnect(true);
                return false;
            }

            text = text.Trim();

            // message was too long
            // only cheaters would trip this, you may disconnect them
            if (text.Length > ChatUtils.MESSAGE_MAX_CHARS)
                text = text.Substring(0, ChatUtils.MESSAGE_MAX_CHARS);

            // sanitize text
            text = SanitizeMessage(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning($"(IP={conn.GetAddress()}) sending invalid HTML chat: " + text);
                conn.Disconnect(true);
                return false;
            }

            // check commands
            if (ChatCommands.ServerParseCommand(conn, text)) return false;

            return true;
        }

        /// <summary>
        /// Server-side chat broadcast handler.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void HandleChatBroadcast(NetworkConnection conn, ChatBroadcast msg)
        {
            if (!PreprocessMessage(conn, msg.Text)) return;

            // invalid channel
            if (msg.Channel <= 0
                // multiple channels flagged
                || (msg.Channel & (msg.Channel - 1)) != 0)
            {
                Debug.LogWarning($"(IP={conn.GetAddress()}) sending invalid/multiple channels: {msg.Channel}");
                conn.Disconnect(true);
                return;
            }

            // log raw text
            if (_verboseLogging)
                Debug.Log($"Server got chat from on channel {msg.Channel}:\n{msg.Text}");

            // check if this user is muted before sending message to profanity filter and then back to clients
            RunChannelMuteChecker(conn, msg.Channel, (isMuted) =>
            {
                // if muted, send them a mute msg
                if (isMuted)
                {
                    conn.Broadcast(ChatUtils.GenerateMuteMsg(msg.Channel));
                    return;
                }

                // filter profanity
                RunProfanityFilter(msg.Text, filteredText =>
                {
                    // get username
                    RunUsernameGetter(conn, username =>
                    {
                        if (string.IsNullOrEmpty(username))
                        {
                            Debug.LogError("Couldn't get username from connection " + conn.GetAddress());
                            return;
                        }

                        // create msg to broadcast
                        SendToChannel(conn, new ChatBroadcast
                        {
                            Text = $"{username}: {filteredText}",
                            Channel = msg.Channel,
                        });

                    });

                });

            });
        }

        /// <summary>
        /// Sends the specified msg to its channel based on the conn sender.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        private void SendToChannel(NetworkConnection conn, ChatBroadcast msg)
        {
            // send to appropriate channel
            switch (msg.Channel)
            {
                // in all scenes the sender is in
                case ChatChannel.Local:
                    foreach (Scene scene in conn.Scenes)
                    {
                        // get the other conns in the scenes
                        if (InstanceFinder.SceneManager.SceneConnections.TryGetValue(scene, out HashSet<NetworkConnection> otherConns))
                        {
                            // send to all other conns in the same scene
                            foreach (NetworkConnection otherConn in otherConns)
                            {
                                otherConn.Broadcast(msg);
                            }
                        }
                    }
                    break;

                // all active players
                case ChatChannel.Global:
                    InstanceFinder.ServerManager.Broadcast(msg);
                    break;

                // fill in your logic here
                //case ChatChannels.Party:
                //    break;
                //case ChatChannels.Guild:
                //    break;
                //case ChatChannels.Friends:
                //    break;
                //case ChatChannels.System:
                //    break;
                default:
                    Debug.LogWarning("Handler not implemented for: " + msg.Channel.ToString());
                    break;
            }
        }

        private void OnValidate()
        {
            if (_profanityFilter == null)
                _profanityFilter = GetComponent<ProfanityFilterBase>();

            if (_channelMuteChecker == null)
                _channelMuteChecker = GetComponent<MuteCheckerBase>();

            if (_whisperMuteChecker == null)
                _whisperMuteChecker = GetComponent<WhisperMuteCheckerBase>();
        }

    }

}
