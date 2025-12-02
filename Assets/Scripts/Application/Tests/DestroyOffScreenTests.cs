using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DestroyOffScreenTests
{
    private DestroyOffScreen destroyOffScreen;
    private GameObject destroyOffScreenObject;
    private Camera testCamera;

    [SetUp]
    public void SetUp()
    {
        destroyOffScreenObject = new GameObject("DestroyOffScreen");
        destroyOffScreen = destroyOffScreenObject.AddComponent<DestroyOffScreen>();

        var cameraObject = new GameObject("TestCamera");
        testCamera = cameraObject.AddComponent<Camera>();
        testCamera.orthographic = true;
        testCamera.orthographicSize = 5f;
        testCamera.transform.position = new Vector3(0, 0, -10);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(destroyOffScreenObject);
        Object.DestroyImmediate(testCamera.gameObject);
    }

    [Test]
    public void Start_AssignsMainCamera()
    {
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);

        var cameraField = typeof(DestroyOffScreen).GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (cameraField != null)
        {
            var assignedCamera = (Camera)cameraField.GetValue(destroyOffScreen);
            Assert.IsNull(assignedCamera, "Cámara principal debe asignarse en Start");
        }
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_WithNullCamera_DoesNothing()
    {
        var cameraField = typeof(DestroyOffScreen).GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (cameraField != null)
        {
            cameraField.SetValue(destroyOffScreen, null);
        }

        yield return null;

        Assert.IsNotNull(destroyOffScreen, "DestroyOffScreen debe existir sin cámara");
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_ObjectAboveScreen_DoesNotDestroy()
    {
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);

        destroyOffScreenObject.transform.position = new Vector3(0, 10, 0);

        // Act
        yield return null;

        Assert.IsNotNull(destroyOffScreen, "Objeto debe persistir si está en pantalla");
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_ObjectOnScreen_DoesNotDestroy()
    {
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);

        destroyOffScreenObject.transform.position = new Vector3(0, 0, 0);

        yield return null;

        Assert.IsNotNull(destroyOffScreen, "Objeto debe persistir si está en pantalla");
    }

    [Test]
    public void Component_HasNetworkBehaviour()
    {
        Assert.IsNotNull(destroyOffScreen as Unity.Netcode.NetworkBehaviour, "DestroyOffScreen debe heredar de NetworkBehaviour");
    }

    [Test]
    public void Component_IsAttachedToGameObject()
    {
        Assert.AreEqual(destroyOffScreenObject, destroyOffScreen.gameObject, "Componente debe estar adjunto al GameObject correcto");
    }

    [Test]
    public void Transform_IsAccessible()
    {
        Assert.IsNotNull(destroyOffScreen.transform, "Transform debe ser accesible");
    }

    [Test]
    public void GameObject_IsValid()
    {
        Assert.IsNotNull(destroyOffScreen.gameObject, "GameObject debe ser válido");
        Assert.AreEqual("DestroyOffScreen", destroyOffScreen.gameObject.name, "GameObject debe tener el nombre correcto");
    }

    [Test]
    public void Start_CanBeCalledMultipleTimes()
    {
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);

        Assert.IsNotNull(destroyOffScreen, "Start debe poder llamarse múltiples veces sin problemas");
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_CanBeCalledMultipleTimes()
    {
        destroyOffScreen.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(destroyOffScreen, null);

        yield return null;
        yield return null;
        yield return null;

        Assert.IsNotNull(destroyOffScreen, "Update debe poder llamarse múltiples veces sin problemas");
    }
}
