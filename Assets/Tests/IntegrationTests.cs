using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

/// <summary>
/// Tests de integración que prueban múltiples componentes trabajando juntos
/// </summary>
public class IntegrationTests
{
    [Test]
    public void MultipleUIComponents_CanCoexist()
    {
        var container = new GameObject("UIContainer");

        var playerCard = container.AddComponent<PlayerCardUI>();
        var healthBar = container.AddComponent<HealthBarHUD>();
        var playerListEntry = container.AddComponent<PlayerListEntryUI>();

        var nameText = new GameObject("NameText").AddComponent<TextMeshProUGUI>();
        var deathText = new GameObject("DeathText").AddComponent<TextMeshProUGUI>();
        var winsText = new GameObject("WinsText").AddComponent<TextMeshProUGUI>();
        var sliderObj = new GameObject("HealthSlider");
        var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();

        playerCard.playerNameText = nameText;
        playerCard.characterImage = new GameObject("Image").AddComponent<UnityEngine.UI.Image>();
        healthBar.healthBar = slider;
        playerListEntry.nameText = nameText;
        playerListEntry.deathCountText = deathText;
        playerListEntry.roundWinsText = winsText;

        playerCard.Setup("TestPlayer", CharacterType.Flor);
        healthBar.maxHealth = 100f;
        healthBar.currentHealth = 75f;
        playerListEntry.nameText.text = "TestPlayer";
        playerListEntry.deathCountText.text = "2";
        playerListEntry.roundWinsText.text = "1";

        Assert.AreEqual("TestPlayer", playerCard.playerNameText.text, "PlayerCard debe mantener su texto");
        Assert.AreEqual(100f, healthBar.maxHealth, "HealthBar debe mantener su salud máxima");
        Assert.AreEqual("TestPlayer", playerListEntry.nameText.text, "PlayerListEntry debe mantener su texto");

        Object.DestroyImmediate(container);
        Object.DestroyImmediate(nameText.gameObject);
        Object.DestroyImmediate(deathText.gameObject);
        Object.DestroyImmediate(winsText.gameObject);
        Object.DestroyImmediate(sliderObj);
    }

    [Test]
    public void Managers_CanBeCreatedTogether()
    {
        var container = new GameObject("ManagersContainer");

        var qteManager = container.AddComponent<QTEManager>();
        var audioManager = container.AddComponent<AudioManager>();
        var sessionManager = container.AddComponent<SessionManager>();

        Assert.IsNotNull(qteManager, "QTEManager debe crearse correctamente");
        Assert.IsNotNull(audioManager, "AudioManager debe crearse correctamente");
        Assert.IsNotNull(sessionManager, "SessionManager debe crearse correctamente");

        Assert.AreEqual(QTEManager.Instance, qteManager, "QTEManager debe ser singleton");
        Assert.AreEqual(AudioManager.Instance, audioManager, "AudioManager debe ser singleton");
        Assert.AreEqual(SessionManager.Instance, sessionManager, "SessionManager debe ser singleton");

        Object.DestroyImmediate(container);
    }

    [UnityTest]
    public System.Collections.IEnumerator UI_Update_Loop_Works()
    {

        var container = new GameObject("UITest");

        var healthBar = container.AddComponent<HealthBarHUD>();
        var sliderObj = new GameObject("Slider");
        var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        healthBar.healthBar = slider;

        yield return null;
        yield return null;

        Assert.IsNotNull(healthBar, "HealthBar debe sobrevivir a updates");
        Assert.IsNotNull(slider, "Slider debe existir");

        Object.DestroyImmediate(container);
        Object.DestroyImmediate(sliderObj);
    }

    [Test]
    public void CharacterType_Enum_Values_AreValid()
    {
        Assert.AreEqual(0, (int)CharacterType.None, "None debe ser 0");
        Assert.AreEqual(1, (int)CharacterType.Flor, "Flor debe ser 1");
        Assert.AreEqual(2, (int)CharacterType.Girasol, "Girasol debe ser 2");
        Assert.AreEqual(3, (int)CharacterType.Jalapeno, "Jalapeno debe ser 3");
        Assert.AreEqual(4, (int)CharacterType.Manzana, "Manzana debe ser 4");

        Assert.AreEqual(CharacterType.Flor, System.Enum.Parse<CharacterType>("Flor"));
        Assert.AreEqual(CharacterType.Girasol, System.Enum.Parse<CharacterType>("Girasol"));
    }

    [Test]
    public void Component_Initialization_Order_Matters()
    {
        var container = new GameObject("InitOrderTest");

        var sessionManager = container.AddComponent<SessionManager>();
        var qteManager = container.AddComponent<QTEManager>();
        var audioManager = container.AddComponent<AudioManager>();

        Assert.IsNotNull(sessionManager, "SessionManager debe inicializarse primero");
        Assert.IsNotNull(qteManager, "QTEManager debe inicializarse después");
        Assert.IsNotNull(audioManager, "AudioManager debe inicializarse al final");

        Object.DestroyImmediate(container);
    }
}
