using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScoreUnit : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI playerId;
    [SerializeField] private Image arrow;

    public void SetCondition(int rank, int score, string id)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (scoreText != null) scoreText.text = score.ToString();
        if (playerId != null) playerId.text = id;
        if (arrow != null) arrow.enabled = true;
    }

    /// <summary>
    /// 순위 숫자만 업데이트 (Swap 애니메이션용)
    /// </summary>
    public void SetRank(int rank)
    {
        if (rankText != null) rankText.text = rank.ToString();
    }
}
