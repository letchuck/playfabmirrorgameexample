#if USE_PLAYFAB && ENABLE_PLAYFABSERVER_API

using FishNet;
using FishNet.Connection;
using System;

namespace Witchslayer.Chat.ConnectionInfos
{

    /// <summary>
    /// Gets the username from the conn.CustomData assuming you're using the PlayFabAuthenticator
    /// (or cached the authenticated UserAccountInfo into conn.CustomData).
    /// </summary>
    public class PlayFabConnectionInfo : ConnectionInfoBase
    {

        public override void GetConnection(string username, Action<NetworkConnection> cbConn)
        {
            foreach (NetworkConnection conn in InstanceFinder.ServerManager.Clients.Values)
            {
                string user = "";
                if (conn.CustomData is PlayFab.ClientModels.UserAccountInfo client)
                    user = client.Username;
                else if (conn.CustomData is PlayFab.ServerModels.UserAccountInfo server)
                    user = server.Username;

                if (user == username)
                {
                    // only invoke if they have been authenticated and can receive messages
                    if (conn.Authenticated)
                    {
                        cbConn.Invoke(conn);
                        return;
                    }
                }
            }

            cbConn?.Invoke(null);
        }

        public override void GetUsername(NetworkConnection conn, Action<string> cbUsername)
        {
            if (conn!=null)
            {
                // why this is ClientModels from server API, i do not know
                if (conn.CustomData is PlayFab.ClientModels.UserAccountInfo info)
                {
                    cbUsername.Invoke(info.Username);
                    return;
                }
                else if (conn.CustomData is PlayFab.ServerModels.UserAccountInfo serverInfo)
                {
                    cbUsername.Invoke(serverInfo.Username);
                    return;
                }
            }

            cbUsername?.Invoke(null);
        }

    }

}

#endif
