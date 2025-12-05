using UnityEngine;
using Unity.Netcode;

/// <summary>
/// This component is responsible for destroying a GameObject when it goes off-screen.
/// It uses the camera's viewport to check whether the object is off-screen and, if so, requests its despawn.
/// </summary>
public class DestroyOffScreen : NetworkBehaviour
{
    #region Private Fields

    private Camera mainCamera;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Unity callback invoked when the script is first run.
    /// Initializes the main camera reference.
    /// </summary>
    private void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Unity callback invoked once per frame.
    /// Checks if the object is off-screen and handles its despawn either locally or on the server.
    /// </summary>
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

    #endregion

    #region ServerRPC Methods

    /// <summary>
    /// ServerRpc that requests the server to despawn the object.
    /// Only the object owner can call this method.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void RequestDespawnServerRpc()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    #endregion
}