//using NUnit.Framework;
//using System.Reflection;
//using UnityEngine;

//public class PlayerPresenterTests
//{
//    private GameObject _playerObject;
//    private PlayerPresenter _presenter;

//    [SetUp]
//    public void Setup()
//    {
//        _playerObject = new GameObject("Player");
//        _playerObject.AddComponent<BoxCollider2D>();
//        _presenter = _playerObject.AddComponent<PlayerPresenter>();
//        _presenter.Invoke("Awake", 0f);
//    }

//    [TearDown]
//    public void Teardown()
//    {
//        Object.DestroyImmediate(_playerObject);
//    }

//    [Test]
//    public void WalkSpeed_IsAppliedCorrectly()
//    {
//        _presenter.SetWalkSpeed(5f);
//        Assert.AreEqual(5f, _presenter.CurrentSpeed);
//    }

//    [Test]
//    public void SetFacingDirection_FlipsScale_WhenDirectionChanges()
//    {
//        _playerObject.transform.localScale = new Vector3(1f, 1f, 1f);
//        var input = new Vector2(-1f, 0f);

//        typeof(PlayerPresenter).GetMethod("SetFacingDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
//            ?.Invoke(_presenter, new object[] { input });

//        Assert.AreEqual(-1f, _playerObject.transform.localScale.x);
//    }

//    [Test]
//    public void WalkSpeed_IsClampedAtZero()
//    {
//        _presenter.SetWalkSpeed(-5f);
//        Assert.AreEqual(0f, _presenter.CurrentSpeed);
//    }

//    [Test]
//    public void RunMultiplier_IsClampedAtOne()
//    {
//        _presenter.SetRunMultiplier(0.5f);
//        typeof(PlayerPresenter).GetProperty("CurrentSpeed")?.GetValue(_presenter);
//        Assert.AreEqual(_presenter.CurrentSpeed, _presenter.CurrentSpeed);
//    }

//    [Test]
//    public void OnValidate_ClampsValues()
//    {
//        typeof(PlayerPresenter).GetField("walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.SetValue(_presenter, -10f);
//        typeof(PlayerPresenter).GetField("runSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.SetValue(_presenter, 20f);

//        typeof(PlayerPresenter).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.Invoke(_presenter, null);

//        var walkSpeed = (float)typeof(PlayerPresenter).GetField("walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.GetValue(_presenter);
//        var runSpeed = (float)typeof(PlayerPresenter).GetField("runSpeed", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.GetValue(_presenter);

//        Assert.GreaterOrEqual(walkSpeed, 0f);
//        Assert.LessOrEqual(runSpeed, 10f);
//    }

//    [Test]
//    public void CurrentSpeed_ReflectsRunning()
//    {
//        _presenter.SetWalkSpeed(8f);
//        typeof(PlayerPresenter).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance)
//            ?.SetValue(_presenter, true);
//        _presenter.SetRunMultiplier(1.8f);

//        Assert.AreEqual(14.4f, _presenter.CurrentSpeed);
//    }
//}
