using System.Collections.Generic;
using UnityEngine;
using Witchslayer.Chat.Broadcasts;

namespace Witchslayer.Chat
{

    internal class ChatUtils 
    {

        /// <summary>
        /// Max chars a chat message may contain.
        /// </summary>
        internal const int MESSAGE_MAX_CHARS = 100;
        /// <summary>
        /// Key for server channel mutes.
        /// </summary>
        internal const string MUTE_KEY = "Mute";
        /// <summary>
        /// Key for ban data.
        /// </summary>
        internal const string BAN_KEY = "Ban";
        /// <summary>
        /// Key for client mute lists.
        /// </summary>
        internal const string BLOCK_LIST_KEY = "MuteList";
        /// <summary>
        /// Key for admin permissions.
        /// </summary>
        internal const string ADMIN_KEY = "Admin";

        internal static ChatBroadcast GenerateMuteMsg(ChatChannel muteFlags)
        {
            return new ChatBroadcast
            {
                Text = $"You are currently muted in channel: {muteFlags}.",
                Channel = ChatChannel.System,
            };
        }

        internal static int FlagToIndex(ChatChannel channel) => (int)Mathf.Log((int)channel, 2);

        internal static ChatChannel IndexToFlag(int index) => (ChatChannel)Mathf.Pow(2, index);

        /// <summary>
        /// Trims any excess white space in a string.
        /// Credit: https://stackoverflow.com/a/37592018/13668816
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static string NormalizeWhiteSpace(string input)
        {
            int len = input.Length;
            int index = 0;
            int i = 0;
            var src = input.ToCharArray();
            bool skip = false;
            char ch;

            for (; i < len; i++)
            {
                ch = src[i];
                switch (ch)
                {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u200B':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        if (skip) continue;
                        src[index++] = ch;
                        skip = true;
                        continue;
                    default:
                        skip = false;
                        src[index++] = ch;
                        continue;
                }
            }

            return new string(src, 0, index);
        }

    }

}
