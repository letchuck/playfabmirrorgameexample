#if USE_PLAYFAB && FISHNET 

using FishNet.Authenticating;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Broadcast;

using UnityEngine;
using UnityEngine.Events;

using PlayFab;
using Witchslayer.Utilities;
using PlayFab.ClientModels;
using FishNet;

namespace Witchslayer.Authenticators
{

    /// <summary>
    /// Authenticates the user using playfab authentication API.
    /// </summary>
    public class PlayFabAuthenticator : Authenticator
    {

        /// <summary>
        /// Session ticket sent by the client to be validated by the server.
        /// </summary>
        private struct LoginBroadcast : IBroadcast
        {
            /// <summary>
            /// The ticket generated using the playfab client API that needs to be authenticated by server
            /// to spawn the player.
            /// </summary>
            public string SessionTicket;
        }

        /// <summary>
        /// Authentication error sent by the server.
        /// </summary>
        private struct LoginErrorBroadcast : IBroadcast
        {
            /// <summary>
            /// Login error message.
            /// </summary>
            public string Err;
        }

        // abstract
        public override event System.Action<NetworkConnection, bool> OnAuthenticationResult;

        /// <summary>
        /// The session ticket acquired from PlayFabClientAPI login result that will be used to authenticate on server.
        /// Make sure to set this before starting a client connection.
        /// </summary>
        internal static string SessionTicket;

        /// <summary>
        /// Invokes the error when client receives msg from server that login failed,
        /// and bool asServer.
        /// </summary>
        [Space]
        public UnityEvent<string, bool> OnLoginError;

        /// <summary>
        /// (Client only) UserAccountInfo cached when logging in via client API.
        /// </summary>
        public static UserAccountInfo UserInfo;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);

#if UNITY_SERVER || UNITY_EDITOR
            // Listen for broadcast from client. Be sure to set requireAuthentication to false.
            base.NetworkManager.ServerManager.RegisterBroadcast<LoginBroadcast>(OnLoginBroadcast, false);
#endif

            // Listen for connection state change as client.
            base.NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            base.NetworkManager.ClientManager.OnAuthenticated += ClientManager_OnAuthenticated;

            // Listen to response from server.
            base.NetworkManager.ClientManager.RegisterBroadcast<LoginErrorBroadcast>(OnLoginErrorBroadcast);
        }

        private void OnDestroy()
        {
#if UNITY_SERVER || UNITY_EDITOR
            base.NetworkManager.ServerManager.UnregisterBroadcast<LoginBroadcast>(OnLoginBroadcast);
#endif

            base.NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            base.NetworkManager.ClientManager.OnAuthenticated -= ClientManager_OnAuthenticated;

            base.NetworkManager.ClientManager.UnregisterBroadcast<LoginErrorBroadcast>(OnLoginErrorBroadcast);
        }


        #region Server
