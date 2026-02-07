using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PodiumUnit : MonoBehaviour
{
    
    [SerializeField] TextMeshProUGUI IDText;
    [SerializeField] Image profile;
    [SerializeField] List<Sprite> profiles;
    public void SetCondition(string id, int profileIndex)
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
        profile.sprite = profiles[profileIndex];
    }
}
