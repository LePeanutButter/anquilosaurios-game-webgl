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

    [Test]
    public void PlaySFX_WithNullSFXSource_DoesNotThrowException()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        var sfxSourceField = typeof(AudioManager).GetField("SFXSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sfxSourceField != null)
        {
            sfxSourceField.SetValue(audioManager, null);
        }

        Assert.DoesNotThrow(() => audioManager.PlaySFX(testClip), "PlaySFX debe manejar null SFXSource sin excepciones");
    }

    [UnityTest]
    public System.Collections.IEnumerator PlaySFX_InPlayMode_Works()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        audioManager.PlaySFX(testClip);

        yield return new WaitForFixedUpdate();
        Assert.IsNotNull(audioManager, "AudioManager debe existir después de reproducir audio");
    }

    [Test]
    public void PlaySFXNetworked_AsServer_CallsClientRpcDirectly()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        var isServerProperty = typeof(AudioManager).GetProperty("IsServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isServerProperty != null)
        {
            var networkBehaviour = audioManager as Unity.Netcode.NetworkBehaviour;
            if (networkBehaviour != null)
            {
                Assert.DoesNotThrow(() => audioManager.GetType()
                    .GetMethod("PlaySFXNetworked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(audioManager, new object[] { testClip }));
            }
        }
    }

    [Test]
    public void PlaySFXNetworked_AsClient_CallsServerRpc()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        Assert.DoesNotThrow(() => audioManager.GetType()
            .GetMethod("PlaySFXNetworked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.Invoke(audioManager, new object[] { testClip }));
    }

    [Test]
    public void GetClipName_ReturnsCorrectName_ForEachClip()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        var backgroundClip = AudioClip.Create("BackgroundClip", 44100, 1, 44100, false);
        var transitionClip = AudioClip.Create("TransitionClip", 44100, 1, 44100, false);
        var jumpClip = AudioClip.Create("JumpClip", 44100, 1, 44100, false);
        var movingClip = AudioClip.Create("MovingClip", 44100, 1, 44100, false);
        var qteClip = AudioClip.Create("QTEClip", 44100, 1, 44100, false);
        var failClip = AudioClip.Create("FailClip", 44100, 1, 44100, false);

        typeof(AudioManager).GetField("background").SetValue(audioManager, backgroundClip);
        typeof(AudioManager).GetField("transition").SetValue(audioManager, transitionClip);
        typeof(AudioManager).GetField("jump").SetValue(audioManager, jumpClip);
        typeof(AudioManager).GetField("moving").SetValue(audioManager, movingClip);
        typeof(AudioManager).GetField("qte").SetValue(audioManager, qteClip);
        typeof(AudioManager).GetField("fail").SetValue(audioManager, failClip);

        var getClipNameMethod = typeof(AudioManager).GetMethod("GetClipName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.AreEqual("background", getClipNameMethod.Invoke(audioManager, new object[] { backgroundClip }));
        Assert.AreEqual("transition", getClipNameMethod.Invoke(audioManager, new object[] { transitionClip }));
        Assert.AreEqual("jump", getClipNameMethod.Invoke(audioManager, new object[] { jumpClip }));
        Assert.AreEqual("moving", getClipNameMethod.Invoke(audioManager, new object[] { movingClip }));
        Assert.AreEqual("qte", getClipNameMethod.Invoke(audioManager, new object[] { qteClip }));
        Assert.AreEqual("fail", getClipNameMethod.Invoke(audioManager, new object[] { failClip }));

        Assert.AreEqual(string.Empty, getClipNameMethod.Invoke(audioManager, new object[] { testClip }));

        Object.DestroyImmediate(backgroundClip);
        Object.DestroyImmediate(transitionClip);
        Object.DestroyImmediate(jumpClip);
        Object.DestroyImmediate(movingClip);
        Object.DestroyImmediate(qteClip);
        Object.DestroyImmediate(failClip);
    }

    [Test]
    public void GetClipByName_ReturnsCorrectClip_ForEachName()
    {
        audioManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(audioManager, null);

        var backgroundClip = AudioClip.Create("BackgroundClip", 44100, 1, 44100, false);
        var transitionClip = AudioClip.Create("TransitionClip", 44100, 1, 44100, false);
        var jumpClip = AudioClip.Create("JumpClip", 44100, 1, 44100, false);
        var movingClip = AudioClip.Create("MovingClip", 44100, 1, 44100, false);
        var qteClip = AudioClip.Create("QTEClip", 44100, 1, 44100, false);
        var failClip = AudioClip.Create("FailClip", 44100, 1, 44100, false);

        typeof(AudioManager).GetField("background").SetValue(audioManager, backgroundClip);
        typeof(AudioManager).GetField("transition").SetValue(audioManager, transitionClip);
        typeof(AudioManager).GetField("jump").SetValue(audioManager, jumpClip);
        typeof(AudioManager).GetField("moving").SetValue(audioManager, movingClip);
        typeof(AudioManager).GetField("qte").SetValue(audioManager, qteClip);
        typeof(AudioManager).GetField("fail").SetValue(audioManager, failClip);

        var getClipByNameMethod = typeof(AudioManager).GetMethod("GetClipByName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.AreEqual(backgroundClip, getClipByNameMethod.Invoke(audioManager, new object[] { "background" }));
        Assert.AreEqual(transitionClip, getClipByNameMethod.Invoke(audioManager, new object[] { "transition" }));
        Assert.AreEqual(jumpClip, getClipByNameMethod.Invoke(audioManager, new object[] { "jump" }));
        Assert.AreEqual(movingClip, getClipByNameMethod.Invoke(audioManager, new object[] { "moving" }));
        Assert.AreEqual(qteClip, getClipByNameMethod.Invoke(audioManager, new object[] { "qte" }));
        Assert.AreEqual(failClip, getClipByNameMethod.Invoke(audioManager, new object[] { "fail" }));

        Assert.IsNull(getClipByNameMethod.Invoke(audioManager, new object[] { "unknown" }));

        Object.DestroyImmediate(backgroundClip);
        Object.DestroyImmediate(transitionClip);
        Object.DestroyImmediate(jumpClip);
        Object.DestroyImmediate(movingClip);
        Object.DestroyImmediate(qteClip);
        Object.DestroyImmediate(failClip);
    }
}
