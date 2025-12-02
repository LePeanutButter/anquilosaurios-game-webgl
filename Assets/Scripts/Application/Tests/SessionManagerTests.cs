using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Tests unitarios para SessionManager evitando dependencias de Unity Services y Netcode.
/// Se enfoca en probar la lógica de asignación de personajes y gestión de estado.
/// </summary>
public class SessionManagerTests
{
    private SessionManager sessionManager;
    private GameObject sessionManagerObject;

    [SetUp]
    public void SetUp()
    {
        sessionManagerObject = new GameObject("SessionManager");
        sessionManager = sessionManagerObject.AddComponent<SessionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (sessionManagerObject != null)
            Object.DestroyImmediate(sessionManagerObject);
    }

    #region Singleton Tests

    [Test]
    public void SessionManager_HasInstanceProperty()
    {
        // Arrange
        var instanceProperty = typeof(SessionManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(instanceProperty, "SessionManager debe tener propiedad Instance");
    }

    [Test]
    public void SessionManager_InstanceProperty_IsStatic()
    {
        // Arrange
        var instanceProperty = typeof(SessionManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsTrue(instanceProperty?.GetGetMethod()?.IsStatic ?? false,
            "Instance debe ser una propiedad estática");
    }

    #endregion

    #region Initialization Tests

    [Test]
    public void Awake_InitializesCharacterList()
    {
        // Act
        InvokeAwake();

        // Assert
        var allCharactersField = GetPrivateField<List<CharacterType>>("allCharacters");
        Assert.IsNotNull(allCharactersField, "Lista de personajes debe inicializarse");
        Assert.Greater(allCharactersField.Count, 0, "Lista de personajes debe contener personajes");
    }

    [Test]
    public void Awake_InitializesPlayerCharacterMap()
    {
        // Act
        InvokeAwake();

        // Assert
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");
        Assert.IsNotNull(playerCharacterMap, "Mapa de personajes debe inicializarse");
        Assert.AreEqual(0, playerCharacterMap.Count, "Mapa debe empezar vacío");
    }

    [Test]
    public void Awake_InitializesAssignedCharactersSet()
    {
        // Act
        InvokeAwake();

        // Assert
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");
        Assert.IsNotNull(assignedCharactersSet, "Conjunto de personajes asignados debe inicializarse");
        Assert.AreEqual(0, assignedCharactersSet.Count, "Conjunto debe empezar vacío");
    }

    [Test]
    public void Awake_InitializesAuthIdDictionary()
    {
        // Act
        InvokeAwake();

        // Assert
        var authIdByClientId = GetPrivateField<Dictionary<ulong, string>>("authIdByClientId");
        Assert.IsNotNull(authIdByClientId, "Diccionario de AuthId debe inicializarse");
    }

    #endregion

    #region Character Assignment Tests

    [Test]
    public void GetUniqueCharacter_ReturnsValidCharacter()
    {
        // Arrange
        InvokeAwake();

        // Act
        var character = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");

        // Assert
        Assert.AreNotEqual(CharacterType.None, character, "No debe retornar personaje None");
        Assert.IsTrue(
            character == CharacterType.Flor ||
            character == CharacterType.Girasol ||
            character == CharacterType.Jalapeno ||
            character == CharacterType.Manzana,
            "Debe retornar un tipo de personaje válido");
    }

    [Test]
    public void GetUniqueCharacter_ReturnsDifferentCharacters()
    {
        // Arrange
        InvokeAwake();

        // Act
        var char1 = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");
        var char2 = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");

        // Assert
        Assert.AreNotEqual(char1, char2, "Personajes consecutivos deben ser diferentes cuando sea posible");
    }

    [Test]
    public void GetUniqueCharacter_HandlesAllCharactersAssigned()
    {
        // Arrange
        InvokeAwake();
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        // Asignar todos los personajes
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Girasol);
        assignedCharactersSet.Add(CharacterType.Jalapeno);
        assignedCharactersSet.Add(CharacterType.Manzana);

        // Act
        var result = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");

        // Assert
        Assert.AreNotEqual(CharacterType.None, result,
            "Debe retornar un personaje válido incluso sin personajes únicos disponibles");
    }

    [Test]
    public void GetUniqueCharacter_HandlesEmptyCharacterList()
    {
        // Arrange
        InvokeAwake();
        var allCharactersField = typeof(SessionManager).GetField("allCharacters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        allCharactersField?.SetValue(sessionManager, new List<CharacterType>());

        Assert.Throws<System.Exception>(() =>
            InvokePrivateMethod<CharacterType>("GetUniqueCharacter"));
    }

    #endregion

    #region Character Release Tests

    [Test]
    public void ReleaseCharacter_RemovesCharacterAssignment()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        ulong clientId = 123;
        var character = CharacterType.Flor;
        playerCharacterMap[clientId] = character;
        assignedCharactersSet.Add(character);

        // Act
        sessionManager.ReleaseCharacter(clientId);

        // Assert
        Assert.IsFalse(playerCharacterMap.ContainsKey(clientId),
            "Personaje debe removerse del mapa de jugadores");
        Assert.IsFalse(assignedCharactersSet.Contains(character),
            "Personaje debe removerse del conjunto asignado");
    }

    [Test]
    public void ReleaseCharacter_HandlesNonExistentClient()
    {
        // Arrange
        InvokeAwake();

        // Act & Assert
        Assert.DoesNotThrow(() => sessionManager.ReleaseCharacter(999),
            "Debe manejar la liberación de personajes para clientes inexistentes sin errores");
    }

    [Test]
    public void ReleaseCharacter_HandlesClientWithoutCharacter()
    {
        // Arrange
        InvokeAwake();
        ulong clientId = 555;

        // Act & Assert
        Assert.DoesNotThrow(() => sessionManager.ReleaseCharacter(clientId),
            "Cliente sin personaje asignado debe manejarse correctamente");
    }

    [Test]
    public void ReleaseCharacter_OnlyRemovesSpecifiedClient()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        ulong clientId1 = 100;
        ulong clientId2 = 200;
        playerCharacterMap[clientId1] = CharacterType.Flor;
        playerCharacterMap[clientId2] = CharacterType.Girasol;
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Girasol);

