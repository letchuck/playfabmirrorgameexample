#if USE_PLAYFAB

using FishNet;
using FishNet.Connection;

using PlayFab;

using System;
using System.Collections.Generic;

using UnityEngine;

using Witchslayer.Chat;

namespace Witchslayer.Utilities
{

    public class PlayFabUtils
    {

        #region Client API
#if !DISABLE_PLAYFABCLIENT_API

        /// <summary>
        /// Available on server only. Gets the UserAccountInfo from the custom data set on the connection.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static PlayFab.ClientModels.UserAccountInfo GetUserAccountInfo()
        {
            if (InstanceFinder.ClientManager.Started && InstanceFinder.ClientManager.Connection.Authenticated)
            {
                if (InstanceFinder.ClientManager.Connection.CustomData is PlayFab.ClientModels.UserAccountInfo user)
                {
                    return user;
                }
            }

            Debug.LogWarning("Couldn't get client user info.");
            return null;
        }

        /// <summary>
        /// Replace username with playfab ID because it cannot be obtained using server API.
        /// another option is to use the admin API on the server, but it's easy to be rate limited:
        /// https://community.playfab.com/questions/37365/is-there-a-way-to-get-playfab-id-by-display-name.html
        /// </summary>
        /// <param name="playFabId"></param>
        /// <param name="commands"></param>
        /// <param name="requireAdmin"></param>
        /// <param name="onError"></param>
        /// <param name="onResult"></param>
        public static void ReplaceUsernameWithPlayFabId(string[] commands, bool requireAdmin, 
            Action<string> onError, Action<string[]> onResult)
        {
            if (commands==null || commands.Length < 2)
            {
                onError.Invoke("Invalid command parameters. Requires at least: /command username");
                return;
            }

            void ReplaceWithId()
            {
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

                        onResult.Invoke(commands);
                    },
                    error =>
                    {
                        onError?.Invoke(error.GenerateErrorReport());
                    }
                );
            }

            if (requireAdmin)
            {
                GetAdmin(null, (isAdmin) =>
                {
                    if (!isAdmin)
                    {
                        onError?.Invoke("You do not have permission to do that.");
                        return;
                    }

                    ReplaceWithId();
                });
            }
            else
            {
                ReplaceWithId();
            }

        }

#endif
        #endregion


        /// <summary>
        /// Gets the playfab id from the custom data set on the connection.
        /// If conn is null, it will get the playfab ID on the local client. Pass conn if using this method on server.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static string GetPlayFabId(NetworkConnection conn=null)
        {
#if !DISABLE_PLAYFABCLIENT_API
            if (conn == null && InstanceFinder.ClientManager.Started)
            {
                conn = InstanceFinder.ClientManager.Connection;
                if (conn.Authenticated && conn.CustomData is PlayFab.ClientModels.UserAccountInfo clientInfo)
                {
                    return clientInfo.PlayFabId;
                }
            }
#endif

#if ENABLE_PLAYFABSERVER_API
            // assumes you set this on the server when client connected
            if (conn != null && conn.Authenticated)
            {
                if (conn.CustomData is PlayFab.ServerModels.UserAccountInfo user)
                    return user.PlayFabId;
                // i guess this can occur on host, and it does
                else if (conn.CustomData is PlayFab.ClientModels.UserAccountInfo clientUser)
                    return clientUser.PlayFabId;
            }
#endif

            if (conn == null)
                Debug.LogWarning("Couldn't get client info");
            else
                Debug.LogWarning("Couldn't get server info");

            return null;
        }



        #region Server API
