using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 클릭 효과음을 추가하는 컴포넌트
/// Button 컴포넌트가 있는 오브젝트에 추가하면 자동으로 연결됨
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonSFX();
        }
    }
}
