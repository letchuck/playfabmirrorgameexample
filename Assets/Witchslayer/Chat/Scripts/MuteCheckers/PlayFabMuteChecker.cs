#if USE_PLAYFAB

using PlayFab;

using FishNet;
using FishNet.Connection;

using System;
using System.Collections.Generic;

using UnityEngine;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.MuteCheckers
{

    /// <summary>
    /// Uses read-only user data set on playfab to check mutes asynchronously.
    /// </summary>
    public class PlayFabMuteChecker : MuteCheckerBase
    {

        public const string MUTE_KEY = "Mute";

        public override void IsMuted(NetworkConnection conn, ChatChannel channel, Action<bool> callback)
        {
            // Remember that on the client, the ID can be null because they should be signed in locally.
            string playfabId = null;

            // server mute checker
#if ENABLE_PLAYFABSERVER_API

            // You may redesign this as needed to get the proper playfabID.
            playfabId = PlayFabUtils.GetPlayFabId(conn);

            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started
                && !string.IsNullOrWhiteSpace(playfabId))
            {
                PlayFabServerAPI.GetUserReadOnlyData(
                    new PlayFab.ServerModels.GetUserDataRequest
                    {
                        PlayFabId = playfabId,
                        Keys = new List<string>
                        {
                            // ChatChannels type, allows for muting only in certain channels
                            MUTE_KEY
                        }
                    },
                    result =>
                    {
                        bool isMuted = false;

                        if (result?.Data != null
                            && result.Data.TryGetValue(MUTE_KEY, out PlayFab.ServerModels.UserDataRecord record))
                        {
                            if (Enum.TryParse(typeof(ChatChannel), record.Value, out object val))
                            {
                                if (val is ChatChannel muteFlags)
                                {
                                    isMuted = (muteFlags & channel) != 0;
                                }
                            }
                        }

                        callback.Invoke(isMuted);
                    },
                    err =>
                    {
                        // log error
                        Debug.LogError(err.GenerateErrorReport());

                        // something went wrong, invoke false to prevent sending chat 
                        callback.Invoke(true);
                    }
                );
            }

#endif

            // client side mute check
#if !DISABLE_PLAYFABCLIENT_API

            if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started
                && !InstanceFinder.IsHost)
            {
                // you may kick cheaters on server if they make it past this while muted
                PlayFabClientAPI.GetUserReadOnlyData(
                    new PlayFab.ClientModels.GetUserDataRequest
                    {
                        Keys = new List<string>
                        {
                            // ChatChannels type, allows for muting only in certain channels
                            MUTE_KEY
                        }
                    },
                    result =>
                    {
                        bool isMuted = false;

                        // check mute record, if any
                        if (result?.Data != null
                            && result.Data.TryGetValue(MUTE_KEY, out PlayFab.ClientModels.UserDataRecord record))
                        {
                            // parse mute record
                            if (Enum.TryParse(typeof(ChatChannel), record.Value, out object val))
                            {
                                if (val is ChatChannel muteFlags)
                                {
                                    isMuted = (muteFlags & channel) != 0;
                                }
                            }
                        }

                        callback.Invoke(isMuted);
                    },
                    err =>
                    {
                        Debug.LogError(err.GenerateErrorReport());

                        // something went wrong. consider them muted so they cant send the chat
                        callback.Invoke(true);
                    }
                );
            }

#endif

        }


    }

}

#endif
