#if USE_PLAYFAB
using PlayFab;
#endif

using Newtonsoft.Json;

using System.Collections.Generic;

using UnityEngine;
using FishNet.Connection;
using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;
using FishNet;
using System.Reflection;
using System;

namespace Witchslayer.Chat.Commands
{

    /// <summary>
    /// A list of chat commands that can be used on the client.
    /// Simple stuff the client could have control over, like the volume, closing the game, etc.
    /// All command must begin with '/'.
    /// </summary>
    public class ChatCommands 
    {

        public const char COMMAND_CHAR = '/';
        private const char SEP_CHAR = ' ';

        private static Dictionary<string, ChatCommandBase> _chatCommands;

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            _chatCommands = new Dictionary<string, ChatCommandBase>();
            foreach (var item in Assembly.GetAssembly(typeof(ChatCommandBase)).GetTypes())
            {
                if (!item.IsSubclassOf(typeof(ChatCommandBase))) continue;

                ChatCommandBase instance = Activator.CreateInstance(item) as ChatCommandBase;
                foreach (string key in instance.CommandKeys)
                    _chatCommands.TryAdd(key.ToLower(), instance);
            }
        }

        /// <summary>
        /// Returns true if a command was found and executed.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ClientParseCommand(string text, Action<string> cbMessage)
        {
            text = text.Trim();

            if (!IsCommand(text)) return false;

            // substring removes the command char, then splits it into command parts
            return FindCommandClient(text.Substring(1).Split(SEP_CHAR), cbMessage);
        }

        private static bool IsCommand(string text)
        {
            // invalid string
            if (string.IsNullOrWhiteSpace(text)) return false;

            // not a command
            if (text[0] != COMMAND_CHAR) return false;

            return true;
        }

        private static bool FindCommandClient(string[] parts, Action<string> cbMessage)
        {
            if (parts == null || parts.Length == 0) return false;

            if (_chatCommands.TryGetValue(parts[0].ToLower(), out ChatCommandBase command))
            {
                command.Parse(parts, cbMessage);
                return true;
            }

            return false;
        }


        #region Server

        public static bool ServerParseCommand(NetworkConnection conn, string text)
        {
            text = text.Trim();

            if (!IsCommand(text)) return false;

            // substring removes the command char, then splits it into command parts
            FindCommandServer(conn, text.Substring(1).Split(SEP_CHAR));
            return true;
        }

        private static bool FindCommandServer(NetworkConnection conn, string[] parts)
        {
            if (conn == null || parts == null || parts.Length == 0) return false;

            if (_chatCommands.TryGetValue(parts[0].ToLower(), out ChatCommandBase command))
            {
                command.Parse(parts, (err) => Debug.LogError(err), conn);
                return true;
            }

            return false;
        }

        #endregion

    }

}
