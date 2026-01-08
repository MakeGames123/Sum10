using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 게임오버 패널 생성기 (폐기용)
/// 매뉴얼 규칙: 빈 오브젝트를 부모로, 이미지는 자식으로
/// 사용 후 삭제할 것
/// </summary>
public class GameOverPanelGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate GameOver Panel")]
    public static void GenerateGameOverPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas를 찾을 수 없습니다.");
            return;
        }

        string spritePath = "Sprite/260109 Gameover/";
        string commonPath = "Sprite/260108 Common UI/";
        string rankingPath = "Sprite/260108 Ranking&Logo/";

        // 스프라이트 로드 - Gameover 폴더
        Sprite panelSprite = Resources.Load<Sprite>(spritePath + "Panel_GameOver");
        Sprite rankingBarSprite = Resources.Load<Sprite>(spritePath + "Rankingbar");
        Sprite rankingBarUserSprite = Resources.Load<Sprite>(spritePath + "Rankingbar_user");
        Sprite rankingArrowSprite = Resources.Load<Sprite>(spritePath + "Ranking_Arrow");
        Sprite replaySprite = Resources.Load<Sprite>(spritePath + "Replay");
        Sprite homeSprite = Resources.Load<Sprite>(spritePath + "Button_Home");

        // 스프라이트 로드 - Common UI 폴더
        Sprite titleBannerSprite = Resources.Load<Sprite>(commonPath + "Panel_TitleBanner");
        Sprite bestScoreBarSprite = Resources.Load<Sprite>(commonPath + "Bar_BestScore");

        // 스프라이트 로드 - Ranking&Logo 폴더
        Sprite crownSprite = Resources.Load<Sprite>(rankingPath + "crown");

        // ===== Root: GameOverPanel (빈 오브젝트) =====
        GameObject rootGO = CreateEmptyUI("GameOverPanel", canvas.transform);
        RectTransform rootRect = rootGO.GetComponent<RectTransform>();
        SetFullStretch(rootRect);

        // 반투명 배경 (별도 자식으로)
        GameObject dimBgGO = CreateEmptyUI("DimBackground", rootGO.transform);
        SetFullStretch(dimBgGO.GetComponent<RectTransform>());
        Image dimBg = dimBgGO.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.5f);
        dimBg.raycastTarget = true;

        // ===== TitleArea (빈 오브젝트) =====
        GameObject titleAreaGO = CreateEmptyUI("TitleArea", rootGO.transform);
        RectTransform titleAreaRect = titleAreaGO.GetComponent<RectTransform>();
        titleAreaRect.anchoredPosition = new Vector2(0, 700);
        titleAreaRect.sizeDelta = new Vector2(400, 120);

        // TitleBanner (Image - TitleArea의 자식)
        GameObject titleBannerGO = CreateEmptyUI("TitleBanner", titleAreaGO.transform);
        SetFullStretch(titleBannerGO.GetComponent<RectTransform>());
        Image titleBannerImg = titleBannerGO.AddComponent<Image>();
        if (titleBannerSprite != null) titleBannerImg.sprite = titleBannerSprite;
        titleBannerImg.preserveAspect = true;

        // TitleText (TitleBanner의 자식)
        GameObject titleTextGO = CreateEmptyUI("TitleText", titleBannerGO.transform);
        SetFullStretch(titleTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI titleText = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Score";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.4f, 0.6f, 0.8f);
        titleText.fontStyle = FontStyles.Bold;

        // ===== ScoreArea (빈 오브젝트) =====
        GameObject scoreAreaGO = CreateEmptyUI("ScoreArea", rootGO.transform);
        RectTransform scoreAreaRect = scoreAreaGO.GetComponent<RectTransform>();
        scoreAreaRect.anchoredPosition = new Vector2(0, 520);
        scoreAreaRect.sizeDelta = new Vector2(400, 150);

        // BigScoreText (ScoreArea의 자식)
        GameObject bigScoreGO = CreateEmptyUI("BigScoreText", scoreAreaGO.transform);
        SetFullStretch(bigScoreGO.GetComponent<RectTransform>());
        TextMeshProUGUI bigScoreText = bigScoreGO.AddComponent<TextMeshProUGUI>();
        bigScoreText.text = "273";
        bigScoreText.fontSize = 120;
        bigScoreText.alignment = TextAlignmentOptions.Center;
        bigScoreText.color = new Color(1f, 0.8f, 0.4f);
        bigScoreText.fontStyle = FontStyles.Bold;

        // ===== MainPanelArea (빈 오브젝트) =====
        GameObject mainPanelAreaGO = CreateEmptyUI("MainPanelArea", rootGO.transform);
        RectTransform mainPanelAreaRect = mainPanelAreaGO.GetComponent<RectTransform>();
        mainPanelAreaRect.anchoredPosition = new Vector2(0, 50);
        mainPanelAreaRect.sizeDelta = new Vector2(600, 700);

        // Panel (Image - MainPanelArea의 자식)
        GameObject panelGO = CreateEmptyUI("Panel", mainPanelAreaGO.transform);
        SetFullStretch(panelGO.GetComponent<RectTransform>());
        Image panelImg = panelGO.AddComponent<Image>();
        if (panelSprite != null) panelImg.sprite = panelSprite;
        panelImg.type = Image.Type.Sliced;

        // ===== BestScoreRow (빈 오브젝트 - Panel의 자식) =====
        GameObject bestScoreRowGO = CreateEmptyUI("BestScoreRow", panelGO.transform);
        RectTransform bestScoreRowRect = bestScoreRowGO.GetComponent<RectTransform>();
        bestScoreRowRect.anchoredPosition = new Vector2(0, 280);
        bestScoreRowRect.sizeDelta = new Vector2(550, 60);

        // CrownGroup (빈 오브젝트)
        GameObject crownGroupGO = CreateEmptyUI("CrownGroup", bestScoreRowGO.transform);
        RectTransform crownGroupRect = crownGroupGO.GetComponent<RectTransform>();
        crownGroupRect.anchorMin = new Vector2(0, 0.5f);
        crownGroupRect.anchorMax = new Vector2(0, 0.5f);
        crownGroupRect.pivot = new Vector2(0, 0.5f);
        crownGroupRect.anchoredPosition = new Vector2(10, 0);
        crownGroupRect.sizeDelta = new Vector2(100, 50);

        // CrownGroup Background (Bar_BestScore 사용)
        GameObject crownBgGO = CreateEmptyUI("Background", crownGroupGO.transform);
        SetFullStretch(crownBgGO.GetComponent<RectTransform>());
        Image crownBg = crownBgGO.AddComponent<Image>();
        if (bestScoreBarSprite != null)
        {
            crownBg.sprite = bestScoreBarSprite;
            crownBg.type = Image.Type.Sliced;
        }
        else
        {
            crownBg.color = new Color(0.85f, 0.92f, 0.98f);
        }

        // CrownIcon (Ranking&Logo/crown 사용)
        GameObject crownIconGO = CreateEmptyUI("CrownIcon", crownGroupGO.transform);
        RectTransform crownIconRect = crownIconGO.GetComponent<RectTransform>();
        crownIconRect.anchorMin = new Vector2(0, 0.5f);
        crownIconRect.anchorMax = new Vector2(0, 0.5f);
        crownIconRect.pivot = new Vector2(0, 0.5f);
        crownIconRect.anchoredPosition = new Vector2(5, 0);
        crownIconRect.sizeDelta = new Vector2(30, 30);
        Image crownImg = crownIconGO.AddComponent<Image>();
        if (crownSprite != null) crownImg.sprite = crownSprite;
        crownImg.preserveAspect = true;

        // GlobalRankText
        GameObject globalRankTextGO = CreateEmptyUI("GlobalRankText", crownGroupGO.transform);
        RectTransform globalRankTextRect = globalRankTextGO.GetComponent<RectTransform>();
        globalRankTextRect.anchorMin = Vector2.zero;
        globalRankTextRect.anchorMax = Vector2.one;
        globalRankTextRect.offsetMin = new Vector2(35, 0);
        globalRankTextRect.offsetMax = new Vector2(-5, 0);
        TextMeshProUGUI globalRankText = globalRankTextGO.AddComponent<TextMeshProUGUI>();
        globalRankText.text = "277";
        globalRankText.fontSize = 24;
        globalRankText.alignment = TextAlignmentOptions.MidlineLeft;
        globalRankText.color = new Color(0.4f, 0.5f, 0.6f);
        globalRankText.fontStyle = FontStyles.Bold;

        // BestScoreLabel
        GameObject bestScoreLabelGO = CreateEmptyUI("BestScoreLabel", bestScoreRowGO.transform);
        RectTransform bestScoreLabelRect = bestScoreLabelGO.GetComponent<RectTransform>();
        bestScoreLabelRect.anchoredPosition = Vector2.zero;
        bestScoreLabelRect.sizeDelta = new Vector2(150, 40);
        TextMeshProUGUI bestScoreLabel = bestScoreLabelGO.AddComponent<TextMeshProUGUI>();
        bestScoreLabel.text = "Best Score";
        bestScoreLabel.fontSize = 24;
        bestScoreLabel.alignment = TextAlignmentOptions.Center;
        bestScoreLabel.color = new Color(0.5f, 0.6f, 0.7f);

        // BestScoreValue
        GameObject bestScoreValueGO = CreateEmptyUI("BestScoreValue", bestScoreRowGO.transform);
        RectTransform bestScoreValueRect = bestScoreValueGO.GetComponent<RectTransform>();
        bestScoreValueRect.anchorMin = new Vector2(1, 0.5f);
        bestScoreValueRect.anchorMax = new Vector2(1, 0.5f);
        bestScoreValueRect.pivot = new Vector2(1, 0.5f);
        bestScoreValueRect.anchoredPosition = new Vector2(-10, 0);
        bestScoreValueRect.sizeDelta = new Vector2(100, 40);
        TextMeshProUGUI bestScoreValue = bestScoreValueGO.AddComponent<TextMeshProUGUI>();
        bestScoreValue.text = "273";
        bestScoreValue.fontSize = 32;
        bestScoreValue.alignment = TextAlignmentOptions.MidlineRight;
        bestScoreValue.color = new Color(0.4f, 0.6f, 0.8f);
        bestScoreValue.fontStyle = FontStyles.Bold;

        // ===== DashedLine =====
        GameObject dashedLineGO = CreateEmptyUI("DashedLine", panelGO.transform);
        RectTransform dashedLineRect = dashedLineGO.GetComponent<RectTransform>();
        dashedLineRect.anchoredPosition = new Vector2(0, 230);
        dashedLineRect.sizeDelta = new Vector2(520, 4);
        Image dashedLineImg = dashedLineGO.AddComponent<Image>();
        dashedLineImg.color = new Color(0.7f, 0.8f, 0.85f);

        // ===== RankingArea (빈 오브젝트) =====
        GameObject rankingAreaGO = CreateEmptyUI("RankingArea", panelGO.transform);
        RectTransform rankingAreaRect = rankingAreaGO.GetComponent<RectTransform>();
        rankingAreaRect.anchoredPosition = new Vector2(0, -20);
        rankingAreaRect.sizeDelta = new Vector2(540, 450);

        VerticalLayoutGroup vlg = rankingAreaGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        // 랭킹 엔트리 6개 생성
        for (int i = 0; i < 6; i++)
        {
            bool isUser = (i == 3);
            CreateRankingEntry(rankingAreaGO.transform, i, isUser,
                rankingBarSprite, rankingBarUserSprite, rankingArrowSprite);
        }

        // ===== ButtonArea (빈 오브젝트) =====
        GameObject buttonAreaGO = CreateEmptyUI("ButtonArea", rootGO.transform);
        RectTransform buttonAreaRect = buttonAreaGO.GetComponent<RectTransform>();
        buttonAreaRect.anchoredPosition = new Vector2(0, -450);
        buttonAreaRect.sizeDelta = new Vector2(300, 200);

        // ReplayButton (빈 오브젝트)
        GameObject replayBtnGO = CreateEmptyUI("Button_Replay", buttonAreaGO.transform);
        RectTransform replayBtnRect = replayBtnGO.GetComponent<RectTransform>();
        replayBtnRect.anchoredPosition = new Vector2(0, 50);
        replayBtnRect.sizeDelta = new Vector2(280, 100);
        Button replayBtn = replayBtnGO.AddComponent<Button>();

        // ReplayImage (Button_Replay의 자식)
        GameObject replayImgGO = CreateEmptyUI("Image", replayBtnGO.transform);
        SetFullStretch(replayImgGO.GetComponent<RectTransform>());
        Image replayImg = replayImgGO.AddComponent<Image>();
        if (replaySprite != null) replayImg.sprite = replaySprite;
        replayImg.preserveAspect = true;
        replayBtn.targetGraphic = replayImg;

        // HomeButton (빈 오브젝트)
        GameObject homeBtnGO = CreateEmptyUI("Button_Home", buttonAreaGO.transform);
        RectTransform homeBtnRect = homeBtnGO.GetComponent<RectTransform>();
        homeBtnRect.anchoredPosition = new Vector2(0, -70);
        homeBtnRect.sizeDelta = new Vector2(200, 60);
        Button homeBtn = homeBtnGO.AddComponent<Button>();

        // HomeImage (Button_Home의 자식)
        GameObject homeImgGO = CreateEmptyUI("Image", homeBtnGO.transform);
        SetFullStretch(homeImgGO.GetComponent<RectTransform>());
        Image homeImg = homeImgGO.AddComponent<Image>();
        if (homeSprite != null) homeImg.sprite = homeSprite;
        homeImg.preserveAspect = true;
        homeBtn.targetGraphic = homeImg;

        // 비활성화 상태로 시작
        rootGO.SetActive(false);
        Selection.activeGameObject = rootGO;

        Debug.Log("GameOverPanel 생성 완료! (매뉴얼 구조 적용)");
    }

    private static GameObject CreateEmptyUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateRankingEntry(Transform parent, int index, bool isUser,
        Sprite normalSprite, Sprite userSprite, Sprite arrowSprite)
    {
        // RankingEntry (빈 오브젝트)
        GameObject entryGO = CreateEmptyUI($"RankingEntry_{index}", parent);
        RectTransform entryRect = entryGO.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(520, 55);

        LayoutElement le = entryGO.AddComponent<LayoutElement>();
        le.preferredHeight = 55;
        le.minHeight = 55;

        // Background (Image - Entry의 자식)
        GameObject bgGO = CreateEmptyUI("Background", entryGO.transform);
        SetFullStretch(bgGO.GetComponent<RectTransform>());
        Image entryBg = bgGO.AddComponent<Image>();
        entryBg.sprite = isUser ? userSprite : normalSprite;
        entryBg.type = Image.Type.Sliced;

        // Arrow (유저만 - Entry의 자식)
        if (isUser && arrowSprite != null)
        {
            GameObject arrowGO = CreateEmptyUI("Arrow", entryGO.transform);
            RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0, 0.5f);
            arrowRect.anchorMax = new Vector2(0, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-5, 0);
            arrowRect.sizeDelta = new Vector2(30, 30);
            Image arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.sprite = arrowSprite;
            arrowImg.preserveAspect = true;
        }

        // RankNumber
        GameObject rankNumGO = CreateEmptyUI("RankNumber", entryGO.transform);
        RectTransform rankNumRect = rankNumGO.GetComponent<RectTransform>();
        rankNumRect.anchorMin = new Vector2(0, 0);
        rankNumRect.anchorMax = new Vector2(0, 1);
        rankNumRect.pivot = new Vector2(0, 0.5f);
        rankNumRect.anchoredPosition = new Vector2(15, 0);
        rankNumRect.sizeDelta = new Vector2(60, 0);
        TextMeshProUGUI rankNumText = rankNumGO.AddComponent<TextMeshProUGUI>();
        rankNumText.text = $"{276 - index}.";
        rankNumText.fontSize = 24;
        rankNumText.alignment = TextAlignmentOptions.MidlineLeft;
        rankNumText.color = isUser ? new Color(0.4f, 0.6f, 0.8f) : new Color(0.5f, 0.6f, 0.7f);
        rankNumText.fontStyle = FontStyles.Bold;

        // PlayerName
        GameObject playerNameGO = CreateEmptyUI("PlayerName", entryGO.transform);
        RectTransform playerNameRect = playerNameGO.GetComponent<RectTransform>();
        playerNameRect.anchorMin = Vector2.zero;
        playerNameRect.anchorMax = Vector2.one;
        playerNameRect.offsetMin = new Vector2(80, 0);
        playerNameRect.offsetMax = new Vector2(-80, 0);
        TextMeshProUGUI playerNameText = playerNameGO.AddComponent<TextMeshProUGUI>();
        playerNameText.text = "player_euneun";
        playerNameText.fontSize = 22;
        playerNameText.alignment = TextAlignmentOptions.MidlineLeft;
        playerNameText.color = isUser ? new Color(0.4f, 0.6f, 0.8f) : new Color(0.5f, 0.6f, 0.7f);

        // Score
        GameObject scoreGO = CreateEmptyUI("Score", entryGO.transform);
        RectTransform scoreRect = scoreGO.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1, 0);
        scoreRect.anchorMax = new Vector2(1, 1);
        scoreRect.pivot = new Vector2(1, 0.5f);
        scoreRect.anchoredPosition = new Vector2(-15, 0);
        scoreRect.sizeDelta = new Vector2(60, 0);
        TextMeshProUGUI scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreText.text = isUser ? "273" : "277";
        scoreText.fontSize = 24;
        scoreText.alignment = TextAlignmentOptions.MidlineRight;
        scoreText.color = isUser ? new Color(0.4f, 0.6f, 0.8f) : new Color(0.5f, 0.6f, 0.7f);
        scoreText.fontStyle = FontStyles.Bold;
    }
#endif
}