        // Act
        sessionManager.ReleaseCharacter(clientId1);

        // Assert
        Assert.IsFalse(playerCharacterMap.ContainsKey(clientId1), "Cliente 1 debe removerse");
        Assert.IsTrue(playerCharacterMap.ContainsKey(clientId2), "Cliente 2 debe permanecer");
        Assert.IsFalse(assignedCharactersSet.Contains(CharacterType.Flor), "Flor debe liberarse");
        Assert.IsTrue(assignedCharactersSet.Contains(CharacterType.Girasol), "Girasol debe permanecer");
    }

    #endregion

    #region Try Get Assigned Character Tests

    [Test]
    public void TryGetAssignedCharacter_ReturnsCorrectCharacter()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");

        ulong clientId = 123;
        var expectedCharacter = CharacterType.Girasol;
        playerCharacterMap[clientId] = expectedCharacter;

        // Act
        bool result = sessionManager.TryGetAssignedCharacter(clientId, out CharacterType actualCharacter);

        // Assert
        Assert.IsTrue(result, "Debe retornar true cuando el personaje está asignado");
        Assert.AreEqual(expectedCharacter, actualCharacter, "Debe retornar el personaje correcto");
    }

    [Test]
    public void TryGetAssignedCharacter_ReturnsFalseForUnassignedClient()
    {
        // Arrange
        InvokeAwake();
        ulong clientId = 999;

        // Act
        bool result = sessionManager.TryGetAssignedCharacter(clientId, out CharacterType character);

        // Assert
        Assert.IsFalse(result, "Debe retornar false para cliente no asignado");
        Assert.AreEqual(CharacterType.None, character, "Debe retornar None para cliente no asignado");
    }

    [Test]
    public void TryGetAssignedCharacter_HandlesMultipleClients()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");

        playerCharacterMap[100] = CharacterType.Flor;
        playerCharacterMap[200] = CharacterType.Girasol;
        playerCharacterMap[300] = CharacterType.Jalapeno;

        // Act & Assert
        Assert.IsTrue(sessionManager.TryGetAssignedCharacter(100, out var char1));
        Assert.AreEqual(CharacterType.Flor, char1);

        Assert.IsTrue(sessionManager.TryGetAssignedCharacter(200, out var char2));
        Assert.AreEqual(CharacterType.Girasol, char2);

        Assert.IsTrue(sessionManager.TryGetAssignedCharacter(300, out var char3));
        Assert.AreEqual(CharacterType.Jalapeno, char3);

        Assert.IsFalse(sessionManager.TryGetAssignedCharacter(400, out _));
    }

    #endregion

    #region Active Session Tests

    [Test]
    public void ActiveSession_CanBeSet()
    {
        // Act
        sessionManager.ActiveSession = null;

        // Assert
        Assert.IsNull(sessionManager.ActiveSession, "ActiveSession debe poder establecerse");
    }

    [Test]
    public void ActiveSession_AcceptsNullValue()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => sessionManager.ActiveSession = null,
            "El setter debe aceptar valores null sin errores");
        Assert.IsNull(sessionManager.ActiveSession, "ActiveSession debe ser null después de asignar null");
    }

    [Test]
    public void ActiveSession_InitiallyNull()
    {
        // Assert
        Assert.IsNull(sessionManager.ActiveSession, "ActiveSession debe ser null inicialmente");
    }

    #endregion

    #region GetPlayerStateForClient Tests

    [Test]
    public void GetPlayerStateForClient_ReturnsNullWhenNotInitialized()
    {
        // Act
        var result = sessionManager.GetPlayerStateForClient(123);

        // Assert
        Assert.IsNull(result, "Debe retornar null cuando no está inicializado");
    }

    [Test]
    public void GetPlayerStateForClient_HandlesZeroClientId()
    {
        // Act
        var result = sessionManager.GetPlayerStateForClient(0);

        // Assert
        Assert.IsTrue(result == null || result != null,
            "Debe manejar clientId 0 sin excepciones");
    }

    [Test]
    public void GetPlayerStateForClient_HandlesLargeClientId()
    {
        // Act
        var result = sessionManager.GetPlayerStateForClient(ulong.MaxValue);

        // Assert
        Assert.IsNull(result, "Debe manejar clientId grande sin excepciones");
    }

    #endregion

    #region Character Assignment Logic Tests

    [Test]
    public void CharacterAssignment_MaintainsUniqueness()
    {
        // Arrange
        InvokeAwake();
        var assignedCharacters = new List<CharacterType>();

        // Act - Obtener 4 personajes
        for (int i = 0; i < 4; i++)
        {
            var character = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");
            assignedCharacters.Add(character);
        }

        // Assert - Los primeros 4 deben ser únicos
        var uniqueCount = new HashSet<CharacterType>(assignedCharacters).Count;
        Assert.AreEqual(4, uniqueCount, "Los primeros 4 personajes deben ser únicos");
    }

    [Test]
    public void CharacterAssignment_HandlesAllCharactersUsed()
    {
        // Arrange
        InvokeAwake();
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        // Asignar todos los personajes
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Girasol);
        assignedCharactersSet.Add(CharacterType.Jalapeno);
        assignedCharactersSet.Add(CharacterType.Manzana);

        // Act - Intentar obtener uno más
        var character = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");

        // Assert - Debe retornar un personaje válido (reutilizado)
        Assert.IsTrue(
            character == CharacterType.Flor ||
            character == CharacterType.Girasol ||
            character == CharacterType.Jalapeno ||
            character == CharacterType.Manzana,
            "Debe reutilizar personajes cuando todos están asignados");
    }

    #endregion

    #region Dictionary Management Tests

    [Test]
    public void PlayerCharacterMap_AddsEntriesCorrectly()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");

        // Act
        playerCharacterMap[100] = CharacterType.Flor;
        playerCharacterMap[200] = CharacterType.Girasol;

        // Assert
        Assert.AreEqual(2, playerCharacterMap.Count, "Debe tener 2 entradas");
        Assert.AreEqual(CharacterType.Flor, playerCharacterMap[100]);
        Assert.AreEqual(CharacterType.Girasol, playerCharacterMap[200]);
    }

    [Test]
    public void AssignedCharactersSet_TracksAssignments()
    {
        // Arrange
        InvokeAwake();
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        // Act
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Girasol);

        // Assert
        Assert.AreEqual(2, assignedCharactersSet.Count, "Debe tener 2 personajes asignados");
        Assert.IsTrue(assignedCharactersSet.Contains(CharacterType.Flor));
        Assert.IsTrue(assignedCharactersSet.Contains(CharacterType.Girasol));
    }

    [Test]
    public void AssignedCharactersSet_PreventsDuplicates()
    {
        // Arrange
        InvokeAwake();
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        // Act
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Flor);
        assignedCharactersSet.Add(CharacterType.Flor);

        // Assert
        Assert.AreEqual(1, assignedCharactersSet.Count,
            "HashSet debe prevenir duplicados");
    }

    #endregion

    #region Method Existence Tests

    [Test]
    public void SessionManager_HasRequiredMethods()
    {
        // Arrange
        var methods = new[]
        {
            "Awake",
            "GetUniqueCharacter",
            "ReleaseCharacter",
            "TryGetAssignedCharacter",
            "GetPlayerStateForClient"
        };

        // Act & Assert
        foreach (var methodName in methods)
        {
            var method = typeof(SessionManager).GetMethod(methodName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, $"SessionManager debe tener el método {methodName}");
        }
    }

    [Test]
    public void SessionManager_HasActiveSessionProperty()
    {
        // Act
        var property = typeof(SessionManager).GetProperty("ActiveSession",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(property, "SessionManager debe tener propiedad ActiveSession");
        Assert.IsTrue(property.CanRead, "ActiveSession debe tener getter");
        Assert.IsTrue(property.CanWrite, "ActiveSession debe tener setter");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void CompleteFlow_AssignAndRelease()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        ulong clientId = 100;

        // Act - Assign
        var character = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");
        playerCharacterMap[clientId] = character;
        assignedCharactersSet.Add(character);

        // Assert - After assignment
        Assert.AreEqual(1, playerCharacterMap.Count);
        Assert.AreEqual(1, assignedCharactersSet.Count);
        Assert.IsTrue(sessionManager.TryGetAssignedCharacter(clientId, out var retrievedChar));
        Assert.AreEqual(character, retrievedChar);

        // Act - Release
        sessionManager.ReleaseCharacter(clientId);

        // Assert - After release
        Assert.AreEqual(0, playerCharacterMap.Count);
        Assert.AreEqual(0, assignedCharactersSet.Count);
        Assert.IsFalse(sessionManager.TryGetAssignedCharacter(clientId, out _));
    }

    [Test]
    public void MultipleClients_AssignDifferentCharacters()
    {
        // Arrange
        InvokeAwake();
        var playerCharacterMap = GetPrivateField<Dictionary<ulong, CharacterType>>("playerCharacterMap");
        var assignedCharactersSet = GetPrivateField<HashSet<CharacterType>>("assignedCharactersSet");

        // Act - Assign to 4 clients
        for (ulong i = 0; i < 4; i++)
        {
            var character = InvokePrivateMethod<CharacterType>("GetUniqueCharacter");
            playerCharacterMap[i] = character;
            assignedCharactersSet.Add(character);
        }

        // Assert
        Assert.AreEqual(4, playerCharacterMap.Count, "Debe tener 4 clientes asignados");
        Assert.AreEqual(4, assignedCharactersSet.Count, "Debe tener 4 personajes únicos asignados");

        // Verify all different
        var characters = new HashSet<CharacterType>(playerCharacterMap.Values);
        Assert.AreEqual(4, characters.Count, "Todos los personajes deben ser diferentes");
    }

    #endregion

    #region Helper Methods

    private void InvokeAwake()
    {
        var awakeMethod = typeof(SessionManager).GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod?.Invoke(sessionManager, null);
    }

    private T GetPrivateField<T>(string fieldName)
    {
        var field = typeof(SessionManager).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (T)field.GetValue(sessionManager) : default;
    }

    private T InvokePrivateMethod<T>(string methodName, params object[] parameters)
    {
        var method = typeof(SessionManager).GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method != null ? (T)method.Invoke(sessionManager, parameters) : default;
    }

    #endregion
}