using FishNet.Broadcast;

namespace Witchslayer.Chat.Broadcasts
{

    /// <summary>
    /// Broadcast for chat text and channel.
    /// </summary>
    public struct ChatBroadcast : IBroadcast
    {

        /// <summary>
        /// Text to send.
        /// </summary>
        public string Text;

        /// <summary>
        /// Channels in which to send the text.
        /// </summary>
        public ChatChannel Channel;

        public ChatBroadcast(string text, ChatChannel channel)
        {
            Text = text;
            Channel = channel;
        }

        public override string ToString()
        {
            return $"[{Channel}] {Text}";
        }

    }

}
