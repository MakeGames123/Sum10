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
    [SerializeField] Image profile;
    [SerializeField] GameObject profileGroup;
    [SerializeField] List<Sprite> profiles;
    [SerializeField] List<Sprite> badges;
    public void SetCondition(int rank, string id, int score, int profileIndex)
    {
        IDText.text = id;

        if (score == -1) scoreText.text = "---";
        else scoreText.text = score.ToString();

        if (score == -1) profileGroup.SetActive(false);
        else
        {
            profileGroup.SetActive(true);
            profile.sprite = profiles[profileIndex];
        }


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
            background.sprite = backgrounds[rank - 1];
        }
    }
}
