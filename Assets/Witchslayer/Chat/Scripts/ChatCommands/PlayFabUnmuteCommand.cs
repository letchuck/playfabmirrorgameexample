#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;

using PlayFab;

using System.Collections.Generic;

using Witchslayer.Chat.Broadcasts;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.Commands
{

    // TODO redraw ui chat on unmute
    public class PlayFabUnmuteCommand : ChatCommandBase
    {

        public override string[] CommandKeys => new string[] 
        { 
            "unmute", 
            "removemute" 
        };

        public override void Parse(string[] commands, System.Action<string> onError = null, NetworkConnection conn = null)
        {
            // validate syntax
            if (commands.Length != 3)
            {
                onError?.Invoke("Incorrect syntax. Use: /unmute username channel");
                return;
            }

            // client-side mute handler
#if !DISABLE_PLAYFABCLIENT_API
            if (conn == null)
            {
                // check if player has privileges to mute
                PlayFabClientAPI.GetUserReadOnlyData(
                    new PlayFab.ClientModels.GetUserDataRequest
                    {
                        Keys = new List<string> { ChatUtils.ADMIN_KEY }
                    },
                    result =>
                    {
                        // no admin data
                        if (!result.Data.TryGetValue(ChatUtils.ADMIN_KEY, out PlayFab.ClientModels.UserDataRecord record)
                            // not an admin
                            || (bool.TryParse(record.Value, out bool isAdmin) && !isAdmin))
                        {
                            onError?.Invoke("You do not have permission to do that.");
                            return;
                        }

                        // replace username with playfab ID. because it cannot be obtained using server API and the username.
                        // another option is to use the admin API, but that is not a good idea since it's easy to be rate limited
                        // https://community.playfab.com/questions/37365/is-there-a-way-to-get-playfab-id-by-display-name.html
                        PlayFabClientAPI.GetAccountInfo(
                            new PlayFab.ClientModels.GetAccountInfoRequest
                            {
                                // playfab saves usernames as lower
                                Username = commands[1].ToLower(),
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
                                onError?.Invoke(error.GenerateErrorReport());
                            }
                        );
                    },
                    err =>
                    {
                        onError?.Invoke(err.GenerateErrorReport());
                    }
                );
                return;
            }
#endif


#if ENABLE_PLAYFABSERVER_API
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
                onError?.Invoke("Invalid playfab ID for conn, IP: " + conn.GetAddress());
                return;
            }

            string otherPlayfabId = commands[1];
            string channelName = commands[2];
            ChatChannel channelToUnmute = 0;

            if (channelName == "all" || channelName == "everything")
            {
                channelToUnmute = (ChatChannel)~0;
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

                channelToUnmute = (ChatChannel)channelObj;
            }

            // STEP ONE: Check if they have permission to mute =============================================================================
            // you may disconnect the player on failure because only cheaters would get here
            // since this is already checked on the client
            // You can add custom admin levels and such. This just gets admin privileges as a bool.
            PlayFabUtils.GetAdmin(
                adminPlayfabId,
                (isAdmin) =>
                {
                    if (!isAdmin)
                    {
                        onError?.Invoke("Not enough permission to mute.");
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

                                    // not muted in one or all specified channels
                                    if ((muteFlags & channelToUnmute) == 0)
                                    {
                                        conn.Broadcast(new ChatBroadcast
                                        {
                                            Channel = ChatChannel.System,
                                            Text = $"{otherPlayfabId} is not muted in {channelToUnmute}."
                                        });
                                        return;
                                    }
                                }
                            }

                            // remove the mute flag
                            muteFlags &= ~channelToUnmute;

                            // STEP THREE: Save the mute flags back to playfab user readonly data =============================================================================
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
                                    string channelName = channelToUnmute.ToString();
                                    if (channelName == "-1")
                                        channelName = "all channels";

                                    conn.Broadcast(new ChatBroadcast
                                    {
                                        Channel = ChatChannel.System,
                                        Text = $"User has been unmuted in {channelName}."
                                    });

                                    // if player online, inform them they have been unmuted
                                    foreach (var item in InstanceFinder.ServerManager.Clients.Values)
                                    {
                                        if (PlayFabUtils.GetPlayFabId(item) == otherPlayfabId)
                                        {
                                            item.Broadcast(new ChatBroadcast(
                                                $"You have been unmuted in {channelName}.",
                                                ChatChannel.System
                                            ));
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
#endif

        }

    }

}

#endif
