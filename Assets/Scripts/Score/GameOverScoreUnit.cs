using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScoreUnit : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI playerId;
    [SerializeField] Image arrow;
    public void SetCondition(int rank, int score, string playerId, bool isMe)
    {
        rankText.text = rank.ToString();
        scoreText.text = score.ToString();
        this.playerId.text = playerId;

        if(isMe) arrow.enabled = true;
        else arrow.enabled = false;
    }
}
