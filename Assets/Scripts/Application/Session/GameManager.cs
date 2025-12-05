using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the lobby UI, allowing users to host or join a multiplayer session.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Public Fields

    [Header("Buttons")]
    [Tooltip("Button to start hosting a session.")]
    public Button hostButton;

    [Tooltip("Button to join an existing session.")]
    public Button joinButton;

    [Header("Input")]
    [Tooltip("Input field for entering the session code.")]
    public TMP_InputField sessionCodeInput;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes button listeners when the scene starts.
    /// </summary>
    private void Start()
{
    if (hostButton != null)
        hostButton.onClick.AddListener(async () => await OnCreateSessionClicked());

    if (joinButton != null)
        joinButton.onClick.AddListener(async () => await OnJoinClicked());
}


    #endregion

    #region Session Management Methods

    /// <summary>
    /// Called when the "Create Session" button is clicked.
    /// Creates a new multiplayer session and starts the host.
    /// </summary>
    /// <returns>Asynchronous task that completes when the session creation is done.</returns>
    private async Task OnCreateSessionClicked()
    {
        Debug.Log("Creating new session...");

        await SessionManager.Instance.CreateSessionAsync();

        if (SessionManager.Instance.ActiveSession != null)
        {
            Debug.Log("Session created successfully, starting host...");

            SceneTransitionManager.Instance.LoadSceneWithTransition("LobbyScene");
        }
        else
        {
            Debug.LogError("Failed to create session.");
        }
    }

    /// <summary>
    /// Called when the "Join Session" button is clicked.
    /// Attempts to join a multiplayer session using the provided session code.
    /// </summary>
    /// <returns>Asynchronous task that completes when the session join process is done.</returns>
    private async Task OnJoinClicked()
    {
        var code = sessionCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("You must enter a session code to join.");
            return;
        }

        Debug.Log($"Joining session with code: {code}...");

        SceneTransitionManager.Instance.PlayLocalFadeOut();
        float time = SceneTransitionManager.Instance.transitionDuration + 0.5f;

        try
        {
            await SessionManager.Instance.JoinSessionByCodeAsync(code);

            if (SessionManager.Instance.ActiveSession != null)
            {
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.LogError("Failed to join session after join call. Session may not exist or join failed.");
                var ms = (int)(time * 1000);
                await Task.Delay(ms);
                SceneTransitionManager.Instance.PlayLocalFadeIn();
            }
        }
        catch (Exception joinEx)
        {
            Debug.LogError($"Join failed: {joinEx.Message}");
            var ms = (int)(time * 1000);
            await Task.Delay(ms);
            SceneTransitionManager.Instance.PlayLocalFadeIn();
        }
    }

    #endregion
}