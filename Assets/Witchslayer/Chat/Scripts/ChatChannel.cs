
namespace Witchslayer.Chat
{

    /// <summary>
    /// Chat channels you may want. 
    /// Make sure each value is a power of two so the flags remain valid.
    /// </summary>
    [System.Flags]
    public enum ChatChannel
    {
        /// <summary>
        /// Send messages only to other connections in the same scenes as the sender.
        /// </summary>
        Local = 1,
        /// <summary>
        /// Send messages to everyone on server.
        /// </summary>
        Global = 2,
        /// <summary>
        /// Send messages to your party.
        /// </summary>
        Party = 4,
        /// <summary>
        /// Sends messages to your guild.
        /// </summary>
        Guild = 8,
        /// <summary>
        /// Send messages to friends.
        /// </summary>
        Friends = 16,
        /// <summary>
        /// Send messages to most recent whisper.
        /// </summary>
        Whisper = 32,
        /// <summary>
        /// Messages sent by the system or server.
        /// </summary>
        System = 64,
    }

}
