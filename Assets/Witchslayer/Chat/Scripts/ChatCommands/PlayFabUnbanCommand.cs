#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;

using PlayFab;

using System;
using System.Collections.Generic;

using UnityEngine;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.Commands
{

    public class PlayFabUnbanCommand : ChatCommandBase
    {
        public override string[] CommandKeys => new string[]
        {
            "unban",
            "removeban"
        };


        public override void Parse(string[] commands, Action<string> onError = null, NetworkConnection conn = null)
        {
            if (commands.Length < 2)
            {
                // duration is optional
                onError?.Invoke("Invalid syntax. Usage: /unban username");
                return;
            }

#if !DISABLE_PLAYFABCLIENT_API
            if (conn == null)
            {
                PlayFabUtils.ReplaceUsernameWithPlayFabId(commands, true, onError, (resultCommands) =>
                {
                    InstanceFinder.ClientManager.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.Local, // doesnt matter in this context
                        Text = ChatCommands.COMMAND_CHAR + string.Join(' ', new ArraySegment<string>(commands, 0, 2)),
                    });
                });
            }
#endif


#if ENABLE_PLAYFABSERVER_API
            if (conn != null)
            {
                void HandleError(PlayFabError error)
                {
                    var report = error.GenerateErrorReport();
                    onError?.Invoke(report);
                    conn.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.System,
                        Text = report
                    });
                }

                string adminPlayfabId = PlayFabUtils.GetPlayFabId(conn);
                if (string.IsNullOrEmpty(adminPlayfabId))
                {
                    Debug.LogError("Invalid playfab ID for conn, IP: " + conn.GetAddress());
                    return;
                }

                string otherPlayfabId = commands[1];

                // STEP ONE: Check if they have permission to unban =============================================================================
                // you may disconnect the player on failure because only cheaters would get here since this is already checked on the client
                // You can add custom admin levels and such. This simply gets admin privileges as a bool.
                PlayFabUtils.GetAdmin(adminPlayfabId, (isAdmin) =>
                {
                    if (!isAdmin)
                    {
                        onError?.Invoke("Not enough permission to unban.");
                        conn.Broadcast(new ChatBroadcast
                        {
                            Channel = ChatChannel.System,
                            Text = "You do not have permission to unban."
                        });
                        return;
                    }

                    // STEP TWO: Check the target's ban status =============================================================================
                    PlayFabServerAPI.GetUserReadOnlyData(
                        new PlayFab.ServerModels.GetUserDataRequest
                        {
                            PlayFabId = otherPlayfabId,
                            Keys = new List<string> { ChatUtils.BAN_KEY }
                        },
                        banResult =>
                        {
                            // no ban record
                            if (!banResult.Data.TryGetValue(ChatUtils.BAN_KEY, out PlayFab.ServerModels.UserDataRecord banRecord)
                                // invalid ban DateTime string
                                || !System.DateTime.TryParse(banRecord.Value, out DateTime banEndDate)
                                // ban expired already
                                || banEndDate < DateTime.UtcNow)
                            {
                                string err = $"{otherPlayfabId} is not banned.";
                                conn.Broadcast(new ChatBroadcast
                                {
                                    Channel = ChatChannel.System,
                                    Text = err
                                });
                                onError?.Invoke(err);
                                return;
                            }

                            // STEP THREE: Save the ban back to playfab user readonly data =============================================================================
                            PlayFabServerAPI.UpdateUserReadOnlyData(
                                new PlayFab.ServerModels.UpdateUserDataRequest
                                {
                                    PlayFabId = otherPlayfabId,
                                    Data = new Dictionary<string, string>
                                    {
                                        { ChatUtils.BAN_KEY, "" }
                                    }
                                },
                                updateResult =>
                                {
                                    conn.Broadcast(new ChatBroadcast
                                    {
                                        Channel = ChatChannel.System,
                                        Text = $"User has been unbanned."
                                    });
                                },
                                HandleError
                            );

                        },
                        HandleError
                    );
                });
            }
#endif


        }


    }

}

#endif
