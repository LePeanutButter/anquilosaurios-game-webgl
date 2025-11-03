using TMPro;
using Unity.Collections;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerListEntryUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text deathCountText;
    public TMP_Text roundWinsText;

    private PlayerState boundPlayerState;

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

    private void OnDeathChanged(int prev, int current)
    {
        deathCountText.text = current.ToString();
    }

    private void OnRoundWinsChanged(int prev, int current)
    {
        roundWinsText.text = current.ToString();
    }

    private void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes cur)
    {
        nameText.text = string.IsNullOrEmpty(cur.ToString())
            ? AuthenticationService.Instance.PlayerId
            : cur.ToString();
    }

    private void OnDestroy()
    {
        if (boundPlayerState != null)
        {
            boundPlayerState.DeathCount.OnValueChanged -= OnDeathChanged;
            boundPlayerState.RoundWins.OnValueChanged -= OnRoundWinsChanged;
            boundPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
        }
    }
}
