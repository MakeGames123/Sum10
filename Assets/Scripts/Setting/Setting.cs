using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Unity.VisualScripting;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    public Toggle vibrationToggle;
    public Button homeButton;
    public RectTransform settingButton;
    [SerializeField] Button linkButton;
    [SerializeField] Button button1;
    [SerializeField] Button button2;
    [SerializeField] Button retryButton;
    [SerializeField] GameManager game;
    [SerializeField] BoardManager board;
    RectTransform rect;
    const string MASTER_VOL_KEY = "MASTER_VOLUME";
    const string MUSIC_VOL_KEY = "MUSIC_VOLUME";
    const string VIBRATION_KEY = "VIBRATION";
    Vector2 disablePos = new Vector2(9999, 9999);
    Vector2 originPos = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        rect = GetComponent<RectTransform>();
        LoadSettings();
        BindUI();
        homeButton.onClick.AddListener(game.ForceEnd);
        retryButton.onClick.AddListener(Hide);
    }
    void Start()
    {
        originPos = settingButton.position;
        ApplySettings();
    }
    public void Show()
    {
        rect.anchoredPosition = Vector2.zero;

        if (game.isRunning)
        {
            Time.timeScale = 0;
            if (!game.isTutorial)
            {
                homeButton.gameObject.SetActive(true);
                retryButton.gameObject.SetActive(true);
            }
            else
            {
                homeButton.gameObject.SetActive(false);
                retryButton.gameObject.SetActive(false);
            }
            linkButton.gameObject.SetActive(false);
            button1.gameObject.SetActive(false);
            button2.gameObject.SetActive(false);
            board.LockCells();
        }
        else
        {
            homeButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            linkButton.gameObject.SetActive(true);
            button1.gameObject.SetActive(true);
            button2.gameObject.SetActive(true);
        }
    }
    public void ResetPosition()
    {
        settingButton.position = originPos;
    }
    public void LowerPosition(float y)
    {
        settingButton.position = originPos - new Vector2(0, y);
    }
    public void Hide()
    {
        Time.timeScale = 1;
        rect.anchoredPosition = disablePos;
        board.UnlockCells();
    }
    void BindUI()
    {
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        vibrationToggle.onValueChanged.AddListener(SetVibration);
    }

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MASTER_VOL_KEY, value);
        value /= 9f;
        AudioManager.Instance.SetSFX(value);
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, value);
        value /= 9f;
        AudioManager.Instance.SetBGM(value);
    }
    public void SetVibration(bool isOn)
    {
        PlayerPrefs.SetInt(VIBRATION_KEY, isOn ? 1 : 0);
    }

    public bool IsVibrationOn()
    {
        return PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
    }

    void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 5f);
        float music = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 5f);
        bool vibration = PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;

        masterVolumeSlider.value = master;
        musicVolumeSlider.value = music;
        vibrationToggle.isOn = vibration;
    }

    void ApplySettings()
    {
        SetMasterVolume(masterVolumeSlider.value);
        SetMusicVolume(musicVolumeSlider.value);
        SetVibration(vibrationToggle.isOn);
    }
}
