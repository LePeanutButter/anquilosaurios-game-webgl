using TMPro;
using Unity.Collections;
using Unity.Services.Authentication;
using UnityEngine;

/// <summary>
/// Represents a single player's entry in the player list UI,
/// showing the player's name, death count, and round wins.
/// </summary>
public class PlayerListEntryUI : MonoBehaviour
{
    #region UI References

    public TMP_Text nameText;
    public TMP_Text deathCountText;
    public TMP_Text roundWinsText;

    #endregion

    #region Private Fields

    private PlayerState boundPlayerState;

    #endregion

    #region PlayerState Binding

    /// <summary>
    /// Binds a <see cref="PlayerState"/> to this UI entry.
    /// Updates the UI text fields and subscribes to value change events.
    /// </summary>
    /// <param name="state">The <see cref="PlayerState"/> to bind. Pass null to unbind.</param>
    public void SetPlayerState(PlayerState state)
    {
        if (boundPlayerState != null)
        {
            boundPlayerState.DeathCount.OnValueChanged -= OnDeathChanged;
            boundPlayerState.RoundWins.OnValueChanged -= OnRoundWinsChanged;
            boundPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
        }

        boundPlayerState = state;
        if (boundPlayerState == null) return;

        nameText.text = string.IsNullOrEmpty(boundPlayerState.PlayerName.Value.ToString())
            ? AuthenticationService.Instance.PlayerId
            : boundPlayerState.PlayerName.Value.ToString();

        deathCountText.text = boundPlayerState.DeathCount.Value.ToString();
        roundWinsText.text = boundPlayerState.RoundWins.Value.ToString();

        boundPlayerState.DeathCount.OnValueChanged += OnDeathChanged;
        boundPlayerState.RoundWins.OnValueChanged += OnRoundWinsChanged;
        boundPlayerState.PlayerName.OnValueChanged += OnNameChanged;
    }

    #endregion

    #region UI Update Callbacks

    /// <summary>
    /// Updates the death count UI text when the player's death count changes.
    /// </summary>
    /// <param name="prev">The previous death count.</param>
    /// <param name="current">The new death count.</param>
    private void OnDeathChanged(int prev, int current)
    {
        deathCountText.text = current.ToString();
    }

    /// <summary>
    /// Updates the round wins UI text when the player's round wins change.
    /// </summary>
    /// <param name="prev">The previous round wins count.</param>
    /// <param name="current">The new round wins count.</param>
    private void OnRoundWinsChanged(int prev, int current)
    {
        roundWinsText.text = current.ToString();
    }

    /// <summary>
    /// Updates the player name UI text when the player's name changes.
    /// Falls back to the authentication player ID if the name is empty.
    /// </summary>
    /// <param name="prev">The previous player name.</param>
    /// <param name="cur">The new player name.</param>
    private void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes cur)
    {
        nameText.text = string.IsNullOrEmpty(cur.ToString())
            ? AuthenticationService.Instance.PlayerId
            : cur.ToString();
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Cleans up subscriptions to the PlayerState events when this UI entry is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (boundPlayerState != null)
        {
            boundPlayerState.DeathCount.OnValueChanged -= OnDeathChanged;
            boundPlayerState.RoundWins.OnValueChanged -= OnRoundWinsChanged;
            boundPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
        }
    }

    #endregion
}
