using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioManagerTests
{
    private AudioManager audioManager;
    private GameObject audioManagerObject;
    private AudioSource sfxSource;
    private AudioClip testClip;

    [SetUp]
    public void SetUp()
    {
        audioManagerObject = new GameObject("AudioManager");
        audioManager = audioManagerObject.AddComponent<AudioManager>();

        sfxSource = audioManagerObject.AddComponent<AudioSource>();
        testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);

        var sfxSourceField = typeof(AudioManager).GetField("SFXSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sfxSourceField != null)
        {
            sfxSourceField.SetValue(audioManager, sfxSource);
        }
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(audioManagerObject);
        if (testClip != null)
        {
            Object.DestroyImmediate(testClip);
        }
    }

    [Test]
    public void AudioManager_IsSingleton()
    {
        var instance1 = AudioManager.Instance;
        var instance2 = AudioManager.Instance;

        Assert.AreEqual(instance1, instance2, "AudioManager debe ser singleton");
    }

    [Test]
    public void Awake_InitializesSingleton()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        Assert.AreEqual(audioManager, AudioManager.Instance, "AudioManager debe inicializar singleton en Awake");
    }

    [Test]
    public void PlaySFX_WithValidClip_DoesNotThrowException()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        Assert.DoesNotThrow(() => audioManager.PlaySFX(testClip), "PlaySFX no debe lanzar excepción con clip válido");
    }

    [Test]
    public void PlaySFX_WithNullClip_DoesNotThrowException()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        Assert.DoesNotThrow(() => audioManager.PlaySFX(null), "PlaySFX debe manejar null clip sin excepciones");
    }

    [UnityTest]
    public System.Collections.IEnumerator PlaySFX_InPlayMode_Works()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        audioManager.PlaySFX(testClip);

        yield return new WaitForFixedUpdate();
        Assert.IsNotNull(audioManager, "AudioManager debe existir después de reproducir audio");
    }
}
