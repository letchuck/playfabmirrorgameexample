#if USE_PLAYFAB && ENABLE_PLAYFABSERVER_API

using FishNet.Connection;

using System;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using PlayFab;
using Witchslayer.Utilities;

namespace Witchslayer.Chat.MuteCheckers
{


    public class PlayFabWhisperMuteChecker : WhisperMuteCheckerBase
    {

        public override void IsMuted(NetworkConnection senderConn, NetworkConnection receiverConn, Action<bool> isMutedCallback)
        {
            // Need to get username because that's how players mute each other client-side.
            serverChatProcessor.ConnectionInfo.GetUsername(senderConn, (senderUsername) =>
            {
                string receiverPlayfabId = PlayFabUtils.GetPlayFabId(receiverConn);

                // Something went wrong. Consider them muted to prevent sending the message.
                if (string.IsNullOrEmpty(senderUsername) || string.IsNullOrEmpty(receiverPlayfabId))
                {
                    Debug.LogWarning("One or both playfab IDs are invalid");
                    isMutedCallback.Invoke(true);
                    return;
                }

                // get the mute list from client user data
                PlayFabServerAPI.GetUserData(
                    new PlayFab.ServerModels.GetUserDataRequest
                    {
                        PlayFabId = receiverPlayfabId,
                        Keys = new List<string>
                        {
                            ChatUtils.BLOCK_LIST_KEY
                        }
                    },
                    result =>
                    {
                        if (result.Data.TryGetValue(ChatUtils.BLOCK_LIST_KEY, out PlayFab.ServerModels.UserDataRecord record))
                        {
                            List<string> muteList = JsonConvert.DeserializeObject<List<string>>(record.Value);
                            if (muteList.Contains(senderUsername))
                            {
                                // sender is on their mute list
                                isMutedCallback.Invoke(true);
                                return;
                            }
                        }

                        // no mutes found
                        isMutedCallback.Invoke(false);
                    },
                    err =>
                    {
                        // log error
                        Debug.LogError(err.GenerateErrorReport());

                        // something went wrong, invoke false to prevent sending chat 
                        isMutedCallback.Invoke(true);
                    }
                );

            });
        }

    }

}

#endif
