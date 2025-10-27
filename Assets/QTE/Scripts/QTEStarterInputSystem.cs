using UnityEngine;
using UnityEngine.InputSystem;

public class QTEStarterInputSystem : MonoBehaviour
{
    public QTEManagerInputSystem qteManager;

    void Awake()
    {
        if (qteManager == null)
            Debug.LogError("Asigna QTEManager en el inspector de QTEStarter.");
    }

    // Este script no necesita Update; el QTEManager ya tiene la Start action registrada.
    // Pero dejamos un método público para poder iniciar desde otros scripts si se requiere.
    public void TriggerStartQTE()
    {
        qteManager?.StartQTE();
    }
}
