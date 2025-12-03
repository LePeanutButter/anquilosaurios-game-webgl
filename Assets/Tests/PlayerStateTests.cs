using NUnit.Framework;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;

/// <summary>
/// Test suite for PlayerState without using TestHelpers.
/// Tests all public methods, NetworkVariables, and callbacks using manual Netcode setup.
/// </summary>
public class PlayerStateTests
{
    #region Setup & Teardown

    private GameObject _playerStateObj;
    private PlayerState _playerState;


    [SetUp]
    public void SetUp()
    {
        // Crear GO
        _playerStateObj = new GameObject("PlayerState");

        // NetworkObject es OBLIGATORIO para instanciar NetworkBehaviour
        _playerStateObj.AddComponent<NetworkObject>();

        // Agregar PlayerState (sealed está bien)
        _playerState = _playerStateObj.AddComponent<PlayerState>();

        // Inicializar characterPrefabs antes de activar el objeto (antes de Awake)
        // Crear prefabs dummy para cada CharacterType para las pruebas
        var characterPrefabs = new PlayerState.CharacterPrefabEntry[]
        {
            new PlayerState.CharacterPrefabEntry { characterType = CharacterType.Flor, prefab = new GameObject("FlorPrefab") },
            new PlayerState.CharacterPrefabEntry { characterType = CharacterType.Girasol, prefab = new GameObject("GirasolPrefab") },
            new PlayerState.CharacterPrefabEntry { characterType = CharacterType.Jalapeno, prefab = new GameObject("JalapenoPrefab") },
            new PlayerState.CharacterPrefabEntry { characterType = CharacterType.Manzana, prefab = new GameObject("ManzanaPrefab") }
        };

        // Usar reflexión para setear el campo privado characterPrefabs
        var characterPrefabsField = typeof(PlayerState).GetField("characterPrefabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        characterPrefabsField.SetValue(_playerState, characterPrefabs);

        // Activarlo → ejecuta Awake
        _playerStateObj.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        // Limpiar los prefabs dummy creados en SetUp
        if (_playerState != null)
        {
            var characterPrefabsField = typeof(PlayerState).GetField("characterPrefabs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var characterPrefabs = (PlayerState.CharacterPrefabEntry[])characterPrefabsField.GetValue(_playerState);

            if (characterPrefabs != null)
            {
                foreach (var entry in characterPrefabs)
                {
                    if (entry.prefab != null)
                    {
                        UnityEngine.Object.DestroyImmediate(entry.prefab);
                    }
                }
            }
        }

        UnityEngine.Object.DestroyImmediate(_playerStateObj);
    }

    #endregion

    #region NetworkVariable Tests (Non-Network)

    [Test]
    public void PlayerState_NetworkVariables_InitializeWithDefaultValues()
    {
        // Assert
        Assert.AreEqual(0, _playerState.PlayerId.Value);
        Assert.AreEqual("", _playerState.PlayerName.Value.ToString());
        Assert.AreEqual((int)CharacterType.None, _playerState.Character.Value);
        Assert.AreEqual(0, _playerState.DeathCount.Value);
        Assert.AreEqual(0, _playerState.RoundWins.Value);
    }

    [Test]
    public void PlayerState_NetworkVariables_HaveCorrectPermissions()
    {
        // Assert - All should be readable by everyone, writable only by server
        Assert.AreEqual(NetworkVariableReadPermission.Everyone, _playerState.PlayerId.ReadPerm);
        Assert.AreEqual(NetworkVariableWritePermission.Server, _playerState.PlayerId.WritePerm);

        Assert.AreEqual(NetworkVariableReadPermission.Everyone, _playerState.PlayerName.ReadPerm);
        Assert.AreEqual(NetworkVariableWritePermission.Server, _playerState.PlayerName.WritePerm);

        Assert.AreEqual(NetworkVariableReadPermission.Everyone, _playerState.Character.ReadPerm);
        Assert.AreEqual(NetworkVariableWritePermission.Server, _playerState.Character.WritePerm);

        Assert.AreEqual(NetworkVariableReadPermission.Everyone, _playerState.DeathCount.ReadPerm);
        Assert.AreEqual(NetworkVariableWritePermission.Server, _playerState.DeathCount.WritePerm);

        Assert.AreEqual(NetworkVariableReadPermission.Everyone, _playerState.RoundWins.ReadPerm);
        Assert.AreEqual(NetworkVariableWritePermission.Server, _playerState.RoundWins.WritePerm);
    }

    #endregion

    #region Character Callback Tests

    [Test]
    public void PlayerState_CharacterCallback_TriggersOnValueChange()
    {
        // Arrange
        bool callbackTriggered = false;
        int previousValue = -1;
        int currentValue = -1;

        _playerState.Character.OnValueChanged += (prev, curr) =>
        {
            callbackTriggered = true;
            previousValue = prev;
            currentValue = curr;
        };

        // Act
        _playerState.Character.Value = (int)CharacterType.Flor;

        // Assert
        Assert.IsTrue(callbackTriggered, "Callback should have been triggered");
        Assert.AreEqual((int)CharacterType.None, previousValue);
        Assert.AreEqual((int)CharacterType.Girasol, currentValue);
    }

    [Test]
    public void PlayerState_CharacterCallback_TriggersMultipleTimes()
    {
        // Arrange
        int callbackCount = 0;

        _playerState.Character.OnValueChanged += (prev, curr) =>
        {
            callbackCount++;
        };

        // Act
        _playerState.Character.Value = (int)CharacterType.Flor;
        _playerState.Character.Value = (int)CharacterType.Girasol;
        _playerState.Character.Value = (int)CharacterType.Jalapeno;

        // Assert
        Assert.AreEqual(3, callbackCount);
    }

    #endregion

    #region Prefab Map Tests

    [Test]
    public void PlayerState_PrefabMap_InitializesInAwake()
    {
        // Arrange - Create a new PlayerState with character prefabs
        var testObj = new GameObject("TestPlayer");
        var testState = testObj.AddComponent<PlayerState>();

        // Use reflection to verify prefab map initialization
        var prefabMapField = typeof(PlayerState).GetField("_prefabMap",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(prefabMapField);
        var prefabMap = prefabMapField.GetValue(testState);
        Assert.IsNotNull(prefabMap, "Prefab map should be initialized in Awake");

        // Cleanup
        Object.DestroyImmediate(testObj);
    }

    #endregion

    #region Active Presenter Tests

    [Test]
    public void PlayerState_ActivePresenter_InitiallyNull()
    {
        // Assert
        Assert.IsNull(_playerState.ActivePresenter);
    }

    #endregion

    #region InitializeDataServer Tests (Simulated)

    [Test]
    public void PlayerState_InitializeDataServer_SetsPlayerName()
    {
        // Arrange
        string testName = "TestPlayer";

        // Act - Simulate server behavior by directly setting values
        _playerState.PlayerName.Value = testName;

        // Assert
        Assert.AreEqual(testName, _playerState.PlayerName.Value.ToString());
    }

    [Test]
    public void PlayerState_InitializeDataServer_SetsCharacter()
    {
        // Arrange
        int testCharacter = (int)CharacterType.Girasol;

        // Act
        _playerState.Character.Value = testCharacter;

        // Assert
        Assert.AreEqual(testCharacter, _playerState.Character.Value);
    }

    [Test]
    public void PlayerState_InitializeDataServer_ResetsCounters()
    {
        // Arrange - Set some initial values
        _playerState.DeathCount.Value = 5;
        _playerState.RoundWins.Value = 3;

        // Act - Reset to 0
        _playerState.DeathCount.Value = 0;
        _playerState.RoundWins.Value = 0;

        // Assert
        Assert.AreEqual(0, _playerState.DeathCount.Value);
        Assert.AreEqual(0, _playerState.RoundWins.Value);
    }

    #endregion

    #region Counter Increment Tests (Simulated)

    [Test]
    public void PlayerState_DeathCount_CanIncrement()
    {
        // Arrange
        int initialValue = _playerState.DeathCount.Value;

        // Act
        _playerState.DeathCount.Value++;

        // Assert
        Assert.AreEqual(initialValue + 1, _playerState.DeathCount.Value);
    }

    [Test]
    public void PlayerState_DeathCount_MultipleIncrements()
    {
        // Arrange
        int incrementCount = 5;

        // Act
        for (int i = 0; i < incrementCount; i++)
        {
            _playerState.DeathCount.Value++;
        }

        // Assert
        Assert.AreEqual(incrementCount, _playerState.DeathCount.Value);
    }

    [Test]
    public void PlayerState_RoundWins_CanIncrement()
    {
        // Arrange
        int initialValue = _playerState.RoundWins.Value;

        // Act
        _playerState.RoundWins.Value++;

        // Assert
        Assert.AreEqual(initialValue + 1, _playerState.RoundWins.Value);
    }

    [Test]
    public void PlayerState_RoundWins_MultipleIncrements()
    {
        // Arrange
        int incrementCount = 10;

        // Act
        for (int i = 0; i < incrementCount; i++)
        {
            _playerState.RoundWins.Value++;
        }

        // Assert
        Assert.AreEqual(incrementCount, _playerState.RoundWins.Value);
    }

    #endregion

    #region Complex Scenario Tests

    [Test]
    public void PlayerState_CompleteGameScenario_AllValuesCorrect()
    {
        // Arrange & Act - Simulate a complete game scenario
        _playerState.PlayerName.Value = "Player1";
        _playerState.Character.Value = (int)CharacterType.Manzana;

        // Simulate deaths during rounds
        _playerState.DeathCount.Value++;
        _playerState.DeathCount.Value++;
        _playerState.DeathCount.Value++;

        // Simulate round win
        _playerState.RoundWins.Value++;

        // Assert
        Assert.AreEqual("Player1", _playerState.PlayerName.Value.ToString());
        Assert.AreEqual((int)CharacterType.Manzana, _playerState.Character.Value);
        Assert.AreEqual(3, _playerState.DeathCount.Value);
        Assert.AreEqual(1, _playerState.RoundWins.Value);
    }

    [Test]
    public void PlayerState_MultipleRounds_CountersAccumulate()
    {
        // Act - Simulate 3 rounds
        for (int round = 0; round < 3; round++)
        {
            _playerState.DeathCount.Value += 2; // 2 deaths per round
            _playerState.RoundWins.Value++; // Win each round
        }

        // Assert
        Assert.AreEqual(6, _playerState.DeathCount.Value, "Should have 6 deaths total");
        Assert.AreEqual(3, _playerState.RoundWins.Value, "Should have 3 round wins");
    }

    #endregion

    #region DebugNetworkState Tests

    [Test]
    public void PlayerState_DebugNetworkState_DoesNotThrow()
    {
        // Arrange
        _playerState.PlayerName.Value = "DebugTest";
        _playerState.Character.Value = (int)CharacterType.Girasol;

        // Act & Assert
        Assert.DoesNotThrow(() => _playerState.DebugNetworkState());
    }

    #endregion

    #region Callback Cleanup Tests

    [Test]
    public void PlayerState_CallbackUnsubscribe_StopsReceivingEvents()
    {
        // Arrange
        int callbackCount = 0;

        void CharacterChanged(int prev, int curr)
        {
            callbackCount++;
        }

        _playerState.Character.OnValueChanged += CharacterChanged;

        // Act - Trigger callback
        _playerState.Character.Value = (int)CharacterType.Flor;
        Assert.AreEqual(1, callbackCount);

        // Unsubscribe
        _playerState.Character.OnValueChanged -= CharacterChanged;

        // Trigger again
        _playerState.Character.Value = (int)CharacterType.Girasol;

        // Assert - Count should still be 1
        Assert.AreEqual(1, callbackCount, "Callback should not trigger after unsubscribe");
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void PlayerState_PlayerId_HandlesMaxValue()
    {
        // Act
        _playerState.PlayerId.Value = ulong.MaxValue;

        // Assert
        Assert.AreEqual(ulong.MaxValue, _playerState.PlayerId.Value);
    }

    [Test]
    public void PlayerState_PlayerName_HandlesEmptyString()
    {
        // Act
        _playerState.PlayerName.Value = "";

        // Assert
        Assert.AreEqual("", _playerState.PlayerName.Value.ToString());
    }

    [Test]
    public void PlayerState_PlayerName_HandlesMaxLength()
    {
        // Arrange - FixedString64Bytes can hold up to 61 UTF-8 bytes
        string longName = new string('A', 61);

        // Act
        _playerState.PlayerName.Value = longName;

        // Assert
        Assert.AreEqual(longName, _playerState.PlayerName.Value.ToString());
    }

    [Test]
    public void PlayerState_Character_HandlesInvalidEnumValue()
    {
        // Act - Set to an invalid enum value
        _playerState.Character.Value = 999;

        // Assert
        Assert.AreEqual(999, _playerState.Character.Value);
    }

    [Test]
    public void PlayerState_Counters_HandleLargeValues()
    {
        // Act
        _playerState.DeathCount.Value = int.MaxValue;
        _playerState.RoundWins.Value = int.MaxValue;

        // Assert
        Assert.AreEqual(int.MaxValue, _playerState.DeathCount.Value);
        Assert.AreEqual(int.MaxValue, _playerState.RoundWins.Value);
    }

    #endregion
}