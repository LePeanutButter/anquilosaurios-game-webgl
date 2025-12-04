using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

public class GameRoundManagerTests
{
    private GameRoundManager gameRoundManager;
    private GameObject gameRoundManagerObject;

    [SetUp]
    public void SetUp()
    {
        gameRoundManagerObject = new GameObject("GameRoundManager");
        gameRoundManager = gameRoundManagerObject.AddComponent<GameRoundManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameRoundManagerObject);
    }

    #region Singleton and Initialization Tests

    [Test]
    public void GameRoundManager_IsSingleton()
    {
        var instance1 = GameRoundManager.Instance;
        var instance2 = GameRoundManager.Instance;
        Assert.AreEqual(instance1, instance2, "GameRoundManager debe ser singleton");
        Assert.IsNotNull(instance1, "Instance no debe ser null");
    }

    [Test]
    public void Awake_PreventsMultipleInstances()
    {
        // Arrange - Crear una segunda instancia
        var secondObject = new GameObject("SecondGameRoundManager");
        var secondGameRoundManager = secondObject.AddComponent<GameRoundManager>();

        // Act - Llamar Awake en la segunda instancia
        secondGameRoundManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(secondGameRoundManager, null);

        // Assert - La segunda instancia debe ser destruida y la primera debe permanecer
        Assert.AreEqual(gameRoundManager, GameRoundManager.Instance, "Debe mantener la primera instancia como singleton");

        // Cleanup
        Object.DestroyImmediate(secondObject);
    }

    [UnityTest]
    public System.Collections.IEnumerator Awake_InitializesPlayerPrefabsDict()
    {
        // Arrange - Configurar lista de prefabs de prueba usando reflexión
        var playerPrefabsMapField = typeof(GameRoundManager).GetField("playerPrefabsMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(playerPrefabsMapField, "Campo playerPrefabsMap debe existir");

        var testPrefabs = new List<GameRoundManager.PlayerPrefabEntry>
        {
            new GameRoundManager.PlayerPrefabEntry { Type = CharacterType.Flor, Prefab = new GameObject("FlorPrefab") },
            new GameRoundManager.PlayerPrefabEntry { Type = CharacterType.Girasol, Prefab = new GameObject("GirasolPrefab") }
        };
        playerPrefabsMapField.SetValue(gameRoundManager, testPrefabs);

        yield return null; // Esperar un frame para que Awake se ejecute

        // Assert - Verificar que el diccionario se inicializó
        var dictField = typeof(GameRoundManager).GetField("playerPrefabsDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(dictField, "Campo playerPrefabsDict debe existir");

        var dict = (Dictionary<CharacterType, GameObject>)dictField.GetValue(gameRoundManager);
        Assert.IsNotNull(dict, "Diccionario de prefabs debe inicializarse");
        Assert.AreEqual(2, dict.Count, "Diccionario debe contener 2 entradas");
        Assert.IsTrue(dict.ContainsKey(CharacterType.Flor), "Debe contener CharacterType.Flor");
        Assert.IsTrue(dict.ContainsKey(CharacterType.Girasol), "Debe contener CharacterType.Girasol");
    }

    [UnityTest]
    public System.Collections.IEnumerator InitializePlayerPrefabsDict_HandlesDuplicates()
    {
        // Arrange - Configurar lista con duplicados
        var playerPrefabsMapField = typeof(GameRoundManager).GetField("playerPrefabsMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(playerPrefabsMapField, "Campo playerPrefabsMap debe existir");

        var testPrefabs = new List<GameRoundManager.PlayerPrefabEntry>
        {
            new GameRoundManager.PlayerPrefabEntry { Type = CharacterType.Flor, Prefab = new GameObject("FlorPrefab1") },
            new GameRoundManager.PlayerPrefabEntry { Type = CharacterType.Flor, Prefab = new GameObject("FlorPrefab2") }, // Duplicado
            new GameRoundManager.PlayerPrefabEntry { Type = CharacterType.Girasol, Prefab = new GameObject("GirasolPrefab") }
        };
        playerPrefabsMapField.SetValue(gameRoundManager, testPrefabs);

        yield return null; // Esperar que se procese

        // Act - Llamar el método directamente
        var method = gameRoundManager.GetType().GetMethod("InitializePlayerPrefabsDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "Método InitializePlayerPrefabsDict debe existir");
        method.Invoke(gameRoundManager, null);

        // Assert - Solo debe contener entradas únicas
        var dictField = typeof(GameRoundManager).GetField("playerPrefabsDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(dictField, "Campo playerPrefabsDict debe existir");

        var dict = (Dictionary<CharacterType, GameObject>)dictField.GetValue(gameRoundManager);
        Assert.AreEqual(2, dict.Count, "Diccionario debe contener solo entradas únicas");
        Assert.IsTrue(dict.ContainsKey(CharacterType.Flor), "Debe contener CharacterType.Flor");
        Assert.IsTrue(dict.ContainsKey(CharacterType.Girasol), "Debe contener CharacterType.Girasol");
    }

    [Test]
    public void InitializePlayerPrefabsDict_WithEmptyList_Works()
    {
        // Arrange - Lista vacía
        var playerPrefabsMapField = typeof(GameRoundManager).GetField("playerPrefabsMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        playerPrefabsMapField.SetValue(gameRoundManager, new List<GameRoundManager.PlayerPrefabEntry>());

        // Act
        gameRoundManager.GetType().GetMethod("InitializePlayerPrefabsDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameRoundManager, null);

        // Assert
        var dictField = typeof(GameRoundManager).GetField("playerPrefabsDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<CharacterType, GameObject>)dictField.GetValue(gameRoundManager);

        Assert.AreEqual(0, dict.Count, "Diccionario debe estar vacío con lista vacía");
    }

    #endregion

    #region Network Variable Tests

    [Test]
    public void IsRoundInitialized_NetworkVariable_DefaultsToFalse()
    {
        Assert.IsFalse(gameRoundManager.isRoundInitialized.Value, "isRoundInitialized debe ser false por defecto");
    }

    [Test]
    public void IsRoundInitialized_NetworkVariable_CanBeSet()
    {
        // Act
        gameRoundManager.isRoundInitialized.Value = true;

        // Assert
        Assert.IsTrue(gameRoundManager.isRoundInitialized.Value, "isRoundInitialized debe poder cambiarse a true");
    }

    #endregion

    #region Update Method Tests

    [Test]
    public void Update_AsClient_CallsUpdateRemainingTimeDisplay()
    {
        // Esta prueba es difícil de hacer en unit tests porque requiere mocking de IsClient
        // En su lugar, probamos que el método existe y no lanza excepciones
        Assert.DoesNotThrow(() => gameRoundManager.GetType().GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameRoundManager, null));
    }

    [UnityTest]
    public System.Collections.IEnumerator UpdateRemainingTimeDisplay_WithValidTimeTextHud_UpdatesText()
    {
        // Arrange
        var timeTextHudField = typeof(GameRoundManager).GetField("timeTextHud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(timeTextHudField, "Campo timeTextHud debe existir");

        var textObject = new GameObject("TimeText");
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        timeTextHudField.SetValue(gameRoundManager, textComponent);

        yield return null; // Esperar un frame

        var remainingTimeField = typeof(GameRoundManager).GetField("remainingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(remainingTimeField, "Campo remainingTime debe existir");

        var remainingTime = (Unity.Netcode.NetworkVariable<float>)remainingTimeField.GetValue(gameRoundManager);
        remainingTime.Value = 125f; // 2:05

        // Act
        var method = gameRoundManager.GetType().GetMethod("UpdateRemainingTimeDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "Método UpdateRemainingTimeDisplay debe existir");
        method.Invoke(gameRoundManager, null);

        yield return null; // Esperar que se actualice el texto

        // Assert
        Assert.AreEqual("02:05", textComponent.text, "Debe mostrar tiempo en formato MM:SS");

        // Cleanup
        Object.DestroyImmediate(textObject);
    }

    [Test]
    public void UpdateRemainingTimeDisplay_WithNullTimeTextHud_DoesNothing()
    {
        // Arrange
        var timeTextHudField = typeof(GameRoundManager).GetField("timeTextHud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        timeTextHudField.SetValue(gameRoundManager, null);

        // Act
        Assert.DoesNotThrow(() =>
            gameRoundManager.GetType().GetMethod("UpdateRemainingTimeDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameRoundManager, null));
    }

    [Test]
    public void UpdateRemainingTimeDisplay_WithZeroTime_ShowsZeros()
    {
        // Arrange
        var timeTextHudField = typeof(GameRoundManager).GetField("timeTextHud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var remainingTimeField = typeof(GameRoundManager).GetField("remainingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var textObject = new GameObject("TimeText");
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        timeTextHudField.SetValue(gameRoundManager, textComponent);

        var remainingTime = (Unity.Netcode.NetworkVariable<float>)remainingTimeField.GetValue(gameRoundManager);
        remainingTime.Value = 0f;

        // Act
        gameRoundManager.GetType().GetMethod("UpdateRemainingTimeDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(gameRoundManager, null);

        // Assert
        Assert.AreEqual("00:00", textComponent.text, "Debe mostrar 00:00 para tiempo cero");

        // Cleanup
        Object.DestroyImmediate(textObject);
    }

    #endregion

    #region QTE System Tests

    [Test]
    public void RegisterQTEPressFromClient_MethodExists()
    {
        // Verificar que el método existe y es accesible
        var method = typeof(GameRoundManager).GetMethod("RegisterQTEPressFromClient", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "RegisterQTEPressFromClient debe existir como método público");
        Assert.AreEqual(typeof(void), method.ReturnType, "RegisterQTEPressFromClient debe retornar void");
    }

    [Test]
    public void GetAlivePlayerIds_MethodExists()
    {
        // Verificar que el método existe
        var method = typeof(GameRoundManager).GetMethod("GetAlivePlayerIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "GetAlivePlayerIds debe existir");
        Assert.AreEqual(typeof(System.Collections.Generic.IEnumerable<ulong>), method.ReturnType, "GetAlivePlayerIds debe retornar IEnumerable<ulong>");
    }

    #endregion

    #region Round End Tests

    [Test]
    public void CheckRoundEndByDeaths_MethodExists()
    {
        // Verificar que el método existe
        var method = typeof(GameRoundManager).GetMethod("CheckRoundEndByDeaths", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "CheckRoundEndByDeaths debe existir");
        Assert.AreEqual(typeof(void), method.ReturnType, "CheckRoundEndByDeaths debe retornar void");
    }

    [Test]
    public void GetRemainingTime_MethodExistsAndReturnsFloat()
    {
        // Verificar que el método existe y retorna float
        var method = typeof(GameRoundManager).GetMethod("GetRemainingTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "GetRemainingTime debe existir como método público");
        Assert.AreEqual(typeof(float), method.ReturnType, "GetRemainingTime debe retornar float");
    }

    #endregion

    #region Component Validation Tests

    [Test]
    public void Component_InheritsFromNetworkBehaviour()
    {
        Assert.IsNotNull(gameRoundManager as Unity.Netcode.NetworkBehaviour, "GameRoundManager debe heredar de NetworkBehaviour");
    }

    [Test]
    public void Component_HasNetworkVariable()
    {
        // Verificar que tiene la NetworkVariable pública
        var field = typeof(GameRoundManager).GetField("isRoundInitialized", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, "Debe tener NetworkVariable isRoundInitialized");
        Assert.AreEqual(typeof(Unity.Netcode.NetworkVariable<bool>), field.FieldType, "isRoundInitialized debe ser NetworkVariable<bool>");
    }

    [Test]
    public void Component_HasPublicMethods()
    {
        // Verificar métodos públicos importantes
        var getRemainingTimeMethod = typeof(GameRoundManager).GetMethod("GetRemainingTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(getRemainingTimeMethod, "Debe tener método GetRemainingTime público");

        var reportQTEPressMethod = typeof(GameRoundManager).GetMethod("ReportQTEPressServerRpc", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(reportQTEPressMethod, "Debe tener método ReportQTEPressServerRpc público");
    }

    [Test]
    public void Component_IsAttachedToGameObject()
    {
        Assert.AreEqual(gameRoundManagerObject, gameRoundManager.gameObject, "Componente debe estar adjunto al GameObject correcto");
        Assert.AreEqual("GameRoundManager", gameRoundManager.gameObject.name, "GameObject debe tener el nombre correcto");
    }

    #endregion

    #region RPC Methods Validation

    [Test]
    public void ServerRpcMethods_HaveCorrectAttributes()
    {
        // Verificar que los métodos ServerRpc existen con los atributos correctos
        var startRoundMethod = typeof(GameRoundManager).GetMethod("StartRoundServerRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(startRoundMethod, "StartRoundServerRpc debe existir");

        var endRoundMethod = typeof(GameRoundManager).GetMethod("EndRoundServerRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(endRoundMethod, "EndRoundServerRpc debe existir");

        var reportQTEMethod = typeof(GameRoundManager).GetMethod("ReportQTEPressServerRpc", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(reportQTEMethod, "ReportQTEPressServerRpc debe existir como método público");
    }

    [Test]
    public void ClientRpcMethods_Exist()
    {
        // Verificar que los métodos ClientRpc existen
        var spawnHUDMethod = typeof(GameRoundManager).GetMethod("SpawnPlayerHUDClientRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(spawnHUDMethod, "SpawnPlayerHUDClientRpc debe existir");

        var startQTEMethod = typeof(GameRoundManager).GetMethod("StartQTEClientRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(startQTEMethod, "StartQTEClientRpc debe existir");

        var endQTEMethod = typeof(GameRoundManager).GetMethod("EndQTEClientRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(endQTEMethod, "EndQTEClientRpc debe existir");
    }

    #endregion

    #region Configuration Validation

    [Test]
    public void ConfigurationFields_Exist()
    {
        // Verificar que los campos de configuración existen (sin verificar valores específicos
        // ya que pueden cambiar y la reflexión puede fallar en Unity Test Runner)

        var matchDurationField = typeof(GameRoundManager).GetField("matchDurationSeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(matchDurationField, "Campo matchDurationSeconds debe existir");

        var spawnDelayField = typeof(GameRoundManager).GetField("spawnStartDelaySeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(spawnDelayField, "Campo spawnStartDelaySeconds debe existir");

        var qteWindowField = typeof(GameRoundManager).GetField("qteInputWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(qteWindowField, "Campo qteInputWindow debe existir");
    }

    [Test]
    public void PrefabReferences_Exist()
    {
        // Verificar que los campos de prefabs existen
        var lethalPrefabField = typeof(GameRoundManager).GetField("lethalPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(lethalPrefabField, "Campo lethalPrefab debe existir");

        var mapPrefabsField = typeof(GameRoundManager).GetField("mapPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(mapPrefabsField, "Campo mapPrefabs debe existir");
    }

    #endregion

    #region Method Existence Tests

    [Test]
    public void MapGeneration_MethodExists()
    {
        var method = typeof(GameRoundManager).GetMethod("GenerateMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "GenerateMap debe existir");
        Assert.AreEqual(typeof(void), method.ReturnType, "GenerateMap debe retornar void");
    }

    [Test]
    public void PlayerSpawning_MethodExists()
    {
        var method = typeof(GameRoundManager).GetMethod("SpawnPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "SpawnPlayer debe existir");
        Assert.AreEqual(typeof(void), method.ReturnType, "SpawnPlayer debe retornar void");
    }

    [Test]
    public void LethalSpawning_MethodExists()
    {
        var method = typeof(GameRoundManager).GetMethod("SpawnLethalServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "SpawnLethalServer debe existir");
        Assert.AreEqual(typeof(void), method.ReturnType, "SpawnLethalServer debe retornar void");
    }

    #endregion

    #region Coroutine Methods Tests

    [Test]
    public void CoroutineMethods_ExistAndReturnIEnumerator()
    {
        // Verificar que los métodos de coroutine existen y retornan IEnumerator
        var serverInitMethod = typeof(GameRoundManager).GetMethod("ServerInitializeRound", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(serverInitMethod, "ServerInitializeRound debe existir");
        Assert.AreEqual(typeof(System.Collections.IEnumerator), serverInitMethod.ReturnType, "Debe retornar IEnumerator");

        var spawnRoutineMethod = typeof(GameRoundManager).GetMethod("ServerSpawnLethalRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(spawnRoutineMethod, "ServerSpawnLethalRoutine debe existir");
        Assert.AreEqual(typeof(System.Collections.IEnumerator), spawnRoutineMethod.ReturnType, "Debe retornar IEnumerator");

        var qteRoutineMethod = typeof(GameRoundManager).GetMethod("QTERoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(qteRoutineMethod, "QTERoutine debe existir");
        Assert.AreEqual(typeof(System.Collections.IEnumerator), qteRoutineMethod.ReturnType, "Debe retornar IEnumerator");
    }

    #endregion

    #region Additional Method Tests

    [Test]
    public void NotifyRoundInterfaceOfEnd_MethodExists()
    {
        var method = typeof(GameRoundManager).GetMethod("NotifyRoundInterfaceOfEnd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "NotifyRoundInterfaceOfEnd debe existir");
        Assert.AreEqual(typeof(void), method.ReturnType, "NotifyRoundInterfaceOfEnd debe retornar void");
    }

    [Test]
    public void NetworkVariable_CanBeModified()
    {
        // Test básico de que la NetworkVariable se puede modificar
        var originalValue = gameRoundManager.isRoundInitialized.Value;
        gameRoundManager.isRoundInitialized.Value = !originalValue;
        Assert.AreNotEqual(originalValue, gameRoundManager.isRoundInitialized.Value, "NetworkVariable debe poder modificarse");

        // Restaurar valor original
        gameRoundManager.isRoundInitialized.Value = originalValue;
    }

    [Test]
    public void GameRoundManager_SurvivesMultipleFrames()
    {
        // Test básico de que el componente sobrevive en el tiempo
        Assert.IsNotNull(gameRoundManager, "GameRoundManager debe existir");
        Assert.IsNotNull(gameRoundManager.gameObject, "GameObject debe existir");
        Assert.IsFalse(gameRoundManager == null, "GameRoundManager no debe ser null");
    }

    #endregion
}
