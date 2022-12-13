using FishNet.Broadcast;

namespace Witchslayer.Chat.Broadcasts
{

    /// <summary>
    /// Unique broadcast specifically for chat whispers to other players.
    /// </summary>
    public struct WhisperBroadcast : IBroadcast
    {

        /// <summary>
        /// Name of player to whisper to.
        /// </summary>
        public string PlayerName;

        /// <summary>
        /// Text to send.
        /// </summary>
        public string Text;

        public override string ToString()
        {
            return $"[To {PlayerName}] {Text}";
        }

    }

}
