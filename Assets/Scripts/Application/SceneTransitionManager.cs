using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : NetworkBehaviour
{
    public static SceneTransitionManager Instance;
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private float transitionDuration = 1f;

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

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsHost && NetworkManager.Singleton.LocalClientId == clientId)
        {
            PlayLocalFadeIn();
        }
    }

    public void LoadSceneWithTransition(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!IsServer || NetworkManager.Singleton == null) return;

        StartCoroutine(FadeOutAndLoad(sceneName, mode));
    }

    private IEnumerator FadeOutAndLoad(string sceneName, LoadSceneMode mode)
    {
        PlayFadeOutClientRpc();

        yield return new WaitForSeconds(transitionDuration);

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, mode);

        StartCoroutine(WaitForSceneLoad(sceneName));
    }


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


    [ClientRpc]
    private void PlayFadeOutClientRpc()
    {
        if (transitionAnimator)
            transitionAnimator.SetTrigger("FadeOut");
    }

    [ClientRpc]
    private void PlayFadeInClientRpc()
    {
        if (transitionAnimator)
            transitionAnimator.SetTrigger("FadeIn");

        StartCoroutine(FadeIdleCoroutine());
    }

    private IEnumerator FadeIdleCoroutine()
    {
        yield return new WaitForSeconds(transitionDuration);

        if (transitionAnimator)
            transitionAnimator.SetTrigger("Idle");
    }

    public void PlayLocalFadeOut()
    {
        if (transitionAnimator)
        {
            transitionAnimator.SetTrigger("FadeOut");
        }
    }

    public void PlayLocalFadeIn()
    {
        if (transitionAnimator)
        {
            transitionAnimator.SetTrigger("FadeIn");
            StartCoroutine(FadeIdleCoroutine());
        }
    }
}
