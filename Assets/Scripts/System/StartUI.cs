using UnityEngine;
using UnityEngine.UI;

public class StartUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked); // StartButton onClick
    }

    private void OnStartClicked()
    {
        // UIController에 게임 시작 요청 위임
        if (UIController.Instance != null)
        {
            UIController.Instance.RequestStartGame();
        }
    }
}
