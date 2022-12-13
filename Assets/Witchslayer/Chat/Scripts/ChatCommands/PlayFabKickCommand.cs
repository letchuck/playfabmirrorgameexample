#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;
using System;
using UnityEngine;
using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.Commands
{

    /// <summary>
    /// PlayFab/server authoritative kick command.
    /// </summary>
    public class PlayFabKickCommand : ChatCommandBase
    {

        public override string[] CommandKeys => new string[]
        {
            "kick"
        };

        public override void Parse(string[] commands, Action<string> onError = null, NetworkConnection conn = null)
        {
            if (commands.Length < 2)
            {
                onError?.Invoke("Invalid syntax. Usage: /kick username");
                return;
            }

#if !DISABLE_PLAYFABCLIENT_API
            // client side
            if (conn == null)
            {
                PlayFabUtils.GetAdmin(null, isAdmin =>
                {
                    if (!isAdmin)
                    {
                        onError?.Invoke("You don't have permission to kick players.");
                        return;
                    }

                    // send command to server
                    InstanceFinder.ClientManager.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.Local, // doesnt matter in this context
                        Text = ChatCommands.COMMAND_CHAR + string.Join(' ', new ArraySegment<string>(commands, 0, 2)),
                    });

                });
            }
#endif

#if ENABLE_PLAYFABSERVER_API
            if (conn == null) return;

            // check if sender is admin
            PlayFabUtils.GetAdmin(PlayFabUtils.GetPlayFabId(conn), isAdmin =>
            {
                // only cheaters would trigger this
                if (!isAdmin)
                {
                    onError?.Invoke("You don't have permission to kick players.");
                    conn.Disconnect(true);
                    return;
                }

                string usernameToKick = commands[1].ToLower();

                // prevent kicking self
                if (PlayFabUtils.GetUserAccountInfo(conn)?.Username.ToLower() == usernameToKick)
                {
                    onError?.Invoke("You cannot kick yourself!");
                    return;
                }

                // find person to kick if online
                foreach (NetworkConnection item in InstanceFinder.ServerManager.Clients.Values)
                {
                    if (item.CustomData is PlayFab.ServerModels.UserAccountInfo info)
                    {
                        if (info.Username == usernameToKick)
                        {
                            Debug.Log($"{usernameToKick} has been kicked.");
                            item.Disconnect(false);
                            return;
                        }
                    }
                }

                // user to kick was not online
                onError?.Invoke($"{usernameToKick} is not online.");

            });
#endif

        }

    }

}

#endif