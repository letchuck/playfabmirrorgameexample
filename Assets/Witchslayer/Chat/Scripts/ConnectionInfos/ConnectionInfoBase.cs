using FishNet.Connection;
using System;

namespace Witchslayer.Chat.ConnectionInfos
{

    /// <summary>
    /// Base getter class using NetworkConnections if you wish to 
    /// display usernames in chat, which I assume most people would.
    /// </summary>
    public abstract class ConnectionInfoBase : ServerChatProcessorComponent
    {

        /// <summary>
        /// Invokes the username obtained from the connection.
        /// </summary>
        /// <param name="conn"></param>Connection to find the username of.
        /// <param name="cbUsername"></param>Callback to invoke with the found username.
        public abstract void GetUsername(NetworkConnection conn, Action<string> cbUsername);

        /// <summary>
        /// Invokes the network connection with the unique corresponding username.
        /// </summary>
        /// <param name="username"></param>Username to search for.
        /// <param name="cbConn"></param>Callback to invoke with the found connection.
        public abstract void GetConnection(string username, Action<NetworkConnection> cbConn);

    }

}
