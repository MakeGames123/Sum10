using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
public interface IMainPanel
{
    void SetCondition();
    public void OnDisable();
}
public class WorldScorePanel : MonoBehaviour, IMainPanel
{
    public List<WorldScoreUnit> units = new();
    public List<PodiumUnit> podiumUnits = new();
    public GameManager gameManager;
    public ScoreManager scoreManager;

    private void Awake()
    {
        // OnGameOver 리스너는 1회만 등록
        gameManager.OnGameOver.AddListener((val) => RefreshFromCache());
    }

    private void Start()
    {
        if (scoreManager != null)
            scoreManager.OnTop50Updated += RefreshFromCache;
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.OnTop50Updated -= RefreshFromCache;
    }

    // IMainPanel: 탭 클릭 시 호출
    public void SetCondition()
    {
        RefreshFromCache();
    }

    public void OnDisable()
    {
    }

    private void RefreshFromCache()
    {
        ClearUI();

        if (scoreManager == null)
        {
            Debug.Log("[WorldScorePanel] scoreManager가 null");
            return;
        }

        if (!scoreManager.IsTop50Ready)
        {
            Debug.Log("[WorldScorePanel] 캐시 비어있음 → FetchTop50 트리거");
            _ = scoreManager.FetchTop50WithProfilesAsync();
            return;
        }

        Debug.Log($"[WorldScorePanel] 캐시에서 표시: {scoreManager.CachedTop50.Count}명");
        DrawLeaderboard(scoreManager.CachedTop50);
    }

    private void DrawLeaderboard(List<PlayerLeaderboardEntry> leaderboard)
    {
        string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;
        int uiIndex = 0;

        PlayerLeaderboardEntry myEntry =
            leaderboard.Find(e => e.PlayFabId == myPlayFabId);

        // 내 점수
        if (myEntry != null && units.Count > 0)
        {
            units[0].SetCondition(
                myEntry.Position + 1,
                string.IsNullOrEmpty(myEntry.DisplayName)
                    ? myEntry.PlayFabId
                    : myEntry.DisplayName,
                myEntry.StatValue,
                PlayerData.Instance.EquippedProfileImage
            );
            units[0].gameObject.SetActive(true);
            uiIndex = 1;
        }

        // 월드 랭킹
        foreach (var entry in leaderboard)
        {
            if (uiIndex >= units.Count) break;

            units[uiIndex].SetCondition(
                entry.Position + 1,
                string.IsNullOrEmpty(entry.DisplayName)
                    ? entry.PlayFabId
                    : entry.DisplayName,
                entry.StatValue,
                scoreManager.GetProfileIndex(entry.PlayFabId)
            );
            units[uiIndex].gameObject.SetActive(true);

            uiIndex++;
        }

        // 시상대
        for (int i = 0; i < 3 && i < leaderboard.Count; i++)
        {
            var entry = leaderboard[i];

            podiumUnits[i].SetCondition(
                string.IsNullOrEmpty(entry.DisplayName)
                    ? entry.PlayFabId
                    : entry.DisplayName,
                scoreManager.GetProfileIndex(entry.PlayFabId)
            );
        }
    }

    private void ClearUI()
    {
        foreach (var unit in units)
        {
            unit.gameObject.SetActive(false);
        }
    }
}
