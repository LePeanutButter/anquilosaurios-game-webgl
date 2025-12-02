using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using TMPro;

public class QTEManagerTests
{
    private QTEManager qteManager;
    private GameObject qteManagerObject;
    private TextMeshProUGUI qteText;
    private GameObject qtePanel;

    [SetUp]
    public void SetUp()
    {
        qteManagerObject = new GameObject("QTEManager");
        qteManager = qteManagerObject.AddComponent<QTEManager>();

        qtePanel = new GameObject("QTEPanel");
        qteText = qtePanel.AddComponent<TextMeshProUGUI>();
        qtePanel.SetActive(false);

        var qtePanelField = typeof(QTEManager).GetField("qtePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var qteTextField = typeof(QTEManager).GetField("qteText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (qtePanelField != null) qtePanelField.SetValue(qteManager, qtePanel);
        if (qteTextField != null) qteTextField.SetValue(qteManager, qteText);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(qteManagerObject);
        if (qtePanel != null) Object.DestroyImmediate(qtePanel);
    }

    [Test]
    public void QTEManager_IsSingleton()
    {
        var instance1 = QTEManager.Instance;
        var instance2 = QTEManager.Instance;
        Assert.AreEqual(instance1, instance2, "QTEManager debe ser singleton");
    }

    [Test]
    public void Awake_InitializesSingleton()
    {
        qteManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(qteManager, null);
        Assert.AreEqual(qteManager, QTEManager.Instance, "QTEManager debe inicializar singleton en Awake");
    }

    [Test]
    public void StartQTE_ActivatesUIAndShowsText()
    {
        qteManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(qteManager, null);

        qteManager.StartQTE();

        Assert.IsTrue(qtePanel.activeSelf, "Panel QTE debe activarse");
        if (qteText != null)
        {
            Assert.AreEqual("Presiona E", qteText.text, "Texto QTE debe mostrar instrucción correcta");
        }
    }

    [Test]
    public void ForceEndQTE_DeactivatesUI()
    {
        qteManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(qteManager, null);
        qteManager.StartQTE();

        qteManager.ForceEndQTE(null);

        Assert.IsFalse(qtePanel.activeSelf, "Panel QTE debe desactivarse después de ForceEndQTE");
    }

    [UnityTest]
    public System.Collections.IEnumerator QTECountdown_ExpiresAfterTimeout()
    {
        qteManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(qteManager, null);

        var inputWindowField = typeof(QTEManager).GetField("inputWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (inputWindowField != null)
        {
            inputWindowField.SetValue(qteManager, 0.1f);
        }

        qteManager.StartQTE();

        yield return new WaitForSeconds(0.15f);

        // Assert
        Assert.IsFalse(qtePanel.activeSelf, "QTE debe expirar después del timeout");
    }

    [Test]
    public void ForceEndQTE_WithWinner_InvokesEvent()
    {
        qteManager.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(qteManager, null);

        bool eventInvoked = false;
        ulong winnerId = 123;
        qteManager.OnQTECompleted += (id) => {
            eventInvoked = true;
            Assert.AreEqual(winnerId, id, "Evento debe invocar con ID correcto del ganador");
        };

        qteManager.ForceEndQTE(winnerId);

        Assert.IsTrue(eventInvoked, "Evento OnQTECompleted debe invocarse cuando hay ganador");
    }
}
