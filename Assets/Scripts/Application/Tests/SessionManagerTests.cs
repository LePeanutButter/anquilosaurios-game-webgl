using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        Object.DestroyImmediate(sessionManagerObject);
    }

    [Test]
    public void SessionManager_IsSingleton()
    {
        var instance1 = SessionManager.Instance;
        var instance2 = SessionManager.Instance;
        Assert.AreEqual(instance1, instance2, "SessionManager debe ser singleton");
    }

    [Test]
    public void Awake_InitializesCharacterList()
    {
        sessionManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(sessionManager, null);

        var allCharactersField = typeof(SessionManager).GetField("allCharacters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (allCharactersField != null)
        {
            var allCharacters = (System.Collections.Generic.List<CharacterType>)allCharactersField.GetValue(sessionManager);
            Assert.IsNotNull(allCharacters, "Lista de personajes debe inicializarse");
            Assert.Greater(allCharacters.Count, 0, "Lista de personajes debe contener personajes");
        }
    }

    [Test]
    public void GetUniqueCharacter_ReturnsValidCharacter()
    {
        sessionManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(sessionManager, null);

        var method = typeof(SessionManager).GetMethod("GetUniqueCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            var character = (CharacterType)method.Invoke(sessionManager, null);

            Assert.AreNotEqual(CharacterType.None, character, "No debe retornar personaje None");
            Assert.IsTrue(character == CharacterType.Flor || character == CharacterType.Girasol ||
                         character == CharacterType.Jalapeno || character == CharacterType.Manzana,
                         "Debe retornar un tipo de personaje válido");
        }
    }

    [Test]
    public void ReleaseCharacter_RemovesCharacterAssignment()
    {
        sessionManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(sessionManager, null);

        ulong clientId = 123;

        var playerCharacterMapField = typeof(SessionManager).GetField("playerCharacterMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var assignedCharactersSetField = typeof(SessionManager).GetField("assignedCharactersSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (playerCharacterMapField != null && assignedCharactersSetField != null)
        {
            var playerCharacterMap = (System.Collections.Generic.Dictionary<ulong, CharacterType>)playerCharacterMapField.GetValue(sessionManager);
            var assignedCharactersSet = (System.Collections.Generic.HashSet<CharacterType>)assignedCharactersSetField.GetValue(sessionManager);

            var character = CharacterType.Flor;
            playerCharacterMap[clientId] = character;
            assignedCharactersSet.Add(character);

            sessionManager.ReleaseCharacter(clientId);

            Assert.IsFalse(playerCharacterMap.ContainsKey(clientId), "Personaje debe removerse del mapa de jugadores");
            Assert.IsFalse(assignedCharactersSet.Contains(character), "Personaje debe removerse del conjunto asignado");
        }
    }

    [Test]
    public void TryGetAssignedCharacter_ReturnsCorrectCharacter()
    {
        sessionManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(sessionManager, null);

        ulong clientId = 123;
        var expectedCharacter = CharacterType.Girasol;

        var playerCharacterMapField = typeof(SessionManager).GetField("playerCharacterMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerCharacterMapField != null)
        {
            var playerCharacterMap = (System.Collections.Generic.Dictionary<ulong, CharacterType>)playerCharacterMapField.GetValue(sessionManager);
            playerCharacterMap[clientId] = expectedCharacter;

            bool result = sessionManager.TryGetAssignedCharacter(clientId, out CharacterType actualCharacter);

            Assert.IsTrue(result, "Debe retornar true cuando el personaje está asignado");
            Assert.AreEqual(expectedCharacter, actualCharacter, "Debe retornar el personaje correcto");
        }
    }

    [Test]
    public void TryGetAssignedCharacter_ReturnsFalseForUnassignedClient()
    {
        ulong clientId = 999;

        bool result = sessionManager.TryGetAssignedCharacter(clientId, out CharacterType character);

        Assert.IsFalse(result, "Debe retornar false para cliente no asignado");
        Assert.AreEqual(CharacterType.None, character, "Debe retornar None para cliente no asignado");
    }

    [Test]
    public void CharacterAssignment_UniquePerClient()
    {
        sessionManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(sessionManager, null);

        var method = typeof(SessionManager).GetMethod("GetUniqueCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            var char1 = (CharacterType)method.Invoke(sessionManager, null);
            var char2 = (CharacterType)method.Invoke(sessionManager, null);

            Assert.AreNotEqual(char1, char2, "Personajes deben ser únicos cuando sea posible");
        }
    }
}
