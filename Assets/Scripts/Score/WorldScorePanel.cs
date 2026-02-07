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
    private const string PROFILE_INDEX_KEY = "ProfileIndex";
    private Dictionary<string, int> profileIndexCache = new();
    private const string TargetStatistic = "HighScore";
    bool init = false;
    public GameManager gameManager;
    public void SetCondition()
    {
        gameManager.OnGameOver.AddListener((val) => LoadWorldLeaderboard());
        UpdateRank();
    }
    public void OnDisable()
    {
    }

    public void UpdateRank()
    {
        ClearUI();
        LoadWorldLeaderboard();
    }
    private void LoadWorldLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = TargetStatistic,
            StartPosition = 0,
            MaxResultsCount = 50,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;

            if (!init)
            {
                PreloadProfileIndices(result.Leaderboard, () =>
                {
                    DrawLeaderboard(result.Leaderboard, myPlayFabId);
                });
                init = true;
            }
            else
            {
                DrawLeaderboard(result.Leaderboard, myPlayFabId);
            }
        }, error =>
        {
            Debug.LogError("리더보드 로드 실패: " + error.GenerateErrorReport());
        });
    }
    private void DrawLeaderboard(
        List<PlayerLeaderboardEntry> leaderboard,
        string myPlayFabId)
    {
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
                profileIndexCache[entry.PlayFabId]
            );
            units[uiIndex].gameObject.SetActive(true);

            uiIndex++;
        }

        // 시상대
        for (int i = 0; i < 3; i++)
        {
            var entry = leaderboard[i];

            podiumUnits[i].SetCondition(
                string.IsNullOrEmpty(entry.DisplayName)
                    ? entry.PlayFabId
                    : entry.DisplayName,
                profileIndexCache[entry.PlayFabId]
            );
        }
    }

    private void PreloadProfileIndices(
        List<PlayerLeaderboardEntry> entries,
        System.Action onComplete)
    {
        int remaining = entries.Count;

        if (remaining == 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (var entry in entries)
        {
            string playFabId = entry.PlayFabId;

            PlayFabClientAPI.GetUserData(
                new GetUserDataRequest
                {
                    PlayFabId = playFabId,
                    Keys = new List<string> { PROFILE_INDEX_KEY }
                },
                result =>
                {
                    int index = 0;

                    if (result.Data != null &&
                        result.Data.TryGetValue(PROFILE_INDEX_KEY, out var data))
                    {
                        int.TryParse(data.Value, out index);
                    }

                    profileIndexCache[playFabId] = index;

                    if (--remaining == 0)
                        onComplete?.Invoke();
                },
                error =>
                {
                    // 실패해도 기본값
                    profileIndexCache[playFabId] = 0;

                    if (--remaining == 0)
                        onComplete?.Invoke();
                }
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
