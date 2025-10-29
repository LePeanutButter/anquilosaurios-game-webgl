using System;
using UnityEngine;

public static class PlayerEvents
{
    public static event Action<ulong> OnPlayerDied;

    public static void RaisePlayerDied(ulong playerId)
    {
        Debug.Log($"[Events] Player {playerId} died");
        OnPlayerDied?.Invoke(playerId);
    }
}