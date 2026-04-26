using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
public interface IMainPanel
{
    void SetCondition();
    public void OnDisable();
}
public class WorldScorePanel : MonoBehaviour, IMainPanel
{
    public WorldScoreUnit myUnit = new();
    public List<WorldScoreUnit> units = new();
    public List<PodiumUnit> podiumUnits = new();
    public GameManager gameManager;
    public ScoreManager scoreManager;
    public TextMeshProUGUI resetTimeText;
    public Image resetTimeIcon;
    public TextMeshProUGUI coolTimeText;
    public Button refreshButton;
    public Image weekButtonPressed;
    public Image allButtonPressed;
    public TextMeshProUGUI weekTextPressed;
    public TextMeshProUGUI allTextPressed;
    public TextMeshProUGUI weekText;
    public TextMeshProUGUI allText;
    private Coroutine coolTimeCoroutine;
    private const float CoolTimeDuration = 60f;
    bool isWeekly = true;
    private void Awake()
    {
        // OnGameOver 리스너는 1회만 등록
        gameManager.OnGameOver.AddListener((val) => RefreshFromCache());
        refreshButton.onClick.AddListener(RefreshCache);
    }
    private void Start()
    {
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated += RefreshFromCache;
            scoreManager.OnOverallTop50Updated += RefreshFromCache;
        }

        ChangeToWeek();
    }
    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated -= RefreshFromCache;
            scoreManager.OnOverallTop50Updated -= RefreshFromCache;
        }
    }
    public void ChangeToWeek()
    {
        if (!isWeekly)
        {
            isWeekly = true;
            resetTimeText.enabled = true;
            resetTimeIcon.enabled = true;

            weekButtonPressed.enabled = true;
            allButtonPressed.enabled = false;

            weekTextPressed.enabled = true;
            weekText.enabled = false;
            allTextPressed.enabled = false;
            allText.enabled = true;
            RefreshFromCache();
        }
    }
    public void ChangeToAll()
    {
        if (isWeekly)
        {
            isWeekly = false;
            resetTimeText.enabled = false;
            resetTimeIcon.enabled = false;

            allButtonPressed.enabled = true;
            weekButtonPressed.enabled = false;

            weekTextPressed.enabled = false;
            weekText.enabled = true;
            allTextPressed.enabled = true;
            allText.enabled = false;
            RefreshFromCache();
        }
    }
    public void SetCondition()
    {
    }

    public void OnDisable()
    {
    }
    public void RefreshCache()
    {
        refreshButton.gameObject.SetActive(false);

        if (coolTimeCoroutine != null)
            StopCoroutine(coolTimeCoroutine);

        coolTimeCoroutine = StartCoroutine(UpdateCoolTime());

        _ = scoreManager.FetchWeeklyTop50WithProfilesAsync();
        _ = scoreManager.FetchTop50WithProfilesAsync();
    }
    private IEnumerator UpdateCoolTime()
    {
        coolTimeText.enabled = true;

        float remaining = CoolTimeDuration;

        while (remaining > 0)
        {
            coolTimeText.text = $"{Mathf.CeilToInt(remaining)}초";
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        coolTimeText.enabled = false;
        refreshButton.gameObject.SetActive(true);
        coolTimeCoroutine = null;
    }

    private void RefreshFromCache()
    {
        ClearUI();

        if (scoreManager == null) return;

        DrawLeaderboard(isWeekly ? scoreManager.CachedWeeklyTop50 : scoreManager.CachedTop50);
    }

    private void DrawLeaderboard(List<PlayerLeaderboardEntry> leaderboard)
    {
        string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;
        int uiIndex = 0;

        PlayerLeaderboardEntry myEntry =
            leaderboard.Find(e => e.PlayFabId == myPlayFabId);

        // 내 점수
        if (myEntry != null)
        {
            myUnit.SetCondition(
                myEntry.Position,
                string.IsNullOrEmpty(myEntry.DisplayName)
                    ? myEntry.PlayFabId
                    : myEntry.DisplayName,
                myEntry.StatValue,
                PlayerData.Instance.EquippedProfileImage
            );
            myUnit.gameObject.SetActive(true);
        }
        else
        {
            // Top50 밖: ScoreManager에 캐시된 내 기록 사용
            int myRank = isWeekly ? scoreManager.MyWeeklyRank : scoreManager.MyOverallRank;
            int myScore = isWeekly ? scoreManager.MyWeeklyScore : scoreManager.MyOverallScore;
            if (myRank > 0 && myScore >= 0)
            {
                string myName = string.IsNullOrEmpty(PlayerData.Instance.nickName)
                    ? myPlayFabId
                    : PlayerData.Instance.nickName;
                myUnit.SetCondition(
                    myRank - 1, // SetCondition은 0-based로 받음
                    myName,
                    myScore,
                    PlayerData.Instance.EquippedProfileImage
                );
                myUnit.gameObject.SetActive(true);
            }
        }

        // 월드 랭킹
        foreach (var entry in leaderboard)
        {
            if (uiIndex >= units.Count) break;

            units[uiIndex].SetCondition(
                entry.Position,
                string.IsNullOrEmpty(entry.DisplayName)
                    ? entry.PlayFabId
                    : entry.DisplayName,
                entry.StatValue,
                GetProfileIndex(entry)
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
                GetProfileIndex(entry)
            );
        }
    }
    private int GetProfileIndex(PlayerLeaderboardEntry entry)
    {
        if (entry.Profile != null && !string.IsNullOrEmpty(entry.Profile.AvatarUrl))
        {
            if (int.TryParse(entry.Profile.AvatarUrl, out int index))
                return index;
        }
        return 0; // 기본값
    }
    private void ClearUI()
    {
        int uiIndex = 0;
        foreach (var unit in units)
        {
            unit.SetCondition(uiIndex++, "---", -1, -1);
        }
        myUnit.SetCondition(-1, "---", -1, -1);

        foreach (var unit in podiumUnits)
        {
            unit.SetCondition("", -1);
        }
    }
}
