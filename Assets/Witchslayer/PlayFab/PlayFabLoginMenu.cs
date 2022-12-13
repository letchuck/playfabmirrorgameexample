#if USE_PLAYFAB

using FishNet;
using FishNet.Transporting;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Witchslayer.Authenticators;

namespace Witchslayer.Chat.Examples
{

    public class PlayFabLoginMenu : MonoBehaviour
    {

        [SerializeField] private PlayFabAuthenticator _auth = null;

        [Header("UI Controls")]
        [SerializeField] private GameObject _panel = null;
        [SerializeField] private TMP_InputField _userInput = null;
        [SerializeField] private TMP_InputField _emailInput = null;
        [SerializeField] private TMP_InputField _passwordInput = null;
        [SerializeField] private Button _loginButton = null;
        [SerializeField] private Button _registerButton = null;
        [SerializeField] private TMP_Text _errorText = null;

        /// <summary>
        /// True if the client is trying to log in and the connection is starting.
        /// </summary>
        private bool _starting;

        private void OnEnable()
        {
            _errorText.text = "";

            _loginButton.onClick.AddListener(OnLogin);
            _registerButton.onClick.AddListener(OnRegister);

            InstanceFinder.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            InstanceFinder.ClientManager.OnAuthenticated += ClientManager_OnAuthenticated;

            _auth.OnLoginError.AddListener(OnLoginError);
        }

        private void OnDisable()
        {
            _loginButton.onClick.RemoveListener(OnLogin);
            _registerButton.onClick.RemoveListener(OnRegister);

            _auth.OnLoginError.RemoveListener(OnLoginError);

            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
                InstanceFinder.ClientManager.OnAuthenticated -= ClientManager_OnAuthenticated;
            }
        }

        /// <summary>
        /// Handles login error broadcast from server.
        /// </summary>
        /// <param name="err"></param>
        /// <param name="asServer"></param>
        private void OnLoginError(string err, bool asServer)
        {
            if (asServer) return;

            _errorText.text = err;
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// Register callback.
        /// </summary>
        private void OnRegister()
        {
            SetButtonsInteractable(false);
            _errorText.text = "";

            PlayFabClientAPI.RegisterPlayFabUser(
                new RegisterPlayFabUserRequest
                {
                    Username = _userInput.text,
                    Email = _emailInput.text,
                    Password = _passwordInput.text,
                },
                result =>
                {
                    _errorText.text = "Registration successful! You may now log in.";
                    SetButtonsInteractable(true);
                },
                error =>
                {
                    _errorText.text = "Registration error: " + error.GenerateErrorReport();
                    SetButtonsInteractable(true);
                }
            );
        }

        private void OnLogin()
        {
            SetButtonsInteractable(false);
            _errorText.text = "";

            PlayFabAuthenticator.ClientPlayFabLogin(_userInput.text, _passwordInput.text, HandleLoginSuccess, HandleLoginFailure);
        }

        private void HandleLoginSuccess(LoginResult result)
        {
        }

        private void HandleLoginFailure(PlayFabError err)
        {
            _errorText.text = "Error logging in: " + err.GenerateErrorReport();
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool enabled)
        {
            // TODO also set tabs unchangeable
            _loginButton.interactable = enabled;
            _registerButton.interactable = enabled;
        }

        /// <summary>
        /// Successfully authenticated callback.
        /// </summary>
        private void ClientManager_OnAuthenticated()
        {
            _panel.SetActive(false);
        }

        /// <summary>
        /// Detects if client cannot connect to server.
        /// </summary>
        /// <param name="args"></param>
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Starting)
            {
                _starting = true;
                _errorText.text = "Connecting to server...";
                return;
            }

            if ((args.ConnectionState == LocalConnectionState.Stopping || args.ConnectionState == LocalConnectionState.Stopped)
                && _starting)
            {
                _errorText.text = "Server is offline.";
            }
            else if (args.ConnectionState != LocalConnectionState.Stopped)
            {
                _errorText.text = "";
                _panel.SetActive(true);
            }

            _starting = false;
            SetButtonsInteractable(true);
        }

    }

}

#endif
