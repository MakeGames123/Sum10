using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
public class ProfileGroup : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image profileImage;
    public ProfilePanel panel;
    [SerializeField] TextMeshProUGUI nickName;
    [SerializeField] TextMeshProUGUI rank;
    [SerializeField] GameManager game;
    [SerializeField] ScoreManager scoreManager;
    public PlayFabLoginManager login;
    void Awake()
    {
        // 인스펙터 미할당 안전장치: 씬에서 자동 탐색
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        login.onLogined.AddListener(UpdateProfile);
        game.OnGameOver.AddListener((value) => UpdateProfile());

        // 랭킹 갱신 이벤트 구독 - 새로고침 버튼/게임오버 후 Top50 갱신 시 탑바 순위도 같이 갱신
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated += UpdateRank;
            scoreManager.OnOverallTop50Updated += UpdateRank;
            // 내 순위가 계산되면 즉시 반영 (PlayFab Position 갱신 지연 우회)
            scoreManager.OnMyRankUpdated += HandleMyRankUpdated;
        }
        else
        {
            Debug.LogWarning("[ProfileGroup] ScoreManager 참조를 찾지 못해 탑바 순위 갱신 이벤트 구독 실패");
        }
    }

    void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated -= UpdateRank;
            scoreManager.OnOverallTop50Updated -= UpdateRank;
            scoreManager.OnMyRankUpdated -= HandleMyRankUpdated;
        }
    }

    private void HandleMyRankUpdated(int newRank)
    {
        // ScoreManager가 계산한 최신 순위를 즉시 표시 (API 지연 우회)
        rank.text = "#" + newRank.ToString();
    }
    public void OnPointerClick(PointerEventData data)
    {
        AudioManager.Instance?.PlayButtonSFX();
        panel.gameObject.SetActive(true);
        panel.SetCondition();
    }
    public void UpdateProfile()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                nickName.text = result.AccountInfo.TitleInfo.DisplayName;
                if (int.TryParse(result.AccountInfo.TitleInfo.AvatarUrl, out int index)) profileImage.sprite = ProfileList.Instance.profileList[index];//기본프로필 시작 : 저장된 프로필 시작
                else profileImage.sprite = ProfileList.Instance.profileList[0];
                PlayerData.Instance.EquippedProfileImage = index;
                PlayerData.Instance.nickName = result.AccountInfo.TitleInfo.DisplayName;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
        DelayUpdateRank();
    }
    public void DelayUpdateRank()
    {
        Invoke(nameof(UpdateRank), 1);
    }
    public void UpdateRank()
    {
        // 안전장치: 늦게 활성화된 케이스 대응
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (scoreManager == null)
        {
            Debug.LogWarning("[ProfileGroup.UpdateRank] scoreManager 못 찾음");
            rank.text = "#--";
            return;
        }

        Debug.Log($"[ProfileGroup.UpdateRank] cached={scoreManager.CachedRank} weekly={scoreManager.MyWeeklyRank} weeklyScore={scoreManager.MyWeeklyScore}");

        // 우선순위 1: 게임오버 직후 CalculateRank로 계산된 정확한 순위 (1회용)
        if (scoreManager.CachedRank > 0)
        {
            rank.text = "#" + scoreManager.CachedRank.ToString();
            return;
        }

        // 우선순위 2: FetchTop50과 함께 갱신된 내 주간 기록 캐시 (Top50 밖이어도 정확)
        if (scoreManager.MyWeeklyRank > 0)
        {
            rank.text = "#" + scoreManager.MyWeeklyRank.ToString();
            return;
        }

        // 캐시가 없으면 표시 보류 (다음 FetchTop50 완료 시 OnTop50Updated로 갱신됨)
        rank.text = "#--";
    }
}