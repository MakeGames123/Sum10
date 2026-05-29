using UnityEngine;
using TMPro;

public class LocalizeText : MonoBehaviour
{
    [Header("시트상의 ID 입력")]
    [SerializeField] private int stringId;

    private TMP_Text textComponent;

    void Awake()
    {
        // UI용 TextMeshProUGUI와 월드(3D)용 TextMeshPro 컴포넌트를 모두 호환하기 위해 상위 클래스(TMP_Text)로 가져옵니다.
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // 매니저에 이벤트 등록 (시트가 로드 완료되거나 언어가 바뀌면 자동 호출됨)
        if (LocalizationLoader.Instance == null)
        {
            FindAnyObjectByType<LocalizationLoader>().OnLocalizationLoaded += ApplyLocalization;
        }
        else if (LocalizationLoader.Instance.isLoaded)
        {
            ApplyLocalization(); // 이미 로드가 끝나있는 상태라면 바로 적용
        }
        else
        {
            LocalizationLoader.Instance.OnLocalizationLoaded += ApplyLocalization;
        }
    }
    // 실제 텍스트를 변경하는 핵심 함수
    public void ApplyLocalization()
    {
        if (textComponent == null) return;

        if (LocalizationLoader.Instance != null)
        {
            textComponent.text = LocalizationLoader.Instance.GetText(stringId);
        }
    }

    void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (LocalizationLoader.Instance != null)
        {
            LocalizationLoader.Instance.OnLocalizationLoaded -= ApplyLocalization;
        }
    }

}