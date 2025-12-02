using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Unity.Netcode;

/// <summary>
/// Recibe datos de autenticación desde el navegador (Svelte frontend)
/// y los aplica al jugador en Unity
/// </summary>
public class WebAuthReceiver : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerNameDisplay playerNameDisplay;
    
    [Header("Estado")]
    public string UserToken { get; private set; }
    public string UserName { get; private set; } = "Player";
    public string UserEmail { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public static event Action<string, string, string> OnUserDataReceived;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[WebAuth] Esperando datos de autenticación desde el navegador...");
        
        SendMessageToBrowser("UNITY_READY");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        RegisterMessageListener();
        #else
        Debug.Log("[WebAuth] Modo Editor/Standalone - Usando datos de prueba");
        SetUserData("test-token", "TestPlayer", "test@example.com");
        #endif
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterUnityMessageListener();

    private void RegisterMessageListener()
    {
        try
        {
            RegisterUnityMessageListener();
            Debug.Log("[WebAuth] Listener de mensajes registrado");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebAuth] Error al registrar listener: {e.Message}");
        }
    }
    #endif

    public void ReceiveUserData(string jsonData)
    {
        Debug.Log($"[WebAuth] Datos recibidos: {jsonData}");

        try
        {
            UserAuthData data = ParseUserAuthData(jsonData);

            if (data != null)
            {
                SetUserData(data.token, data.userName, data.userEmail);
                
                SendMessageToBrowser("USER_DATA_RECEIVED");
                
                // NUEVO: Enviar nombre al servidor inmediatamente
                SendNameToServer();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebAuth] Error al procesar datos: {e.Message}");
        }
    }

    private UserAuthData ParseUserAuthData(string json)
    {
        try
        {
            json = json.Trim('{', '}');
            var pairs = json.Split(',');
            
            UserAuthData data = new UserAuthData();
            
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':');
                if (keyValue.Length != 2) continue;
                
                string key = keyValue[0].Trim().Trim('"');
                string value = keyValue[1].Trim().Trim('"');
                
                switch (key)
                {
                    case "token":
                        data.token = value;
                        break;
                    case "userName":
                        data.userName = value;
                        break;
                    case "userEmail":
                        data.userEmail = value;
                        break;
                }
            }
            
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebAuth] Error al parsear JSON: {e.Message}");
            return null;
        }
    }

    private void SetUserData(string token, string userName, string userEmail)
    {
        UserToken = token;
        UserName = !string.IsNullOrEmpty(userName) ? userName : "Player";
        UserEmail = userEmail;
        IsAuthenticated = !string.IsNullOrEmpty(token);

        Debug.Log($"[WebAuth] Usuario autenticado: {UserName} ({UserEmail})");

        OnUserDataReceived?.Invoke(UserToken, UserName, UserEmail);

        if (playerNameDisplay != null)
        {
            playerNameDisplay.SetPlayerName(UserName);
        }
        else
        {
            playerNameDisplay = FindObjectOfType<PlayerNameDisplay>();
            if (playerNameDisplay != null)
            {
                playerNameDisplay.SetPlayerName(UserName);
            }
        }
    }

    // NUEVO MÉTODO: Enviar nombre al servidor
    private void SendNameToServer()
    {
        // Esperar hasta que NetworkManager esté listo
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("[WebAuth] NetworkManager no está listo, esperando...");
            StartCoroutine(WaitAndSendName());
            return;
        }

        if (SessionManager.Instance != null)
        {
            Debug.Log($"[WebAuth] Enviando nombre '{UserName}' al servidor");
            SessionManager.Instance.SetPlayerNameServerRpc(UserName);
        }
    }

    private System.Collections.IEnumerator WaitAndSendName()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            SendNameToServer();
        }
        else
        {
            Debug.LogError("[WebAuth] Timeout esperando NetworkManager");
        }
    }

    private void SendMessageToBrowser(string messageType)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string message = $"{{\"type\":\"{messageType}\"}}";
            SendMessageToParent(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebAuth] Error al enviar mensaje: {e.Message}");
        }
        #endif
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SendMessageToParent(string message);
    #endif

    public static string GetUserName()
    {
        var receiver = FindObjectOfType<WebAuthReceiver>();
        return receiver != null ? receiver.UserName : "Player";
    }

    public static bool IsUserAuthenticated()
    {
        var receiver = FindObjectOfType<WebAuthReceiver>();
        return receiver != null && receiver.IsAuthenticated;
    }
}

[Serializable]
public class UserAuthData
{
    public string token;
    public string userName;
    public string userEmail;
}