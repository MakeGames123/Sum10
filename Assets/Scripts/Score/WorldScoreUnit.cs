using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class WorldScoreUnit : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI IDText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] Image background;
    [SerializeField] List<Sprite> backgrounds;
    [SerializeField] Image badge;
    [SerializeField] List<Sprite> badges;
    public void SetCondition(int rank, string id, int score)
    {
        if (!string.IsNullOrEmpty(id) && id.Length > 6)
        {
            // 6글자까지 자르고 뒤에 .. 추가 (예: ABCDEF..)
            IDText.text = id.Substring(0, 6) + "..";
        }
        else
        {
            IDText.text = id;
        }
        scoreText.text = score.ToString();

        if (rank > 3)
        {
            badge.enabled = false;
            rankText.enabled = true;
            rankText.text = rank.ToString();
            background.sprite = backgrounds[3];
        }
        else
        {
            badge.enabled = true;
            rankText.enabled = false;
            badge.sprite = badges[rank - 1];
        }
    }
}
