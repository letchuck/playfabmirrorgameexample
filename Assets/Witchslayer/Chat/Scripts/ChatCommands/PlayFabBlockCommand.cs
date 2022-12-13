#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;
using Newtonsoft.Json;
using PlayFab;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.Commands
{

    public class PlayFabBlockCommand : ChatCommandBase
    {

        public override string[] CommandKeys => new string[]
        {
            "block"
        };

        public override void Parse(string[] commands, Action<string> cbResult = null, NetworkConnection conn = null)
        {
            void HandleError(PlayFabError err)
            {
                cbResult?.Invoke(err.GenerateErrorReport());
            }

            if (commands.Length > 1)
            {
                string playerToBlock = commands[1].ToLower();

#if !DISABLE_PLAYFABCLIENT_API
                if (conn == null)
                {
                    // prevent blocking self
                    if (InstanceFinder.ClientManager?.Connection?.CustomData is PlayFab.ClientModels.UserAccountInfo info)
                    {
                        if (info.Username == playerToBlock)
                        {
                            cbResult?.Invoke("You cannot block yourself!");
                            return;
                        }
                    }

                    // does the user exist?
                    PlayFabClientAPI.GetAccountInfo(
                        new PlayFab.ClientModels.GetAccountInfoRequest
                        {
                            Username = playerToBlock
                        },
                        result =>
                        {
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
                                    // deserialize json into list
                                    List<string> blockList = new List<string>();
                                    if (getResult.Data.TryGetValue(ChatUtils.BLOCK_LIST_KEY, out PlayFab.ClientModels.UserDataRecord record))
                                        blockList = JsonConvert.DeserializeObject<List<string>>(record.Value);

                                    // cant block who's already blocked
                                    if (blockList.Contains(playerToBlock))
                                    {
                                        Debug.LogWarning(playerToBlock + " is already blocked.");
                                        return;
                                    }

                                    // add the player to block list
                                    blockList.Add(playerToBlock);

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
                                            cbResult?.Invoke($"You have blocked {playerToBlock}.");
                                        },
                                        HandleError
                                    );

                                },
                                HandleError
                            );

                        },
                        HandleError
                    );

                }
#endif

            }
        }

    }

}

#endif
