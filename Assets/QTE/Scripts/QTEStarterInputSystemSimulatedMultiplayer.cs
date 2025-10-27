using UnityEngine;
using UnityEngine.InputSystem;

public class QTEStarterInputSystemSimulatedMultiplayer : MonoBehaviour
{
    public QTESimulatedMultiplayerInputSystem qteManager;

    void Update()
    {
        // Este script usa el Input System Start action si prefieres, pero aquí usamos el viejo Update para simplicidad:
        // Mantén esto solo para debug local: presionar Space inicia el QTE
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            qteManager?.StartQTE();
        }
    }
}