#if ENABLE_PLAYFABSERVER_API

        /// <summary>
        /// Available on server only. Gets the UserAccountInfo from the custom data set on the connection.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static PlayFab.ServerModels.UserAccountInfo GetUserAccountInfo(NetworkConnection conn)
        {
            // assumes you set this on the server when client connected
            if (conn != null && conn.Authenticated
                && conn.CustomData is PlayFab.ServerModels.UserAccountInfo user)
                return user;
            return null;
        }

        /// <summary>
        /// Gets the admin bool value from playfab readonly user data.
        /// Pass null playFabId if checking on client.
        /// Invokes true if admin.
        /// </summary>
        /// <param name="playFabId"></param>PlayFab ID to check. Pass null to check on client.
        /// <param name="cb"></param>Callback result for if they are an admin or not.
        public static void GetAdmin(string playFabId, Action<bool> cb)
        {
            // no playfab ID needs to be specified if checking on client
            if (string.IsNullOrEmpty(playFabId))
            {
#if !DISABLE_DISABLE_PLAYFABCLIENT_API
                PlayFabClientAPI.GetUserReadOnlyData(
                    new PlayFab.ClientModels.GetUserDataRequest
                    {
                        Keys = new List<string> { ChatUtils.ADMIN_KEY }
                    },
                    result =>
                    {
                        cb.Invoke(
                            // admin key on record
                            result.Data.TryGetValue(ChatUtils.ADMIN_KEY, out PlayFab.ClientModels.UserDataRecord getRecord)
                            // valid bool record vlaue
                            && bool.TryParse(getRecord.Value, out bool isAdmin)
                            // is an admin
                            && isAdmin
                        );
                    },
                    err =>
                    {
                        cb.Invoke(false);
                    }
                );
#endif
            }
#if ENABLE_PLAYFABSERVER_API
            else
            {
                PlayFabServerAPI.GetUserReadOnlyData(
                    new PlayFab.ServerModels.GetUserDataRequest
                    {
                        PlayFabId = playFabId,
                        Keys = new List<string> { ChatUtils.ADMIN_KEY }
                    },
                    result =>
                    {
                        cb.Invoke(
                            // admin key on record
                            result.Data.TryGetValue(ChatUtils.ADMIN_KEY, out PlayFab.ServerModels.UserDataRecord getRecord)
                            // valid bool record vlaue
                            && bool.TryParse(getRecord.Value, out bool isAdmin)
                            // is an admin
                            && isAdmin
                        );
                    },
                    err =>
                    {
                        cb.Invoke(false);
                    }
                );
            }
#endif

        }

        /// <summary>
        /// Gets the mute channel flags for the user. 
        /// </summary>
        /// <param name="playFabId"></param>
        /// <param name="onMuteFlags"></param>
        public static void GetMute(string playFabId, Action<ChatChannel> onMuteFlags, Action<string> onError)
        {
            PlayFabServerAPI.GetUserReadOnlyData(
                new PlayFab.ServerModels.GetUserDataRequest
                {
                    PlayFabId = playFabId,
                    Keys = new List<string> { ChatUtils.MUTE_KEY }
                },
                result =>
                {
                    ChatChannel muteFlags = 0;

                    // if they have mute flags, get those first and check if already muted in channel
                    if (result.Data.TryGetValue(ChatUtils.MUTE_KEY, out PlayFab.ServerModels.UserDataRecord muteRecord))
                    {
                        if (System.Enum.TryParse(typeof(ChatChannel), muteRecord.Value, out object muteFlagsObj))
                        {
                            muteFlags = (ChatChannel)muteFlagsObj;
                        }
                    }

                    onMuteFlags?.Invoke(muteFlags);
                },
                err =>
                {
                    onError.Invoke(err.GenerateErrorReport());
                }
            );
        }

        public static void GetBan(string playFabId, Action<bool> onResult, Action<string> onError)
        {
            // client 
            if (string.IsNullOrEmpty(playFabId))
            {
#if !DISABLE_DISABLE_PLAYFABCLIENT_API
                PlayFabClientAPI.GetUserReadOnlyData(
                    new PlayFab.ClientModels.GetUserDataRequest
                    {
                        Keys = new List<string> { ChatUtils.BAN_KEY }
                    },
                    result =>
                    {
                        if (result.Data.TryGetValue(ChatUtils.BAN_KEY, out PlayFab.ClientModels.UserDataRecord record))
                        {
                            if (DateTime.TryParse(record.Value, out DateTime banEndDate))
                            {
                                if (banEndDate > DateTime.UtcNow)
                                {
                                    onResult.Invoke(true);
                                    return;
                                }
                            }
                        }

                        onResult.Invoke(false);
                    },
                    err =>
                    {
                        onError.Invoke(err.GenerateErrorReport());
                    }
                );
#else
                // let them pass
                onResult?.Invoke(false);
                onError?.Invoke("Couldn't get ban record");
#endif
            }
#if ENABLE_PLAYFABSERVER_API
            else
            {
                PlayFabServerAPI.GetUserReadOnlyData(
                    new PlayFab.ServerModels.GetUserDataRequest
                    {
                        PlayFabId = playFabId,
                        Keys = new List<string> { ChatUtils.BAN_KEY }
                    },
                    result =>
                    {
                        if (result.Data.TryGetValue(ChatUtils.BAN_KEY, out PlayFab.ServerModels.UserDataRecord record))
                        {
                            if (DateTime.TryParse(record.Value, out DateTime banEndDate))
                            {
                                if (banEndDate > DateTime.UtcNow)
                                {
                                    onResult.Invoke(true);
                                    return;
                                }
                            }
                        }

                        onResult.Invoke(false);
                    },
                    err =>
                    {
                        onError.Invoke(err.GenerateErrorReport());
                    }
                );
            }
#endif
        }

#endif
#endregion

            }

}

#endif
