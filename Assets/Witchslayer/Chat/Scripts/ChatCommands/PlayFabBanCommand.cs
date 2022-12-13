#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;

using PlayFab;

using System;
using System.Collections.Generic;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.Commands
{

    public class PlayFabBanCommand : ChatCommandBase
    {

        public override string[] CommandKeys => new string[]
        {
            "ban"
        };

        public override void Parse(string[] commands, Action<string> onResult = null, NetworkConnection conn = null)
        {
            if (commands.Length < 2)
            {
                // duration is optional
                onResult?.Invoke("Invalid syntax. Usage: /ban username");
                return;
            }

#if !DISABLE_PLAYFABCLIENT_API
            // client side
            if (conn == null)
            {
                PlayFabUtils.ReplaceUsernameWithPlayFabId(commands, true, onResult, (resultCommands) =>
                {
                    InstanceFinder.ClientManager.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.Local, // doesnt matter in this context
                        Text = ChatCommands.COMMAND_CHAR + string.Join(' ', new ArraySegment<string>(resultCommands, 0, 2)),
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
                    onResult?.Invoke(report);
                    conn.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.System,
                        Text = report
                    });
                }

                string adminPlayfabId = PlayFabUtils.GetPlayFabId(conn);
                if (string.IsNullOrEmpty(adminPlayfabId))
                {
                    onResult?.Invoke("Invalid playfab ID for conn, IP: " + conn.GetAddress());
                    return;
                }

                string otherPlayfabId = commands[1];

                // prevent banning self
                if (adminPlayfabId == otherPlayfabId)
                {
                    onResult?.Invoke("You cannot ban yourself.");
                    return;
                }

                // STEP ONE: Check if they have permission to ban =============================================================================
                // you may disconnect the player on failure because only cheaters would get here since this is already checked on the client
                PlayFabUtils.GetAdmin(adminPlayfabId, (isAdmin) =>
                {
                    if (!isAdmin)
                    {
                        onResult?.Invoke("Not enough permission to ban.");
                        conn.Broadcast(new ChatBroadcast
                        {
                            Channel = ChatChannel.System,
                            Text = "You do not have permission to ban."
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
                            DateTime banEndDate;

                            if (banResult.Data.TryGetValue(ChatUtils.BAN_KEY, out PlayFab.ServerModels.UserDataRecord banRecord))
                            {
                                if (System.DateTime.TryParse(banRecord.Value, out banEndDate))
                                {
                                    // banned
                                    if (banEndDate > DateTime.UtcNow)
                                    {
                                        string err = $"{otherPlayfabId} is already banned until {banEndDate.ToShortDateString()}.";
                                        conn.Broadcast(new ChatBroadcast
                                        {
                                            Channel = ChatChannel.System,
                                            Text = err
                                        });
                                        onResult?.Invoke(err);
                                        return;
                                    }
                                }
                            }

                            // ban them for 10 years
                            // you may adjust duration as you see fit with a 3rd command parameter
                            // You may also add another parameter for a ban reason, then make sure to save it as a serialized JSON string.
                            banEndDate = DateTime.UtcNow.AddYears(10);

                            // STEP THREE: Save the ban back to playfab user readonly data =============================================================================
                            PlayFabServerAPI.UpdateUserReadOnlyData(
                                new PlayFab.ServerModels.UpdateUserDataRequest
                                {
                                    PlayFabId = otherPlayfabId,
                                    Data = new Dictionary<string, string>
                                    {
                                        { ChatUtils.BAN_KEY, banEndDate.ToString() }
                                    }
                                },
                                updateResult =>
                                {
                                    conn.Broadcast(new ChatBroadcast
                                    {
                                        Channel = ChatChannel.System,
                                        Text = $"User has been banned."
                                    });

                                    // if player is online, inform them they have been banned and disconnect them
                                    foreach (NetworkConnection item in InstanceFinder.ServerManager.Clients.Values)
                                    {
                                        if (PlayFabUtils.GetPlayFabId(item) == otherPlayfabId)
                                        {
                                            item.Broadcast(new ChatBroadcast(
                                                $"You have been banned.",
                                                ChatChannel.System
                                            ));

                                            // not immediate so they can get the broadcast
                                            item.Disconnect(false);

                                            break;
                                        }
                                    }
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
