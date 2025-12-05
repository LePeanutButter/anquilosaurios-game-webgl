using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading.Tasks;

public class GameManagerPlayModeTests
{
    private GameObject sessionManagerGO;
    private GameObject gameManagerGO;

    private GameManager gameManager;
    private Button hostButton;
    private Button joinButton;
    private TMP_InputField sessionCodeInput;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // 1. Crear el SessionManager en la escena
        sessionManagerGO = new GameObject("SessionManager");
        sessionManagerGO.AddComponent<SessionManager>(); // Awake() asignará Instance automáticamente
        yield return null; // Esperar un frame para que Awake() corra

        // 2. Crear el GameManager en la escena
        gameManagerGO = new GameObject("GameManager");
        gameManager = gameManagerGO.AddComponent<GameManager>();

        // 3. Crear botones e input
        hostButton = new GameObject("HostButton").AddComponent<Button>();
        joinButton = new GameObject("JoinButton").AddComponent<Button>();
        sessionCodeInput = new GameObject("SessionCodeInput").AddComponent<TMP_InputField>();

        // 4. Asignar referencias
        gameManager.hostButton = hostButton;
        gameManager.joinButton = joinButton;
        gameManager.sessionCodeInput = sessionCodeInput;

        // 5. Llamar Start manualmente (invoca listeners)
        gameManager.Invoke("Start", 0f);
        yield return null; // esperar un frame
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(sessionManagerGO);
        Object.DestroyImmediate(gameManagerGO);
        Object.DestroyImmediate(hostButton.gameObject);
        Object.DestroyImmediate(joinButton.gameObject);
        Object.DestroyImmediate(sessionCodeInput.gameObject);

        yield return null;
    }

    [UnityTest]
    public IEnumerator HostButtonClick_DoesNotThrow()
    {
        // Simular click
        hostButton.onClick.Invoke();
        yield return null;

        Assert.Pass("HostButton invoked without exception");
    }

    [UnityTest]
    public IEnumerator JoinButtonClick_DoesNotThrow()
    {
        sessionCodeInput.text = "TESTCODE";

        // Simular click
        joinButton.onClick.Invoke();
        yield return null;

        Assert.Pass("JoinButton invoked without exception");
    }
}

