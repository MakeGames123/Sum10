using System.Collections;
using UnityEngine;

/// <summary>
/// 언어별 레이아웃 변형 스위처.
/// KR / EN 각각 별도 GameObject 변형을 만들어 인스펙터에서 할당하면,
/// 현재 언어에 맞는 변형만 활성화되고 나머지는 비활성화된다.
/// </summary>
public class LocaleVariant : MonoBehaviour
{
    [SerializeField] private GameObject krVariant;
    [SerializeField] private GameObject enVariant;

    void OnEnable()
    {
        if (LocalizationLoader.Instance != null)
        {
            Apply();
            LocalizationLoader.Instance.OnLocalizationLoaded += Apply;
        }
        else
        {
            // LocalizationLoader.Awake가 아직 안 돈 케이스
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
        string lang = LocalizationLoader.Instance.CurrentLanguage.ToLower();
        bool isKr = lang == "kr";
        if (krVariant != null) krVariant.SetActive(isKr);
        if (enVariant != null) enVariant.SetActive(!isKr);
    }
}
