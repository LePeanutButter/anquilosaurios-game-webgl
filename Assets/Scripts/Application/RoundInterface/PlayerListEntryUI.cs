using TMPro;
using UnityEngine;
using Unity.Collections;

public class PlayerListEntryUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text deathCountText;
    public TMP_Text roundWinsText;

    private PlayerState boundPlayerState;

    public void SetPlayerState(PlayerState ps)
    {
        if (boundPlayerState != null)
        {
            boundPlayerState.DeathCount.OnValueChanged -= OnDeathChanged;
            boundPlayerState.RoundWins.OnValueChanged -= OnRoundWinsChanged;
            boundPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
        }

        boundPlayerState = ps;
        if (ps == null) return;

        nameText.text = string.IsNullOrEmpty(ps.PlayerName.Value.ToString())
        ? ps.PlayerId.ToString()
        : ps.PlayerName.Value.ToString();
        deathCountText.text = ps.DeathCount.Value.ToString();
        roundWinsText.text = ps.RoundWins.Value.ToString();

        ps.DeathCount.OnValueChanged += OnDeathChanged;
        ps.RoundWins.OnValueChanged += OnRoundWinsChanged;
        ps.PlayerName.OnValueChanged += OnNameChanged;
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
        ? boundPlayerState.PlayerId.ToString()
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
