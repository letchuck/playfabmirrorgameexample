#if USE_PLAYFAB

using FishNet.Connection;
using Newtonsoft.Json;
using PlayFab;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.Commands
{

    public class PlayFabUnblockCommand : ChatCommandBase
    {
        public override string[] CommandKeys => new string[]
        {
            "unblock",
            "removeblock"
        };

        public override void Parse(string[] commands, Action<string> onResult = null, NetworkConnection conn = null)
        {
            if (commands.Length > 1)
            {
                string playerToUnblock = commands[1];

#if !DISABLE_PLAYFABCLIENT_API
                PlayFabClientAPI.GetUserData(
                    new PlayFab.ClientModels.GetUserDataRequest
                    {
                        Keys = new List<string>
                        {
                            ChatUtils.BLOCK_LIST_KEY
                        }
                    },
                    getResult =>
                    {
                        // deserialize into a list
                        List<string> blockList = new List<string>();
                        if (getResult.Data.TryGetValue(ChatUtils.BLOCK_LIST_KEY, out PlayFab.ClientModels.UserDataRecord record))
                            blockList = JsonConvert.DeserializeObject<List<string>>(record.Value);

                        // cant unblock who's not blocked
                        if (!blockList.Contains(playerToUnblock))
                        {
                            Debug.LogWarning(playerToUnblock + " is not blocked.");
                            return;
                        }

                        // remove player from block list
                        blockList.Remove(playerToUnblock);

                        // send update to playfab
                        PlayFabClientAPI.UpdateUserData(
                            new PlayFab.ClientModels.UpdateUserDataRequest()
                            {
                                Data = new Dictionary<string, string>
                                {
                                    { ChatUtils.BLOCK_LIST_KEY, JsonConvert.SerializeObject(blockList) }
                                },
                            },
                            updateResult =>
                            {
                                onResult?.Invoke($"You have unblocked {playerToUnblock}.");
                            },
                            updateError =>
                            {
                                onResult?.Invoke(updateError.GenerateErrorReport());
                            }
                        );

                    },
                    getError =>
                    {
                        onResult?.Invoke(getError.GenerateErrorReport());
                    }
                );
#else
                onResult?.Invoke("Not yet implemented.");
#endif
            }
        }
    }

}

#endif
