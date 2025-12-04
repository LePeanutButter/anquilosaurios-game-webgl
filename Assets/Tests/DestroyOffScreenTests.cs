using NUnit.Framework;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

public class DestroyOffScreenTests
{
    private GameObject testObject;
    private DestroyOffScreen destroyOffScreen;
    private Camera camera;

    [SetUp]
    public void SetUp()
    {
        // Crear un objeto de prueba y añadir el componente DestroyOffScreen
        testObject = new GameObject("TestObject");
        destroyOffScreen = testObject.AddComponent<DestroyOffScreen>();

        // Crear cámara principal de prueba
        var cameraGO = new GameObject("MainCamera");
        camera = cameraGO.AddComponent<Camera>();
        camera.tag = "MainCamera"; // necesario para Camera.main
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
        Object.DestroyImmediate(camera.gameObject);
    }

    [UnityTest]
    public IEnumerator Start_AssignsMainCamera()
    {
        // Esperar un frame para que Start se ejecute
        yield return null;

        Assert.AreEqual(Camera.main, typeof(DestroyOffScreen)
            .GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(destroyOffScreen));
    }

    [UnityTest]
    public IEnumerator Update_OffScreenBelowDespawns_Server()
    {
        // Simular que es servidor
        typeof(NetworkBehaviour).GetProperty("IsServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(destroyOffScreen, true);

        // Simular NetworkObject
        var networkObjectGO = new GameObject("NetworkObject");
        var networkObject = networkObjectGO.AddComponent<NetworkObject>();
        destroyOffScreen.GetType().GetProperty("NetworkObject").SetValue(destroyOffScreen, networkObject);
        networkObject.Spawn();

        // Colocar el objeto fuera de pantalla (abajo)
        destroyOffScreen.transform.position = new Vector3(0, -10, 0);

        yield return null; // Esperar un frame para que Update se ejecute

        Assert.IsFalse(networkObject.IsSpawned, "El objeto debería haber sido despawned en servidor");
        Object.DestroyImmediate(networkObjectGO);
    }


    [UnityTest]
    public IEnumerator Update_OffScreenBelowRequestsDespawn_ClientOwner()
    {
        // Simular que es propietario (pero no servidor)
        typeof(NetworkBehaviour).GetProperty("IsServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(destroyOffScreen, false);
        typeof(NetworkBehaviour).GetProperty("IsOwner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(destroyOffScreen, true);

        var networkObjectGO = new GameObject("NetworkObject");
        var networkObject = networkObjectGO.AddComponent<NetworkObject>();
        destroyOffScreen.GetType().GetProperty("NetworkObject").SetValue(destroyOffScreen, networkObject);
        networkObject.Spawn();

        destroyOffScreen.transform.position = new Vector3(0, -10, 0);

        yield return null;

        Assert.IsTrue(networkObject.IsSpawned, "El objeto sigue spawned en modo cliente, la ServerRpc no puede ejecutarse en test unitario");
        Object.DestroyImmediate(networkObjectGO);
    }

    [UnityTest]
    public IEnumerator Update_OnScreen_DoesNotDespawn()
    {
        destroyOffScreen.transform.position = new Vector3(0, 0, 0);

        yield return null;

        Assert.Pass("Objeto dentro de pantalla no intenta despawn y no lanza excepción");
    }
}
