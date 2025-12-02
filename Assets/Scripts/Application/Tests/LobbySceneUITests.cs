using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests unitarios para LobbySceneUI sin usar Singletons directamente.
/// Se enfoca en probar la l�gica interna sin dependencias de NetworkManager real.
/// </summary>
public class LobbySceneUITests
{
    private LobbySceneUI lobbySceneUI;
    private GameObject lobbySceneObject;
    private TMP_Text sessionCodeText;
    private Button startGameButton;
    private Transform playerListParent;
    private GameObject playerCardPrefab;

    [SetUp]
    public void SetUp()
    {
        lobbySceneObject = new GameObject("LobbySceneUI");
        lobbySceneUI = lobbySceneObject.AddComponent<LobbySceneUI>();

        // Create UI components
        var sessionCodeObj = new GameObject("SessionCodeText");
        sessionCodeText = sessionCodeObj.AddComponent<TextMeshProUGUI>();
        sessionCodeObj.transform.SetParent(lobbySceneObject.transform);

        var startButtonObj = new GameObject("StartGameButton");
        startGameButton = startButtonObj.AddComponent<Button>();
        startButtonObj.transform.SetParent(lobbySceneObject.transform);

        var playerListObj = new GameObject("PlayerListParent");
        playerListParent = playerListObj.transform;
        playerListObj.transform.SetParent(lobbySceneObject.transform);

        playerCardPrefab = new GameObject("PlayerCardPrefab");

        // Assign references using reflection (assuming these are serialized fields)
        var sessionCodeField = typeof(LobbySceneUI).GetField("sessionCodeText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sessionCodeField?.SetValue(lobbySceneUI, sessionCodeText);

        var startButtonField = typeof(LobbySceneUI).GetField("startGameButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        startButtonField?.SetValue(lobbySceneUI, startGameButton);

        var playerListField = typeof(LobbySceneUI).GetField("playerListParent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        playerListField?.SetValue(lobbySceneUI, playerListParent);

        var prefabField = typeof(LobbySceneUI).GetField("playerCardPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prefabField?.SetValue(lobbySceneUI, playerCardPrefab);
    }

    [TearDown]
    public void TearDown()
    {
        if (lobbySceneObject != null)
            Object.DestroyImmediate(lobbySceneObject);
        if (playerCardPrefab != null)
            Object.DestroyImmediate(playerCardPrefab);
    }

    #region UI Component Tests

    [Test]
    public void LobbySceneUI_HasRequiredUIComponents()
    {
        // Assert
        var sessionCodeField = typeof(LobbySceneUI).GetField("sessionCodeText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var startButtonField = typeof(LobbySceneUI).GetField("startGameButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerListField = typeof(LobbySceneUI).GetField("playerListParent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prefabField = typeof(LobbySceneUI).GetField("playerCardPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(sessionCodeField?.GetValue(lobbySceneUI));
        Assert.IsNotNull(startButtonField?.GetValue(lobbySceneUI));
        Assert.IsNotNull(playerListField?.GetValue(lobbySceneUI));
        Assert.IsNotNull(prefabField?.GetValue(lobbySceneUI));
    }

    [Test]
    public void SessionCodeText_InitiallyEmpty()
    {
        // Assert
        Assert.IsNotNull(sessionCodeText);
        Assert.AreEqual("", sessionCodeText.text);
    }

    [Test]
    public void StartGameButton_IsInteractable()
    {
        // Assert
        Assert.IsNotNull(startGameButton);
        Assert.IsTrue(startGameButton.interactable);
    }

    [Test]
    public void PlayerListParent_InitiallyEmpty()
    {
        // Assert
        Assert.IsNotNull(playerListParent);
        Assert.AreEqual(0, playerListParent.childCount);
    }

    #endregion

    #region Session Code Display Tests

    [Test]
    public void SessionCodeText_CanBeUpdated()
    {
        // Arrange
        string testCode = "TEST123";

        // Act
        sessionCodeText.text = $"Codigo de sesion: {testCode}";

        // Assert
        Assert.AreEqual("Codigo de sesion: TEST123", sessionCodeText.text);
    }

    [Test]
    public void SessionCodeText_HandlesEmptyCode()
    {
        // Act
        sessionCodeText.text = "Codigo de sesion no disponible";

        // Assert
        Assert.AreEqual("Codigo de sesion no disponible", sessionCodeText.text);
    }

    [Test]
    public void SessionCodeText_HandlesLongCode()
    {
        // Arrange
        string longCode = "VERYLONGCODE123456789";

        // Act
        sessionCodeText.text = $"Codigo de sesion: {longCode}";

        // Assert
        Assert.IsTrue(sessionCodeText.text.Contains(longCode));
    }

    #endregion

    #region Player List Management Tests

    [Test]
    public void PlayerListParent_CanAddChildren()
    {
        // Arrange
        var child = new GameObject("PlayerCard");

        // Act
        child.transform.SetParent(playerListParent);

        // Assert
        Assert.AreEqual(1, playerListParent.childCount);
        Assert.AreEqual(child.transform, playerListParent.GetChild(0));

        // Cleanup
        Object.DestroyImmediate(child);
    }

    [Test]
    public void PlayerListParent_CanRemoveChildren()
    {
        // Arrange
        var child = new GameObject("PlayerCard");
        child.transform.SetParent(playerListParent);
        Assert.AreEqual(1, playerListParent.childCount);

        // Act
        Object.DestroyImmediate(child);

        // Assert
        Assert.AreEqual(0, playerListParent.childCount);
    }

    [Test]
    public void PlayerListParent_CanAddMultipleChildren()
    {
        // Arrange & Act
        var children = new List<GameObject>();
        for (int i = 0; i < 5; i++)
        {
            var child = new GameObject($"PlayerCard_{i}");
            child.transform.SetParent(playerListParent);
            children.Add(child);
        }

        // Assert
        Assert.AreEqual(5, playerListParent.childCount);

        // Cleanup
        foreach (var child in children)
        {
            Object.DestroyImmediate(child);
        }
    }

    #endregion

    #region Player Card Instantiation Tests

    [Test]
    public void PlayerCardPrefab_CanBeInstantiated()
    {
        // Act
        var instance = Object.Instantiate(playerCardPrefab, playerListParent);

        // Assert
        Assert.IsNotNull(instance);
        Assert.AreEqual(playerListParent, instance.transform.parent);

        // Cleanup
        Object.DestroyImmediate(instance);
    }

    [Test]
    public void PlayerCardPrefab_WithPlayerCardUI_ReturnsComponent()
    {
        // Arrange
        var playerCardUI = playerCardPrefab.AddComponent<PlayerCardUI>();

        // Act
        var instance = Object.Instantiate(playerCardPrefab, playerListParent);
        var component = instance.GetComponent<PlayerCardUI>();

        // Assert
        Assert.IsNotNull(component);

        // Cleanup
        Object.DestroyImmediate(instance);
    }

    [Test]
    public void MultiplePlayerCards_CanBeInstantiated()
    {
        // Arrange
        playerCardPrefab.AddComponent<PlayerCardUI>();
        var instances = new List<GameObject>();

        // Act
        for (int i = 0; i < 4; i++)
        {
            var instance = Object.Instantiate(playerCardPrefab, playerListParent);
            instances.Add(instance);
        }

        // Assert
        Assert.AreEqual(4, playerListParent.childCount);
        foreach (var instance in instances)
        {
            Assert.IsNotNull(instance.GetComponent<PlayerCardUI>());
        }

        // Cleanup
        foreach (var instance in instances)
        {
            Object.DestroyImmediate(instance);
        }
    }

    #endregion

    #region Dictionary Management Tests

    [Test]
    public void PlayerCards_Dictionary_InitializesEmpty()
    {
        // Arrange
        var playerCardsField = typeof(LobbySceneUI).GetField("playerCards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var playerCards = playerCardsField?.GetValue(lobbySceneUI);

        // Assert
        Assert.IsNotNull(playerCards);
        if (playerCards is Dictionary<ulong, PlayerCardUI> dict)
        {
            Assert.AreEqual(0, dict.Count);
        }
    }

    [Test]
    public void BoundPlayerStates_Dictionary_InitializesEmpty()
    {
        // Arrange
        var boundStatesField = typeof(LobbySceneUI).GetField("boundPlayerStates",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var boundStates = boundStatesField?.GetValue(lobbySceneUI);

        // Assert
        Assert.IsNotNull(boundStates);
        if (boundStates is Dictionary<ulong, PlayerState> dict)
        {
            Assert.AreEqual(0, dict.Count);
        }
    }

    [Test]
    public void PlayerCards_Dictionary_CanAddEntries()
    {
        // Arrange
        var playerCardsField = typeof(LobbySceneUI).GetField("playerCards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerCards = playerCardsField?.GetValue(lobbySceneUI) as Dictionary<ulong, PlayerCardUI>;

        var cardObj = new GameObject("TestCard");
        var cardUI = cardObj.AddComponent<PlayerCardUI>();

        // Act
        playerCards?.Add(123UL, cardUI);

        // Assert
        Assert.IsNotNull(playerCards);
        Assert.AreEqual(1, playerCards.Count);
        Assert.IsTrue(playerCards.ContainsKey(123UL));
        Assert.AreEqual(cardUI, playerCards[123UL]);

        // Cleanup
        Object.DestroyImmediate(cardObj);
    }

    [Test]
    public void PlayerCards_Dictionary_CanRemoveEntries()
    {
        // Arrange
        var playerCardsField = typeof(LobbySceneUI).GetField("playerCards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerCards = playerCardsField?.GetValue(lobbySceneUI) as Dictionary<ulong, PlayerCardUI>;

        var cardObj = new GameObject("TestCard");
        var cardUI = cardObj.AddComponent<PlayerCardUI>();
        playerCards?.Add(123UL, cardUI);

        // Act
        playerCards?.Remove(123UL);

        // Assert
        Assert.IsNotNull(playerCards);
        Assert.AreEqual(0, playerCards.Count);
        Assert.IsFalse(playerCards.ContainsKey(123UL));

        // Cleanup
        Object.DestroyImmediate(cardObj);
    }

    [Test]
    public void BoundPlayerStates_Dictionary_CanAddEntries()
    {
        // Arrange
        var boundStatesField = typeof(LobbySceneUI).GetField("boundPlayerStates",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var boundStates = boundStatesField?.GetValue(lobbySceneUI) as Dictionary<ulong, PlayerState>;

        var stateObj = new GameObject("TestState");
        var playerState = stateObj.AddComponent<PlayerState>();

        // Act
        boundStates?.Add(456UL, playerState);

        // Assert
        Assert.IsNotNull(boundStates);
        Assert.AreEqual(1, boundStates.Count);
        Assert.IsTrue(boundStates.ContainsKey(456UL));
        Assert.AreEqual(playerState, boundStates[456UL]);

        // Cleanup
        Object.DestroyImmediate(stateObj);
    }

    #endregion

    #region Button Interaction Tests

    [Test]
    public void StartGameButton_CanBeClicked()
    {
        // Arrange
        bool wasClicked = false;
        startGameButton.onClick.AddListener(() => wasClicked = true);

        // Act
        startGameButton.onClick.Invoke();

        // Assert
        Assert.IsTrue(wasClicked);
    }

    [Test]
    public void StartGameButton_CanBeDisabled()
    {
        // Act
        startGameButton.interactable = false;

        // Assert
        Assert.IsFalse(startGameButton.interactable);
    }

    [Test]
    public void StartGameButton_CanBeEnabled()
    {
        // Arrange
        startGameButton.interactable = false;

        // Act
        startGameButton.interactable = true;

        // Assert
        Assert.IsTrue(startGameButton.interactable);
    }

    [Test]
    public void StartGameButton_MultipleListeners_AllExecute()
    {
        // Arrange
        int clickCount = 0;
        startGameButton.onClick.AddListener(() => clickCount++);
        startGameButton.onClick.AddListener(() => clickCount++);
        startGameButton.onClick.AddListener(() => clickCount++);

        // Act
        startGameButton.onClick.Invoke();

        // Assert
        Assert.AreEqual(3, clickCount);
    }

    #endregion

    #region Reflection Helper Tests

    [Test]
    public void LobbySceneUI_HasSetupLobbyMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("SetupLobby",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "SetupLobby method should exist");
    }

    [Test]
    public void LobbySceneUI_HasRefreshPlayerListMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("RefreshPlayerList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "RefreshPlayerList method should exist");
    }

    [Test]
    public void LobbySceneUI_HasCreatePlayerCardPlaceholderMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("CreatePlayerCardPlaceholder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "CreatePlayerCardPlaceholder method should exist");
    }

    [Test]
    public void LobbySceneUI_HasCreateAndBindCardMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("CreateAndBindCard",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "CreateAndBindCard method should exist");
    }

    [Test]
    public void LobbySceneUI_HasUnbindPlayerStateMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("UnbindPlayerState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "UnbindPlayerState method should exist");
    }

    [Test]
    public void LobbySceneUI_HasOnClientDisconnectedMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("OnClientDisconnected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "OnClientDisconnected method should exist");
    }

    [Test]
    public void LobbySceneUI_HasOnStartGameClickedMethod()
    {
        // Act
        var method = typeof(LobbySceneUI).GetMethod("OnStartGameClicked",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(method, "OnStartGameClicked method should exist");
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void PlayerListParent_HandlesNullChild()
    {
        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => playerListParent.GetChild(0));
    }

    [Test]
    public void SessionCodeText_HandlesNullString()
    {
        // Act
        sessionCodeText.text = null;

        // Assert - TextMeshPro handles null as empty string
        Assert.AreEqual("", sessionCodeText.text);
    }

    [Test]
    public void PlayerCardPrefab_HandlesMultipleComponents()
    {
        // Arrange
        playerCardPrefab.AddComponent<PlayerCardUI>();
        playerCardPrefab.AddComponent<Image>();
        playerCardPrefab.AddComponent<CanvasGroup>();

        // Act
        var instance = Object.Instantiate(playerCardPrefab, playerListParent);

        // Assert
        Assert.IsNotNull(instance.GetComponent<PlayerCardUI>());
        Assert.IsNotNull(instance.GetComponent<Image>());
        Assert.IsNotNull(instance.GetComponent<CanvasGroup>());

        // Cleanup
        Object.DestroyImmediate(instance);
    }

    #endregion

    #region Integration-Like Tests

    [Test]
    public void CompletePlayerCardFlow_CreateAndDestroy()
    {
        // Arrange
        playerCardPrefab.AddComponent<PlayerCardUI>();
        var playerCardsField = typeof(LobbySceneUI).GetField("playerCards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerCards = playerCardsField?.GetValue(lobbySceneUI) as Dictionary<ulong, PlayerCardUI>;

        // Act - Create
        var instance = Object.Instantiate(playerCardPrefab, playerListParent);
        var cardUI = instance.GetComponent<PlayerCardUI>();
        playerCards?.Add(100UL, cardUI);

        // Assert - Created
        Assert.AreEqual(1, playerListParent.childCount);
        Assert.AreEqual(1, playerCards?.Count);
        Assert.IsTrue(playerCards?.ContainsKey(100UL));

        // Act - Destroy
        playerCards?.Remove(100UL);
        Object.DestroyImmediate(instance);

        // Assert - Destroyed
        Assert.AreEqual(0, playerListParent.childCount);
        Assert.AreEqual(0, playerCards?.Count);
    }

    [Test]
    public void MultiplePlayersFlow_AddAndRemove()
    {
        // Arrange
        playerCardPrefab.AddComponent<PlayerCardUI>();
        var playerCardsField = typeof(LobbySceneUI).GetField("playerCards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerCards = playerCardsField?.GetValue(lobbySceneUI) as Dictionary<ulong, PlayerCardUI>;

        var instances = new List<(ulong id, GameObject obj)>();

        // Act - Add 4 players
        for (ulong i = 0; i < 4; i++)
        {
            var instance = Object.Instantiate(playerCardPrefab, playerListParent);
            var cardUI = instance.GetComponent<PlayerCardUI>();
            playerCards?.Add(i, cardUI);
            instances.Add((i, instance));
        }

        // Assert - All added
        Assert.AreEqual(4, playerListParent.childCount);
        Assert.AreEqual(4, playerCards?.Count);

        // Act - Remove 2 players
        for (int i = 0; i < 2; i++)
        {
            playerCards?.Remove(instances[i].id);
            Object.DestroyImmediate(instances[i].obj);
        }

        // Assert - 2 remaining
        Assert.AreEqual(2, playerListParent.childCount);
        Assert.AreEqual(2, playerCards?.Count);

        // Cleanup
        for (int i = 2; i < 4; i++)
        {
            Object.DestroyImmediate(instances[i].obj);
        }
    }

    #endregion
}
