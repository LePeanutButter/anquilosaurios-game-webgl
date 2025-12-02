using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el nombre del jugador en la UI
/// </summary>
public class PlayerNameDisplay : MonoBehaviour
{
    [Header("UI Referencias")]
    [SerializeField] private TextMeshProUGUI playerNameText; 
    
    [Header("Configuración")]
    [SerializeField] private string defaultName = "Player";
    [SerializeField] private bool updateOnStart = true;

    private void Start()
    {
        if (updateOnStart)
        {
            UpdatePlayerName();
        }
        
        WebAuthReceiver.OnUserDataReceived += OnUserAuthenticated;
    }

    private void OnDestroy()
    {
        WebAuthReceiver.OnUserDataReceived -= OnUserAuthenticated;
    }

    /// <summary>
    /// Llamado cuando se reciben datos del usuario
    /// </summary>
    private void OnUserAuthenticated(string token, string userName, string userEmail)
    {
        Debug.Log($"[PlayerNameDisplay] Usuario autenticado: {userName}");
        SetPlayerName(userName);
    }

    /// <summary>
    /// Actualizar el nombre del jugador desde el WebAuthReceiver
    /// </summary>
    public void UpdatePlayerName()
    {
        string userName = WebAuthReceiver.GetUserName();
        SetPlayerName(userName);
    }

    /// <summary>
    /// Establecer el nombre del jugador en la UI
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = !string.IsNullOrEmpty(name) ? name : defaultName;
            Debug.Log($"[PlayerNameDisplay] Nombre actualizado: {playerNameText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerNameDisplay] playerNameText no está asignado");
        }
    }

    /// <summary>
    /// Método para llamar desde botones u otros componentes
    /// </summary>
    public void RefreshPlayerName()
    {
        UpdatePlayerName();
    }
}
