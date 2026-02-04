using UnityEngine;

/// <summary>
/// 게임 오디오 관리자
/// BGM과 SFX를 관리하며, 효과음 비교 테스트 기능 제공
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] private AudioClip lobbyBGM;
    [SerializeField] private AudioClip ingameBGM;

    [Header("BGM Settings")]
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float masterSfxVolume = 1f;

    [Header("=== SFX: Button (비교용) ===")]
    [SerializeField] private AudioClip[] buttonClips;
    [SerializeField] [Range(0, 4)] private int currentButtonIndex = 0;
    [SerializeField] [Range(0f, 2f)] private float buttonVolume = 1f;

    [Header("=== SFX: Pop/Select (비교용) ===")]
    [SerializeField] private AudioClip[] popClips;
    [SerializeField] [Range(0, 4)] private int currentPopIndex = 0;
    [SerializeField] [Range(0f, 2f)] private float popVolume = 1f;

    [Header("Pop Pitch Settings")]
    [SerializeField] private bool enablePopPitchIncrease = true;
    [SerializeField] [Range(0.5f, 1.2f)] private float popBasePitch = 0.9f;
    [SerializeField] [Range(0.05f, 0.3f)] private float popPitchIncrement = 0.12f;
    [SerializeField] [Range(1f, 2f)] private float popMaxPitch = 1.6f;

    [Header("Deselect Sound Settings")]
    [SerializeField] private bool enableDeselectSound = true;
    [SerializeField] [Range(0f, 2f)] private float deselectVolume = 0.7f;
    [SerializeField] [Range(0.5f, 1f)] private float deselectPitchMultiplier = 0.85f;

    [Header("=== SFX: Cell Destroy 1 (비교용) ===")]
    [SerializeField] private AudioClip[] cellDestroyClips;
    [SerializeField] [Range(0, 4)] private int currentCellDestroyIndex = 0;
    [SerializeField] [Range(0f, 2f)] private float cellDestroyVolume = 1f;

    [Header("=== SFX: Cell Destroy 2 (동시 재생) ===")]
    [SerializeField] private bool enableCellDestroy2 = true;
    [SerializeField] private AudioClip[] cellDestroyClips2;
    [SerializeField] [Range(0, 4)] private int currentCellDestroyIndex2 = 0;
    [SerializeField] [Range(0f, 2f)] private float cellDestroyVolume2 = 1f;

    [Header("=== SFX: Game Events ===")]
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] [Range(0f, 2f)] private float gameStartVolume = 1f;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] [Range(0f, 2f)] private float gameOverVolume = 1f;

    [Header("=== SFX: Hint ===")]
    [SerializeField] private AudioClip[] hintClips;
    [SerializeField] [Range(0, 4)] private int currentHintIndex = 0;
    [SerializeField] [Range(0f, 2f)] private float hintVolume = 0.5f;

    [Header("=== Game Over Panel A/B/C Test ===")]
    [SerializeField] private GameOverAudioMode gameOverAudioMode = GameOverAudioMode.LobbyBGM;
    [SerializeField] [Range(0.3f, 1f)] private float gameOverIngameVolumeMultiplier = 0.7f;
    [SerializeField] private AudioClip gameOverBGM;
    [SerializeField] [Range(0f, 3f)] private float gameOverBGMStartOffset = 0f;

    public enum GameOverAudioMode
    {
        LobbyBGM,       // A: 로비 BGM 재생
        IngameBGM,      // B: 인게임 BGM 볼륨 낮춰서 유지
        GameOverBGM     // C: 게임오버 전용 BGM
    }

    // Pop pitch tracking
    private int currentPopCount = 0;

    // 슬라이더 비율 (0.0~1.0)
    private float sfxSliderRatio = 1f;
    private float bgmSliderRatio = 1f;
    private float baseMasterSfxVolume;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            baseMasterSfxVolume = masterSfxVolume;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource 자동 생성
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // UIController 이벤트 구독
        if (UIController.Instance != null)
        {
            UIController.Instance.OnUIStateChanged += HandleUIStateChanged;
        }
    }

    private void Start()
    {
        // UIController가 늦게 초기화될 수 있으므로 다시 체크
        if (UIController.Instance != null)
        {
            UIController.Instance.OnUIStateChanged -= HandleUIStateChanged;
            UIController.Instance.OnUIStateChanged += HandleUIStateChanged;
        }

        // 초기 볼륨 설정
        bgmSource.volume = bgmVolume * bgmSliderRatio;

        // 로비 BGM 시작
        PlayBGM(lobbyBGM);
    }

    private void OnDestroy()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.OnUIStateChanged -= HandleUIStateChanged;
        }
    }
    public void SetSFX(float ratio)
    {
        sfxSliderRatio = ratio;
        masterSfxVolume = baseMasterSfxVolume * sfxSliderRatio;
    }

    public void SetBGM(float ratio)
    {
        bgmSliderRatio = ratio;
        bgmSource.volume = bgmVolume * bgmSliderRatio;
    }

    #region UI State Handling

    private void HandleUIStateChanged(UIController.UIState newState)
    {
        switch (newState)
        {
            case UIController.UIState.Lobby:
                PlayBGM(lobbyBGM);
                break;

            case UIController.UIState.InGame:
                PlayGameStartSFX();
                PlayBGM(ingameBGM, forceRestart: true);  // 리플레이 시 처음부터 재생
                break;

            case UIController.UIState.GameOver:
                PlayGameOverSFX();
                HandleGameOverAudio();
                break;
        }
    }

    private void HandleGameOverAudio()
    {
        switch (gameOverAudioMode)
        {
            case GameOverAudioMode.LobbyBGM:
                // A: 로비 BGM 재생
                PlayBGM(lobbyBGM);
                break;

            case GameOverAudioMode.IngameBGM:
                // B: 인게임 BGM 볼륨 낮춰서 유지
                bgmSource.volume = bgmVolume * gameOverIngameVolumeMultiplier * bgmSliderRatio;
                // 이미 인게임 BGM이 재생 중이므로 그대로 유지
                break;

            case GameOverAudioMode.GameOverBGM:
                // C: 게임오버 전용 BGM 재생
                if (gameOverBGM != null)
                {
                    PlayBGM(gameOverBGM);
                    // 시작 offset 적용
                    if (gameOverBGMStartOffset > 0f)
                    {
                        bgmSource.time = gameOverBGMStartOffset;
                    }
                }
                else
                {
                    // 게임오버 BGM 없으면 로비 BGM으로 폴백
                    PlayBGM(lobbyBGM);
                }
                break;
        }
    }

    #endregion

    #region BGM

    public void PlayBGM(AudioClip clip, bool forceRestart = false)
    {
        if (clip == null) return;

        // 같은 곡이 재생 중이면 스킵 (forceRestart가 true면 강제 재시작)
        if (bgmSource.clip == clip && bgmSource.isPlaying && !forceRestart)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume * bgmSliderRatio;
        bgmSource.Play();
    }

    #endregion

    #region SFX

    /// <summary>
    /// 기본 SFX 재생 (개별 볼륨 적용)
    /// </summary>
    private void PlaySFXWithVolume(AudioClip clip, float individualVolume, float pitch = 1f)
    {
        if (clip == null) return;

        // pitch 변경이 필요하면 임시 AudioSource 생성 (PlayOneShot은 pitch 무시함)
        if (Mathf.Abs(pitch - 1f) > 0.01f)
        {
            // 임시 GameObject + AudioSource 생성
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.SetParent(transform);
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.pitch = pitch;
            tempSource.volume = masterSfxVolume * individualVolume;
            tempSource.Play();

            // 재생 끝나면 자동 삭제
            Destroy(tempGO, clip.length / pitch + 0.1f);
        }
        else
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, masterSfxVolume * individualVolume);
        }
    }


    // === 버튼 효과음 ===
    public void PlayButtonSFX()
    {
        if (buttonClips != null && buttonClips.Length > 0 && currentButtonIndex < buttonClips.Length)
        {
            PlaySFXWithVolume(buttonClips[currentButtonIndex], buttonVolume);
        }
    }

    // === 셀 선택 (Pop) 효과음 ===
    /// <summary>
    /// 셀 선택 효과음 (pathCount로 pitch 조절)
    /// </summary>
    /// <param name="pathCount">현재 경로의 셀 개수 (1부터 시작)</param>
    public void PlayPopSFX(int pathCount = 1)
    {
        if (popClips == null || popClips.Length == 0 || currentPopIndex >= popClips.Length)
            return;

        float pitch = popBasePitch;
        if (enablePopPitchIncrease)
        {
            // pathCount가 높을수록 pitch 증가
            pitch = popBasePitch + (pathCount - 1) * popPitchIncrement;
            pitch = Mathf.Min(pitch, popMaxPitch);
        }

        currentPopCount = pathCount;
        PlaySFXWithVolume(popClips[currentPopIndex], popVolume, pitch);
    }

    /// <summary>
    /// Pop pitch 리셋 (새 경로 시작 시)
    /// </summary>
    public void ResetPopPitch()
    {
        currentPopCount = 0;
    }

    /// <summary>
    /// 셀 선택 해제 효과음 (낮은 pitch로 재생)
    /// </summary>
    /// <param name="remainingPathCount">해제 후 남은 경로 셀 개수</param>
    public void PlayDeselectSFX(int remainingPathCount = 1)
    {
        if (!enableDeselectSound) return;
        if (popClips == null || popClips.Length == 0 || currentPopIndex >= popClips.Length)
            return;

        // 남은 셀 개수에 맞는 pitch 계산 후 낮춤
        float pitch = popBasePitch + (remainingPathCount - 1) * popPitchIncrement;
        pitch = Mathf.Min(pitch, popMaxPitch);
        pitch *= deselectPitchMultiplier; // 해제는 더 낮은 음

        PlaySFXWithVolume(popClips[currentPopIndex], deselectVolume, pitch);
    }

    // === 셀 파괴 효과음 (1 + 2 동시 재생) ===
    public void PlayCellDestroySFX()
    {
        // Cell Destroy 1
        if (cellDestroyClips != null && cellDestroyClips.Length > 0 && currentCellDestroyIndex < cellDestroyClips.Length)
        {
            PlaySFXWithVolume(cellDestroyClips[currentCellDestroyIndex], cellDestroyVolume);
        }

        // Cell Destroy 2 (동시 재생)
        if (enableCellDestroy2 && cellDestroyClips2 != null && cellDestroyClips2.Length > 0 && currentCellDestroyIndex2 < cellDestroyClips2.Length)
        {
            PlaySFXWithVolume(cellDestroyClips2[currentCellDestroyIndex2], cellDestroyVolume2);
        }
    }

    // === 게임 시작 효과음 ===
    public void PlayGameStartSFX()
    {
        PlaySFXWithVolume(gameStartClip, gameStartVolume);
    }

    // === 게임 오버 효과음 ===
    public void PlayGameOverSFX()
    {
        PlaySFXWithVolume(gameOverClip, gameOverVolume);
    }

    // === 힌트 효과음 (1회 재생) ===
    public void PlayHintSFX()
    {
        if (hintClips == null || hintClips.Length == 0 || currentHintIndex >= hintClips.Length)
            return;

        PlaySFXWithVolume(hintClips[currentHintIndex], hintVolume);
    }

    #endregion

    #region Editor Testing