#if UNITY_SERVER || UNITY_EDITOR 

        private bool IsClientLoggedIn(string playFabId)
        {
            foreach (var item in base.NetworkManager.ServerManager.Clients)
            {
                NetworkConnection conn = item.Value;
                if (conn.CustomData != null && conn.CustomData is PlayFab.ServerModels.UserAccountInfo info)
                {
                    if (item.Value.Authenticated && info.PlayFabId == playFabId)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a LoginErrorBroadcast to the conn with the err text.
        /// </summary>
        /// <param name="conn"></param>Connection to send to.
        /// <param name="error"></param>Error message.
        void SendLoginErrorBroadcast(NetworkConnection conn, string error)
        {
            base.NetworkManager.ServerManager.Broadcast(conn, new LoginErrorBroadcast { Err = error }, false);
        }

        private void OnLoginBroadcast(NetworkConnection conn, LoginBroadcast msg)
        {
            // already authenticated
            if (conn.Authenticated)
            {
                conn.Disconnect(true);
                return;
            }

#if ENABLE_PLAYFABSERVER_API
            PlayFabServerAPI.AuthenticateSessionTicket(
                new PlayFab.ServerModels.AuthenticateSessionTicketRequest
                {
                    SessionTicket = msg.SessionTicket,
                },
                result =>
                {
                    // expired ticket
                    if (result.IsSessionTicketExpired == true)
                    {
                        SendLoginErrorBroadcast(conn, "Session ticket expired. Try logging in again.");
                        OnAuthenticationResult?.Invoke(conn, false);
                        return;
                    }

                    // user already logged in
                    if (IsClientLoggedIn(result.UserInfo.PlayFabId))
                    {
                        SendLoginErrorBroadcast(conn, "Already logged in.");
                        Debug.LogWarning("(SERVER) Already logged in!");
                        OnAuthenticationResult?.Invoke(conn, false);
                        return;
                    }

                    PlayFabUtils.GetBan(
                        result.UserInfo.PlayFabId,
                        isBanned =>
                        {
                            // only cheaters would get here if they are banned
                            if (isBanned)
                            {
                                SendLoginErrorBroadcast(conn, "You are banned.");
                                OnAuthenticationResult?.Invoke(conn, false);
                                conn.Disconnect(false);
                                return;
                            }

                            // cache UserAccountInfo to be used for playfab-based systems, such as the ChatSystem.
                            conn.CustomData = result.UserInfo;

                            // No need to send a loginSuccessMsg to client since the authentication will indicate that
                            Debug.Log("(SERVER) Successfully logged in as " + result.UserInfo.PlayFabId);
                            OnAuthenticationResult?.Invoke(conn, true);
                        },
                        banError =>
                        {
                            OnAuthenticationResult?.Invoke(conn, false);
                            SendLoginErrorBroadcast(conn, "Error getting ban record.");
                            Debug.LogError("Error getting ban record.");
                            conn.Disconnect(false);
                        }
                    );

                },
                err =>
                {
                    string errorReport = err.GenerateErrorReport();
                    OnLoginError?.Invoke(errorReport, true);

                    // send error back to client
                    SendLoginErrorBroadcast(conn, errorReport);

                    // failed authentication
                    OnAuthenticationResult?.Invoke(conn, false);
                }
            );
#endif
        }

#endif
        #endregion


        #region Client

        private void ClientManager_OnAuthenticated()
        {
            NetworkManager.ClientManager.Connection.CustomData = UserInfo;
        }

        /// <summary>
        /// Call this from your login menu to login client-side. If successful, the client connection will start with inspector values
        /// and send the session ticket from the result to the server where the PlayFab server API will authenticate the connection.
        /// Alternatively, you may log in and set the SessionTicket yourself before starting the connection.
        /// </summary>
        /// <param name="username"></param>Username/email to login with. If this contains an '@' symbol, it will consider it an email.
        /// <param name="password"></param>Account password.
        /// <param name="onLoginSuccess"></param>Invoked with the LoginResult if successful.
        /// <param name="onLoginFailure"></param>Invoked with the PlayFabError on failure.
        public static void ClientPlayFabLogin(string username, string password, 
            System.Action<LoginResult> onLoginSuccess, System.Action<PlayFabError> onLoginFailure)
        {
            if (username.Contains('@'))
            {
                PlayFabClientAPI.LoginWithEmailAddress(
                    new LoginWithEmailAddressRequest
                    {
                        Email = username,
                        Password = password,
                    },
                    result => HandleLoginResult(result, onLoginSuccess),
                    onLoginFailure
                );
            }
            else
            {
                PlayFabClientAPI.LoginWithPlayFab(
                    new LoginWithPlayFabRequest
                    {
                        Username = username,
                        Password = password,
                        InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                        {
                            GetUserAccountInfo = true
                        }
                    },
                    result => HandleLoginResult(result, onLoginSuccess),
                    onLoginFailure
                );
            }
        }

        private static void HandleLoginResult(LoginResult result, 
            System.Action<LoginResult> onLoginSuccess)
        {
            UserInfo = result.InfoResultPayload.AccountInfo;
            SessionTicket = result.SessionTicket;
            InstanceFinder.NetworkManager.ClientManager.StartConnection();

            onLoginSuccess?.Invoke(result);
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                UserInfo = null;
                return;
            }

            if (args.ConnectionState != LocalConnectionState.Started) return;

            // check if banned
            PlayFabUtils.GetBan(
                null, 
                (isBanned) =>
                {
                    if (isBanned)
                    {
                        base.NetworkManager.ClientManager.StopConnection();
                        OnLoginErrorBroadcast(new LoginErrorBroadcast { Err = "You are banned." });
                        return;
                    }

                    base.NetworkManager.ClientManager.Broadcast(new LoginBroadcast
                    {
                        SessionTicket = PlayFabAuthenticator.SessionTicket,
                    });

                    // clear ticket after use
                    PlayFabAuthenticator.SessionTicket = "";
                },
                err =>
                {
                    base.NetworkManager.ClientManager.StopConnection();
                    OnLoginErrorBroadcast(new LoginErrorBroadcast { Err = err });
                    return;
                }
            );
        }

        /// <summary>
        /// Client login error handler.
        /// </summary>
        /// <param name="msg"></param>
        private void OnLoginErrorBroadcast(LoginErrorBroadcast msg)
        {
            Debug.LogError(msg.Err);
            OnLoginError?.Invoke(msg.Err, false);
        }

        #endregion


    }

}

#endif
