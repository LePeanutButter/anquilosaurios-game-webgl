using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Centralized audio manager that handles playing sound effects (SFX)
/// and background music locally and across the network using Unity Netcode.
/// Supports synchronized audio playback between clients and the server.
/// </summary>
public class AudioManager : NetworkBehaviour
{
    #region Singleton Setup

    /// <summary>
    /// Global instance of the AudioManager to ensure only one exists.
    /// </summary>
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Audio Clip")]
    public AudioClip background;
    public AudioClip transition;
    public AudioClip jump;
    public AudioClip moving;
    public AudioClip qte;
    public AudioClip fail;

    /// <summary>
    /// Ensures that there is only one instance of the AudioManager
    /// and prevents it from being destroyed when loading new scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Local Audio Playback

    /// <summary>
    /// Plays a given sound effect locally on the player's machine.
    /// </summary>
    /// <param name="clip">The audio clip to play.</param>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || SFXSource == null)
            return;

        SFXSource.PlayOneShot(clip);
    }

    #endregion

    #region Networked Audio Playback

    /// <summary>
    /// Plays a sound effect across the network, synchronizing it for all connected clients.
    /// The server broadcasts the request to everyone.
    /// </summary>
    /// <param name="clip">The audio clip to play.</param>
    public void PlaySFXNetworked(AudioClip clip)
    {
        if (!IsServer)
        {
            PlaySFXRequestServerRpc(GetClipName(clip));
            return;
        }

        PlaySFXClientRpc(GetClipName(clip));
        PlaySFX(clip);
    }

    /// <summary>
    /// Receives a request from a client to play a specific sound effect across all clients.
    /// This method runs on the server.
    /// </summary>
    /// <param name="clipName">The name of the clip to play.</param>
    [ServerRpc(RequireOwnership = false)]
    private void PlaySFXRequestServerRpc(string clipName)
    {
        PlaySFXClientRpc(clipName);
    }

    /// <summary>
    /// Instructs all clients to play a specific sound effect.
    /// </summary>
    /// <param name="clipName">The name of the clip to play.</param>
    [ClientRpc]
    private void PlaySFXClientRpc(string clipName)
    {
        AudioClip clip = GetClipByName(clipName);
        PlaySFX(clip);
    }

    #endregion

    #region Audio Clip Lookup Helpers

    /// <summary>
    /// Returns the name of a given AudioClip, used for networked clip identification.
    /// </summary>
    /// <param name="clip">The AudioClip to identify.</param>
    /// <returns>The string name corresponding to the clip, or an empty string if not found.</returns>
    private string GetClipName(AudioClip clip)
    {
        if (clip == background) return nameof(background);
        if (clip == transition) return nameof(transition);
        if (clip == jump) return nameof(jump);
        if (clip == moving) return nameof(moving);
        if (clip == qte) return nameof(qte);
        if (clip == fail) return nameof(fail);
        return string.Empty;
    }

    /// <summary>
    /// Retrieves an AudioClip reference by its name.
    /// Used to translate networked clip identifiers back into AudioClip objects.
    /// </summary>
    /// <param name="name">The name of the clip.</param>
    /// <returns>The corresponding AudioClip, or null if no match is found.</returns>
    private AudioClip GetClipByName(string name)
    {
        switch (name)
        {
            case nameof(background): return background;
            case nameof(transition): return transition;
            case nameof(jump): return jump;
            case nameof(moving): return moving;
            case nameof(qte): return qte;
            case nameof(fail): return fail;
            default: return null;
        }
    }

    #endregion
}
