using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Componente que va en el prefab de jugador (NetworkObject). Solo el cliente local con IsOwner escuchará el Input System.
/// Al presionar Submit, llama al ServerRpc del manager para registrar la respuesta.
/// </summary>
public class QTEPlayerInput : NetworkBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActionsAsset; // arrastra Assets/QTE/Input/QTEInputActions

    private InputAction submitAction;

    void Awake()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogWarning("Asigna QTEInputActions en QTEPlayerInput (prefab de player).");
            return;
        }

        // encuentra la acción Submit dentro del map QTE
        var map = inputActionsAsset.FindActionMap("QTE");
        if (map == null)
        {
            Debug.LogWarning("No se encontró ActionMap 'QTE' en el InputActionAsset.");
            return;
        }
        submitAction = map.FindAction("Submit");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Solo habilitar input para el cliente local (owner)
        if (IsOwner)
        {
            if (submitAction != null)
            {
                submitAction.performed += OnSubmitPerformed;
                submitAction.Enable();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (submitAction != null)
        {
            submitAction.performed -= OnSubmitPerformed;
            submitAction.Disable();
        }
    }

    private void OnSubmitPerformed(InputAction.CallbackContext ctx)
    {
        // Llamar al ServerRpc del manager singleton
        if (QTEMultiplayerManagerNetwork.Instance != null)
        {
            // Llamamos al ServerRpc que registrará el submit (RequireOwnership=false en el manager)
            QTEMultiplayerManagerNetwork.Instance.SubmitQTEServerRpc();
        }
    }
}
