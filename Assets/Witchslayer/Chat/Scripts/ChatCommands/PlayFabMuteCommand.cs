#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;

using PlayFab;

using System.Collections.Generic;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.Commands
{

    // TODO redraw ui chat on mute
    public class PlayFabMuteCommand : ChatCommandBase
    {

        public override string[] CommandKeys => new string[] 
        { 
            "mute",
        };

        public override void Parse(string[] commands, System.Action<string> cbResult = null, NetworkConnection conn = null)
        {
            // validate syntax
            if (commands.Length < 3)
            {
                cbResult.Invoke("Incorrect syntax. Use: /mute username channel");
                return;
            }

            // client-side mute handler
#if !DISABLE_PLAYFABCLIENT_API
            if (conn == null)
            {
                // check if player has privileges to mute
                PlayFabUtils.GetAdmin(null, (isAdmin) =>
                {
                    if (!isAdmin)
                    {
                        cbResult?.Invoke("You do not have permission to do that.");
                        return;
                    }

                    string userToMute = commands[1].ToLower();

                    // prevent muting self
                    if (InstanceFinder.ClientManager?.Connection?.CustomData is PlayFab.ClientModels.UserAccountInfo info)
                    {
                        if (info.Username == userToMute)
                        {
                            cbResult?.Invoke("You cannot mute yourself!");
                            return;
                        }
                    }

                    // replace username with playfab ID because it cannot be obtained using server API.
                    // another option is to use the admin API on the server, but it's easy to be rate limited:
                    // https://community.playfab.com/questions/37365/is-there-a-way-to-get-playfab-id-by-display-name.html
                    PlayFabClientAPI.GetAccountInfo(
                        new PlayFab.ClientModels.GetAccountInfoRequest
                        {
                            // playfab saves usernames as lower
                            Username = userToMute,
                        },
                        result =>
                        {
                            // replace username with playFabId
                            commands[1] = result.AccountInfo.PlayFabId;

                            InstanceFinder.ClientManager.Broadcast(new ChatBroadcast
                            {
                                Channel = ChatChannel.Local, // doesnt matter in this context
                                Text = ChatCommands.COMMAND_CHAR + string.Join(' ', commands),
                            });
                        },
                        error =>
                        {
                            cbResult?.Invoke(error.GenerateErrorReport());
                        }
                    );

                });

                // exit early
                return;
            }
#endif

            // we get here from the broadcast in the code block above
#if ENABLE_PLAYFABSERVER_API
            if (conn != null)
            {
                void HandleError(PlayFabError error)
                {
                    string report = error.GenerateErrorReport();
                    cbResult?.Invoke(report);
                    conn.Broadcast(new ChatBroadcast
                    {
                        Channel = ChatChannel.System,
                        Text = report
                    });
                }

                string adminPlayfabId = PlayFabUtils.GetPlayFabId(conn);
                if (string.IsNullOrEmpty(adminPlayfabId))
                {
                    cbResult?.Invoke("Invalid playfab ID for conn, IP: " + conn.GetAddress());
                    return;
                }

                string otherPlayfabId = commands[1];
                string channelName = commands[2];
                ChatChannel channelToMute = 0;

                if (channelName == "all" || channelName == "everything")
                {
                    channelToMute = (ChatChannel)~0;
                }
                else
                {
                    if (!System.Enum.TryParse(typeof(ChatChannel), channelName, true, out object channelObj))
                    {
                        conn.Broadcast(new ChatBroadcast
                        {
                            Channel = ChatChannel.System,
                            Text = "Invalid channel name."
                        });
                        return;
                    }

                    channelToMute = (ChatChannel)channelObj;
                }

                // STEP ONE: Check if they have permission to mute =============================================================================
                // you may disconnect the player on failure because only cheaters would get here
                // since this is already checked on the client
                // You can add custom admin levels and such. This just gets admin privileges as a bool.
                PlayFabUtils.GetAdmin(
                    adminPlayfabId,
                    (isAdmin) =>
                    {
                        // not an admin
                        if (!isAdmin)
                        {
                            cbResult?.Invoke("Not enough permission to mute.");
                            conn.Broadcast(new ChatBroadcast
                            {
                                Channel = ChatChannel.System,
                                Text = "You do not have permission to mute."
                            });
                            return;
                        }

                    // STEP TWO: Check the target's mute status =============================================================================
                    PlayFabServerAPI.GetUserReadOnlyData(
                            new PlayFab.ServerModels.GetUserDataRequest
                            {
                                PlayFabId = otherPlayfabId,
                                Keys = new List<string> { ChatUtils.MUTE_KEY }
                            },
                            muteResult =>
                            {
                                ChatChannel muteFlags = 0;

                                // if they have mute flags, get those first and check if already muted in channel
                                if (muteResult.Data.TryGetValue(ChatUtils.MUTE_KEY, out PlayFab.ServerModels.UserDataRecord muteRecord))
                                {
                                    if (System.Enum.TryParse(typeof(ChatChannel), muteRecord.Value, out object muteFlagsObj))
                                    {
                                        muteFlags = (ChatChannel)muteFlagsObj;

                                        // already muted in channel
                                        if (muteFlags.HasFlag(channelToMute))
                                        {
                                            conn.Broadcast(new ChatBroadcast
                                            {
                                                Channel = ChatChannel.System,
                                                Text = $"{otherPlayfabId} is already muted in {channelToMute}."
                                            });
                                            return;
                                        }
                                    }
                                }

                                // add the mute flag
                                muteFlags |= channelToMute;

                                // STEP THREE: Save the mute flags back to playfab =============================================================================
                                // save it back to user data
                                PlayFabServerAPI.UpdateUserReadOnlyData(
                                    new PlayFab.ServerModels.UpdateUserDataRequest
                                    {
                                        PlayFabId = otherPlayfabId,
                                        Data = new Dictionary<string, string>
                                        {
                                        { ChatUtils.MUTE_KEY, ((int)muteFlags).ToString() }
                                        }
                                    },
                                    updateResult =>
                                    {
                                        string channelName = channelToMute.ToString();
                                        if (channelName == "-1")
                                            channelName = "all channels";

                                        conn.Broadcast(new ChatBroadcast
                                        {
                                            Channel = ChatChannel.System,
                                            Text = $"User has been muted in {channelName}."
                                        });

                                        // if player online, inform them they have been muted
                                        foreach (var item in InstanceFinder.ServerManager.Clients.Values)
                                        {
                                            if (PlayFabUtils.GetPlayFabId(item) == otherPlayfabId)
                                            {
                                                item.Broadcast(new ChatBroadcast(
                                                    $"You have been muted in {channelName}.",
                                                    ChatChannel.System)
                                                );
                                                break;
                                            }
                                        }
                                    },
                                    HandleError
                                );

                            },
                            HandleError
                        );
                    }
                );
            }
#endif

        }
    }

}

#endif