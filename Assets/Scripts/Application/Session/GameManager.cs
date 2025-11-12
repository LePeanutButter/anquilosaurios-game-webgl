using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the lobby UI, allowing users to host or join a multiplayer session.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("Button to start hosting a session.")]
    public Button hostButton;

    [Tooltip("Button to join an existing session.")]
    public Button joinButton;

    [Header("Input")]
    [Tooltip("Input field for entering the session code.")]
    public TMP_InputField sessionCodeInput;

    /// <summary>
    /// Initializes button listeners when the scene starts.
    /// </summary>
    private void Start()
    {
        hostButton.onClick.AddListener(async () => await OnCreateSessionClicked());
        joinButton.onClick.AddListener(async () => await OnJoinClicked());
    }


    /// <summary>
    /// Called when the "Create Session" button is clicked.
    /// Creates a new session.
    /// </summary>
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
    /// Called when the join button is clicked.
    /// Attempts to join a session using the provided session name.
    /// </summary>
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

        await SessionManager.Instance.JoinSessionByCodeAsync(code);

        if (SessionManager.Instance.ActiveSession != null)
        {
            Debug.Log("Joined session successfully, playing local transition...");

            Debug.Log("Starting client...");
            NetworkManager.Singleton.StartClient();
        } 
        else
        {
            Debug.LogError("Failed to join session with the provided code.");
            SceneTransitionManager.Instance.PlayLocalFadeIn();
        }
    }
}