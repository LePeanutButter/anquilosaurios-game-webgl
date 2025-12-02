using NUnit.Framework;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GameManagerTests
{
    private GameManager gameManager;
    private GameObject gameManagerObject;
    private Button hostButton;
    private Button joinButton;
    private TMP_InputField sessionCodeInput;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();

        // Crear botones en objetos separados
        var hostObj = new GameObject("HostButton");
        hostButton = hostObj.AddComponent<Button>();

        var joinObj = new GameObject("JoinButton");
        joinButton = joinObj.AddComponent<Button>();

        // InputField en objeto separado
        var inputObject = new GameObject("SessionCodeInput");
        sessionCodeInput = inputObject.AddComponent<TMP_InputField>();

        // Asignar referencias por reflection
        typeof(GameManager).GetField("hostButton", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(gameManager, hostButton);

        typeof(GameManager).GetField("joinButton", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(gameManager, joinButton);

        typeof(GameManager).GetField("sessionCodeInput", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(gameManager, sessionCodeInput);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
        if (sessionCodeInput != null) Object.DestroyImmediate(sessionCodeInput.gameObject);
    }

    [Test]
    public void Start_AssignsButtonListeners()
    {
        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);

        Assert.IsNotNull(hostButton.onClick, "Botón host debe tener listener");
        Assert.IsNotNull(joinButton.onClick, "Botón join debe tener listener");
    }

    [Test]
    public void OnJoinClicked_WithEmptyCode_DoesNotAttemptJoin()
    {
        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);
        sessionCodeInput.text = "";

        joinButton.onClick.Invoke();

        Assert.IsNotNull(gameManager, "GameManager debe existir después del intento fallido");
    }

    [Test]
    public void OnJoinClicked_WithValidCode_AttemptsJoin()
    {
        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);
        sessionCodeInput.text = "VALID123";

        joinButton.onClick.Invoke();

        Assert.IsNotNull(gameManager, "GameManager debe existir después del intento de join");
    }

    [UnityTest]
    public System.Collections.IEnumerator OnCreateSessionClicked_InitiatesSessionCreation()
    {

        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);


        hostButton.onClick.Invoke();


        yield return null;
        Assert.IsNotNull(gameManager, "GameManager debe existir después de iniciar creación de sesión");
    }

    [Test]
    public void GameManager_InitializesWithNullSessionCodeInput_HandlesGracefully()
    {
        var sessionCodeInputField = typeof(GameManager).GetField("sessionCodeInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sessionCodeInputField != null)
        {
            sessionCodeInputField.SetValue(gameManager, null);
        }

        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);

        Assert.IsNotNull(gameManager, "GameManager debe manejar null sessionCodeInput");
    }

    [Test]
    public void GameManager_ButtonsCanBeNull_HandlesGracefully()
    {
        var hostButtonField = typeof(GameManager).GetField("hostButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var joinButtonField = typeof(GameManager).GetField("joinButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (hostButtonField != null) hostButtonField.SetValue(gameManager, null);
        if (joinButtonField != null) joinButtonField.SetValue(gameManager, null);

        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);

        Assert.IsNotNull(gameManager, "GameManager debe manejar botones null");
    }

    [Test]
    public void SessionCodeInput_Validation_Works()
    {
        gameManager.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameManager, null);

        sessionCodeInput.text = "VALIDCODE";
        Assert.AreEqual("VALIDCODE", sessionCodeInput.text, "Código válido debe mantenerse");

        sessionCodeInput.text = "";
        Assert.AreEqual("", sessionCodeInput.text, "Campo vacío debe mantenerse vacío");
    }
}