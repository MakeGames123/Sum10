using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedLogo : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite krSprite;
    [SerializeField] private Sprite enSprite;

    void Reset()
    {
        // 같은 오브젝트에 Image가 있으면 자동 할당
        targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (LocalizationLoader.Instance != null)
        {
            Apply();
            LocalizationLoader.Instance.OnLocalizationLoaded += Apply;
        }
        else
        {
            // LocalizationLoader.Awake가 아직 안 돈 케이스 (Script Execution Order 의존 회피)
            StartCoroutine(WaitForLoader());
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (LocalizationLoader.Instance != null)
            LocalizationLoader.Instance.OnLocalizationLoaded -= Apply;
    }

    IEnumerator WaitForLoader()
    {
        while (LocalizationLoader.Instance == null)
            yield return null;
        Apply();
        LocalizationLoader.Instance.OnLocalizationLoaded += Apply;
    }

    void Apply()
    {
        if (targetImage == null) return;
        string lang = LocalizationLoader.Instance.CurrentLanguage.ToLower();
        targetImage.sprite = lang == "kr" ? krSprite : enSprite;
    }
}
