using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine.Events;
/// <summary>
/// 게임오버 패널
/// 자신의 표시/숨김과 이벤트 발생만 담당 (Single Responsibility)
/// UI 전환은 UIController가 처리
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("Panel Elements")]
    [SerializeField] private TMP_Text bigScoreText;         // 큰 점수 텍스트
    [SerializeField] private TMP_Text bestScoreText;        // 베스트 스코어 텍스트
    [SerializeField] private TMP_Text globalRankText;       // 글로벌 랭킹 텍스트

    [Header("Buttons")]
    public Button replayButton;           // 리플레이 버튼
    public Button homeButton;             // 홈 버튼

    [SerializeField] private GameManager gameManager;             // 홈 버튼
    [SerializeField] private List<GameOverScoreUnit> units;             // 홈 버튼

    public ScoreManager scoreManager;
    RectTransform myRect;
    Vector2 disablePos = new Vector2(-9999, 9999);
    int finalScore;
    private void Awake()
    {
        myRect = GetComponent<RectTransform>();
        // 버튼 이벤트 코드로 할당 (매뉴얼 규칙)
        if (replayButton != null) replayButton.onClick.AddListener(Hide); // Button_Replay onClick

        if (homeButton != null) homeButton.onClick.AddListener(Hide); // Button_Home onClick

        gameManager.OnGameOver.AddListener(SetCondition);
    }
    private async void SetCondition(int finalScore)
    {
        bool success = await scoreManager.SubmitScoreAsync(finalScore);
        this.finalScore = finalScore;
        myRect.anchoredPosition = Vector2.zero;

        if (success)
        {
            GetAroundMyRank();
        }
    }

    public void UpdateUI(List<PlayerLeaderboardEntry> result)
    {
        gameObject.SetActive(true);

        if (bigScoreText != null)
            bigScoreText.text = finalScore.ToString();

        int myIndex = -1;
        string myId = PlayFabSettings.staticPlayer.PlayFabId;

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].PlayFabId == myId)
            {
                myIndex = i;
                break;
            }
        }

        List<PlayerLeaderboardEntry> unitResult = new();
        for (int i = myIndex - 1, j = 1; i > 0 && j < 4; i--, j++)
        {
            unitResult.Add(result[myIndex - j]);
        }
        unitResult.Reverse();

        int upperCount = unitResult.Count;
        for (int i = 0; i < 6 - upperCount; i++)
        {
            unitResult.Add(result[i + myIndex]);
        }

        for (int i = 0; i < 6; i++)
        {
            units[i].SetCondition(unitResult[i].Position + 1, unitResult[i].StatValue, unitResult[i].PlayFabId, i == upperCount);
        }


        if (bestScoreText != null) bestScoreText.text = result[myIndex].StatValue.ToString();
        if (globalRankText != null) globalRankText.text = (result[myIndex].Position + 1).ToString();
    }

    public void Hide()
    {
        myRect.anchoredPosition = disablePos;
    }
    public void GetAroundMyRank()
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "HighScore", // 리더보드 통계 이름
            MaxResultsCount = 11      // 위 5 + 나 + 아래 5
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            request,
            OnSuccess,
            OnError
        );
    }
    void OnSuccess(GetLeaderboardAroundPlayerResult result)
    {
        UpdateUI(result.Leaderboard);
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError($"리더보드 조회 실패: {error.GenerateErrorReport()}");
    }
}
