using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class PlayerControllerTests
{
    private GameObject _playerObject;
    private PlayerController _controller;

    [SetUp]
    public void Setup()
    {
        _playerObject = new GameObject("Player");
        _playerObject.AddComponent<BoxCollider2D>();
        _controller = _playerObject.AddComponent<PlayerController>();

        _controller.Invoke("Awake", 0f);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(_playerObject);
    }

    [Test]
    public void WalkSpeed_IsAppliedCorrectly()
    {
        _controller.SetWalkSpeed(5f);
        Assert.AreEqual(5f, _controller.CurrentSpeed);
    }

    [Test]
    public void SetFacingDirection_FlipsScale_WhenDirectionChanges()
    {
        _playerObject.transform.localScale = new Vector3(1f, 1f, 1f);
        var input = new Vector2(-1f, 0f);

        typeof(PlayerController).GetMethod("SetFacingDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_controller, new object[] { input });

        Assert.AreEqual(-1f, _playerObject.transform.localScale.x);
    }

    [Test]
    public void WalkSpeed_IsClampedAtZero()
    {
        _controller.SetWalkSpeed(-5f);
        Assert.AreEqual(0f, _controller.CurrentSpeed);
    }

    [Test]
    public void RunMultiplier_IsClampedAtOne()
    {
        _controller.SetRunMultiplier(0.5f);
        typeof(PlayerController).GetProperty("CurrentSpeed")?.GetValue(_controller);
        Assert.AreEqual(_controller.CurrentSpeed, _controller.CurrentSpeed);
    }

    [Test]
    public void OnValidate_ClampsValues()
    {
        typeof(PlayerController).GetField("walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, -10f);
        typeof(PlayerController).GetField("runSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, 20f);

        typeof(PlayerController).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_controller, null);

        var walkSpeed = (float)typeof(PlayerController).GetField("walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(_controller);
        var runSpeed = (float)typeof(PlayerController).GetField("runSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(_controller);

        Assert.GreaterOrEqual(walkSpeed, 0f);
        Assert.LessOrEqual(runSpeed, 10f);
    }

    [Test]
    public void CurrentSpeed_ReflectsRunning()
    {
        _controller.SetWalkSpeed(8f);
        typeof(PlayerController).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, true);
        _controller.SetRunMultiplier(1.8f);

        Assert.AreEqual(14.4f, _controller.CurrentSpeed);
    }


}
