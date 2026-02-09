using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PodiumUnit : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI IDText;
    [SerializeField] Image profile;
    [SerializeField] Image crown;
    [SerializeField] GameObject profileGroup;
    [SerializeField] List<Sprite> profiles;
    public void SetCondition(string id, int profileIndex)
    {
        IDText.text = id;

        if (profileIndex == -1)
        {
            crown.enabled = false;
            profileGroup.SetActive(false);
        }
        else
        {
            crown.enabled = true;
            profileGroup.SetActive(true);
            profile.sprite = profiles[profileIndex];
        }
    }
}