#if UNITY_EDITOR
    [Header("=== 키보드 테스트 (Play 모드) ===")]
    [SerializeField] private string testInfo = "1:Button 2:Pop 3:Destroy 4:Start 5:Over 6:Deselect 7:Hint ←→:Index ↑↓:PathCount";

    private int testPathCount = 1;

    private void Update()
    {
        // 숫자키로 효과음 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayButtonSFX();
            Debug.Log($"[AudioManager] Button SFX #{currentButtonIndex + 1} 재생 (Vol: {buttonVolume})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayPopSFX(testPathCount);
            Debug.Log($"[AudioManager] Pop SFX #{currentPopIndex + 1} 재생 (Vol: {popVolume}, PathCount: {testPathCount})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayCellDestroySFX();
            Debug.Log($"[AudioManager] Cell Destroy SFX 1:#{currentCellDestroyIndex + 1}(Vol:{cellDestroyVolume}) + 2:#{currentCellDestroyIndex2 + 1}(Vol:{cellDestroyVolume2}, Enabled:{enableCellDestroy2})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayGameStartSFX();
            Debug.Log($"[AudioManager] Game Start SFX 재생 (Vol: {gameStartVolume})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlayGameOverSFX();
            Debug.Log($"[AudioManager] Game Over SFX 재생 (Vol: {gameOverVolume})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            PlayDeselectSFX(testPathCount);
            Debug.Log($"[AudioManager] Deselect SFX 재생 (Vol: {deselectVolume}, PathCount: {testPathCount})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            PlayHintSFX();
            Debug.Log($"[AudioManager] Hint SFX #{currentHintIndex + 1} 재생 (Vol: {hintVolume})");
        }

        // 방향키로 인덱스 변경
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentPopIndex = Mathf.Max(0, currentPopIndex - 1);
            Debug.Log($"[AudioManager] Pop Index: {currentPopIndex + 1}");
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentPopIndex = Mathf.Min(popClips.Length - 1, currentPopIndex + 1);
            Debug.Log($"[AudioManager] Pop Index: {currentPopIndex + 1}");
        }

        // 위아래 방향키로 pitch 테스트 (pathCount 시뮬레이션)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            testPathCount = Mathf.Min(testPathCount + 1, 15);
            Debug.Log($"[AudioManager] Test PathCount: {testPathCount}");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            testPathCount = Mathf.Max(1, testPathCount - 1);
            Debug.Log($"[AudioManager] Test PathCount: {testPathCount}");
        }
    }
#endif

    #endregion
}
