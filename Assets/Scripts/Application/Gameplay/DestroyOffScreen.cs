using UnityEngine;
using Unity.Netcode;

public class DestroyOffScreen : NetworkBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);

        if (screenPos.y < 0)
        {
            if (IsServer)
            {
                NetworkObject.Despawn();
            }
            else if (IsOwner)
            {
                RequestDespawnServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestDespawnServerRpc()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}