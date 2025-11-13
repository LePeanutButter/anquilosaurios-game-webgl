using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene transitions in a multiplayer environment using Unity Netcode.
/// Handles synchronized fade-in/fade-out animations across all clients
/// and ensures a smooth visual experience when loading new scenes.
/// </summary>
public class SceneTransitionManager : NetworkBehaviour
{
    #region Singleton Setup

    /// <summary>
    /// Global instance of the SceneTransitionManager, ensuring only one exists.
    /// </summary>
    public static SceneTransitionManager Instance;

    [HideInInspector]
    [SerializeField] public float transitionDuration = 1f;

    [SerializeField] private Animator transitionAnimator;

    /// <summary>
    /// Ensures there is only one instance of SceneTransitionManager and prevents it from being destroyed on scene load.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    #endregion

    #region Network Initialization

    /// <summary>
    /// Registers a callback for when a new client connects to the network.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    /// <summary>
    /// Called when a client successfully connects.
    /// If the connected client is the local one (and not the host), plays a local fade-in animation.
    /// </summary>
    /// <param name="clientId">The ID of the connected client.</param>
    private void HandleClientConnected(ulong clientId)
    {
        if (!IsHost && NetworkManager.Singleton.LocalClientId == clientId)
        {
            PlayLocalFadeIn();
        }
    }

    #endregion

    #region Scene Loading & Transitions

    /// <summary>
    /// Initiates a networked scene transition with fade-out and fade-in effects.
    /// Only the server can trigger a synchronized scene load.
    /// </summary>
    /// <param name="sceneName">The name of the target scene to load.</param>
    /// <param name="mode">The scene loading mode (Single or Additive).</param>
    public void LoadSceneWithTransition(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!IsServer || NetworkManager.Singleton == null) return;

        StartCoroutine(FadeOutAndLoad(sceneName, mode));
    }

    /// <summary>
    /// Performs a fade-out animation across all clients, waits for it to finish,
    /// then loads the target scene through the NetworkManager.
    /// </summary>
    private IEnumerator FadeOutAndLoad(string sceneName, LoadSceneMode mode)
    {
        PlayFadeOutClientRpc();

        yield return new WaitForSeconds(transitionDuration);

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, mode);

        StartCoroutine(WaitForSceneLoad(sceneName));
    }


    /// <summary>
    /// Waits until the specified scene is fully loaded, ensures that any gameplay
    /// initialization (like GameRoundManager) is complete, and then triggers a fade-in animation.
    /// </summary>
    private IEnumerator WaitForSceneLoad(string sceneName)
    {
        bool sceneLoaded = false;

        void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == sceneName)
            {
                sceneLoaded = true;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        while (!sceneLoaded)
            yield return null;

        yield return null;
        yield return null;

        var gameRoundManager = GameRoundManager.Instance;
        if (gameRoundManager != null)
        {
            while (!gameRoundManager.isRoundInitialized.Value)
            {
                yield return null;
            }
            Debug.Log("GameRoundManager: Round initialization confirmed by the server.");
        }

        PlayFadeInClientRpc();
    }

    #endregion

    #region Fade Animations


    /// <summary>
    /// Plays a fade animation trigger on the transition animator.
    /// Optionally starts the fade idle coroutine after completion.
    /// </summary>
    private void PlayFade(string trigger, bool startIdleCoroutine)
    {
        if (transitionAnimator)
            transitionAnimator.SetTrigger(trigger);

        if (startIdleCoroutine)
            StartCoroutine(FadeIdleCoroutine());
    }

    /// <summary>
    /// Plays a fade animation with optional sound effects.
    /// Can play sound locally or across the network.
    /// </summary>
    private void PlayFadeWithAudio(string trigger, bool startIdleCoroutine, bool networked)
    {
        PlayFade(trigger, startIdleCoroutine);

        if (AudioManager.Instance == null)
            return;

        if (networked)
            AudioManager.Instance.PlaySFXNetworked(AudioManager.Instance.transition);
        else
            AudioManager.Instance.PlaySFX(AudioManager.Instance.transition);
    }

    #endregion

    #region Networked Fade RPCs

    /// <summary>
    /// Sends a command from the server to all clients to play the fade-out animation.
    /// </summary>
    [ClientRpc]
    private void PlayFadeOutClientRpc()
    {
        PlayFadeWithAudio("FadeOut", false, true);
    }

    /// <summary>
    /// Sends a command from the server to all clients to play the fade-in animation.
    /// </summary>
    [ClientRpc]
    private void PlayFadeInClientRpc()
    {
        PlayFadeWithAudio("FadeIn", true, true);
    }

    #endregion

    #region Local Fades (Non-Networked)

    /// <summary>
    /// Plays a local fade-out animation (not synchronized over the network).
    /// </summary>
    public void PlayLocalFadeOut()
    {
        PlayFadeWithAudio("FadeOut", false, false);
    }

    /// <summary>
    /// Plays a local fade-in animation (not synchronized over the network).
    /// </summary>
    public void PlayLocalFadeIn()
    {
        PlayFadeWithAudio("FadeIn", true, false);
    }

    #endregion

    #region Utility Coroutines

    /// <summary>
    /// Waits for the transition duration and then resets the animator to its "Idle" state.
    /// Used to keep the animation system stable between transitions.
    /// </summary>
    private IEnumerator FadeIdleCoroutine()
    {
        yield return new WaitForSeconds(transitionDuration);

        if (transitionAnimator)
            transitionAnimator.SetTrigger("Idle");
    }

    #endregion
}
