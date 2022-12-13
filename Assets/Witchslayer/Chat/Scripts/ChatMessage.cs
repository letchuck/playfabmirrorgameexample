using UnityEngine;
using Witchslayer.Chat.Broadcasts;
using Witchslayer.Chat.ScriptableObjects;

namespace Witchslayer.Chat
{

    public struct ChatMessage
    {

        /// <summary>
        /// Short time string of when the chat was received on local time.
        /// </summary>
        public string Timestamp;
        /// <summary>
        /// The chat broadcast.
        /// </summary>
        public ChatBroadcast Broadcast;

        [System.NonSerialized]
        private ChatChannelData _data;

        public ChatMessage(ChatBroadcast msg, ChatChannelData channelData)
        {
            _data = channelData;
            Timestamp = $"[{System.DateTime.Now.ToShortTimeString()}]";
            Broadcast = msg;
        }

        public string GetMessageString(bool showTimestamp)
        {
            string s = "";

            if (showTimestamp)
                s += Timestamp;

            s += GetChannelPrefix();

            s += $"{Broadcast.Text}\n";

            s = WrapChatColorHTML(s);

            return s;
        }

        private string WrapChatColorHTML(string text)
        {
            if (_data != null)
            {
                var data = _data.GetChannelData(Broadcast.Channel);
                if (data != null)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(data.Color);
                    text = $"<color=#{hex}>{text}</color>";
                }
            }

            return text;
        }

        private string GetChannelPrefix()
        {
            string s;

            switch (Broadcast.Channel)
            {
                case ChatChannel.Whisper:
                    s = "";
                    break;

                case ChatChannel.System:
                    s = "[SYSTEM] ";
                    break;

                default:
                    s = $"[{Broadcast.Channel}] ";
                    break;
            }

            return s;
        }

    }

}
