using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ADremovePanelGenerator : EditorWindow
{
    [MenuItem("Tools/AD Remove 패널 생성")]
    static void Generate()
    {
        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다. Canvas를 먼저 생성해주세요.");
            return;
        }

        // 스프라이트 로드
        Sprite sprADremovePanel = LoadSprite("260130 ADremove/ADremovePanel");
        Sprite sprADPanel       = LoadSprite("260130 ADremove/AD_Panel");
        Sprite sprDiaButton     = LoadSprite("260130 ADremove/AD_Dia_Button");
        Sprite sprCashButton    = LoadSprite("260130 ADremove/AD_Cash_Button");
        Sprite sprDiamond       = LoadSprite("260108 Common UI/icon_Diamond");
        Sprite sprCoin          = LoadSprite("260108 Common UI/icon_Coin");

        // ============================================================
        // Root: ADremovePopupPanel (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject root = CreateEmpty("ADremovePopupPanel", canvas.transform);
        StretchFull(root);

        // ============================================================
        // DimBackground
        // ============================================================
        GameObject dimBG = CreateImage("DimBackground", root.transform, null);
        StretchFull(dimBG);
        Image dimImg = dimBG.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.5f);
        dimImg.preserveAspect = false; // 딤은 화면 전체 커버

        // ============================================================
        // TapToCloseText
        // ============================================================
        GameObject tapText = CreateTMP("TapToCloseText", root.transform,
            "화면을 터치하여 닫기", 28, Color.white, TextAlignmentOptions.Center);
        SetRect(tapText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 80), new Vector2(400, 50));

        // ============================================================
        // MainPanelArea (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject mainPanel = CreateEmpty("MainPanelArea", root.transform);
        SetRect(mainPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -30), new Vector2(820, 1050));

        // -- Background (ADremovePanel.png)
        GameObject mainBG = CreateImage("Background", mainPanel.transform, sprADremovePanel);
        StretchFull(mainBG);

        // ============================================================
        // ContentArea (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject contentArea = CreateEmpty("ContentArea", mainPanel.transform);
        SetRect(contentArea, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 40), new Vector2(740, 580));

        // -- ContentBG (AD_Panel.png - 점선 테두리 흰색 패널)
        GameObject contentBG = CreateImage("Background", contentArea.transform, sprADPanel);
        StretchFull(contentBG);

        // ============================================================
        // DecoArea (부모 - 이미지/텍스트 없음) - 장식용 다이아/코인
        // ============================================================
        GameObject decoArea = CreateEmpty("DecoArea", contentArea.transform);
        StretchFull(decoArea);

        // 다이아몬드 장식 (참고 이미지 기준 좌우 흩뿌림)
        CreateDecoImage("DecoDiamond_0", decoArea.transform, sprDiamond,
            new Vector2(-280, 100), new Vector2(65, 65), -15f);
        CreateDecoImage("DecoDiamond_1", decoArea.transform, sprDiamond,
            new Vector2(300, 120), new Vector2(50, 50), 20f);
        CreateDecoImage("DecoDiamond_2", decoArea.transform, sprDiamond,
            new Vector2(-180, 160), new Vector2(40, 40), 10f);

        // 코인 장식
        CreateDecoImage("DecoCoin_0", decoArea.transform, sprCoin,
            new Vector2(230, 170), new Vector2(35, 35), 0f);
        CreateDecoImage("DecoCoin_1", decoArea.transform, sprCoin,
            new Vector2(-310, 170), new Vector2(30, 30), -10f);
        CreateDecoImage("DecoCoin_2", decoArea.transform, sprCoin,
            new Vector2(150, 80), new Vector2(28, 28), 15f);

        // ============================================================
        // TitleText
        // ============================================================
        GameObject titleText = CreateTMP("TitleText", contentArea.transform,
            "게임 내 광고\n영원히 제거 !", 52, new Color(0.25f, 0.35f, 0.55f, 1f),
            TextAlignmentOptions.Center);
        SetRect(titleText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -60), new Vector2(600, 160));
        TextMeshProUGUI titleTMP = titleText.GetComponent<TextMeshProUGUI>();
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.enableWordWrapping = true;

        // ============================================================
        // SubtitleText
        // ============================================================
        GameObject subtitleText = CreateTMP("SubtitleText", contentArea.transform,
            "저렴하게 구매해보세요", 30, new Color(0.4f, 0.45f, 0.55f, 1f),
            TextAlignmentOptions.Center);
        SetRect(subtitleText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -220), new Vector2(500, 50));

        // ============================================================
        // ButtonGroup (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject btnGroup = CreateEmpty("ButtonGroup", contentArea.transform);
        SetRect(btnGroup, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 30), new Vector2(680, 210));

        // ============================================================
        // DiaPurchaseBtn (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject diaBtn = CreateEmpty("DiaPurchaseBtn", btnGroup.transform);
        SetRect(diaBtn, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10, 0), new Vector2(320, 190));
        diaBtn.AddComponent<Button>();

        // -- Dia Button Background
        GameObject diaBtnBG = CreateImage("Background", diaBtn.transform, sprDiaButton);
        StretchFull(diaBtnBG);

        // -- Dia Label
        GameObject diaLabel = CreateTMP("LabelText", diaBtn.transform,
            "다이아 구매", 24, Color.white, TextAlignmentOptions.Center);
        SetRect(diaLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -30), new Vector2(200, 40));

        // -- Dia Icon
        GameObject diaIcon = CreateImage("DiaIcon", diaBtn.transform, sprDiamond);
        SetRect(diaIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-55, -15), new Vector2(55, 55));

        // -- Dia PriceText
        GameObject diaPriceText = CreateTMP("PriceText", diaBtn.transform,
            "190", 48, Color.white, TextAlignmentOptions.Center);
        SetRect(diaPriceText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(30, -15), new Vector2(150, 60));
        diaPriceText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // -- Dia OriginalPriceText (취소선)
        GameObject diaOrigPrice = CreateTMP("OriginalPriceText", diaBtn.transform,
            "<s>390</s>", 22, new Color(0.75f, 0.8f, 1f, 0.8f), TextAlignmentOptions.Center);
        SetRect(diaOrigPrice, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 20), new Vector2(150, 30));

        // -- Dia OriginalIcon (작은 다이아 아이콘)
        GameObject diaOrigIcon = CreateImage("OriginalDiaIcon", diaBtn.transform, sprDiamond);
        SetRect(diaOrigIcon, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-35, 20), new Vector2(22, 22));

        // ============================================================
        // CashPurchaseBtn (부모 - 이미지/텍스트 없음)
        // ============================================================
        GameObject cashBtn = CreateEmpty("CashPurchaseBtn", btnGroup.transform);
        SetRect(cashBtn, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-10, 0), new Vector2(320, 190));
        cashBtn.AddComponent<Button>();

        // -- Cash Button Background
        GameObject cashBtnBG = CreateImage("Background", cashBtn.transform, sprCashButton);
        StretchFull(cashBtnBG);

        // -- Cash Label
        GameObject cashLabel = CreateTMP("LabelText", cashBtn.transform,
            "현금 구매", 24, new Color(0.45f, 0.3f, 0.1f, 1f), TextAlignmentOptions.Center);
        SetRect(cashLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -30), new Vector2(200, 40));

        // -- Cash PriceText
        GameObject cashPriceText = CreateTMP("PriceText", cashBtn.transform,
            "KRW1900", 40, new Color(0.45f, 0.3f, 0.1f, 1f), TextAlignmentOptions.Center);
        SetRect(cashPriceText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -15), new Vector2(260, 60));
        cashPriceText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // -- Cash OriginalPriceText (취소선)
        GameObject cashOrigPrice = CreateTMP("OriginalPriceText", cashBtn.transform,
            "<s>KRW3900</s>", 20, new Color(0.6f, 0.45f, 0.25f, 0.8f), TextAlignmentOptions.Center);
        SetRect(cashOrigPrice, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 20), new Vector2(200, 30));

        // 선택 상태 설정
        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create ADremovePopupPanel");

        Debug.Log("ADremovePopupPanel 생성 완료! HeaderArea는 DiaPopupPanel에서 복사해주세요.");
    }

    // =================================================================
    // 헬퍼 메서드
    // =================================================================

    static Sprite LoadSprite(string resourcePath)
    {
        string fullPath = "Assets/Resources/Sprite/" + resourcePath + ".png";
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        if (spr == null)
            Debug.LogWarning($"스프라이트를 찾을 수 없습니다: {fullPath}");
        return spr;
    }

    static GameObject CreateEmpty(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return go;
    }

    static GameObject CreateDecoImage(string name, Transform parent, Sprite sprite,
        Vector2 pos, Vector2 size, float rotation)
    {
        GameObject go = CreateImage(name, parent, sprite);
        SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        go.transform.localEulerAngles = new Vector3(0, 0, rotation);
        return go;
    }

    static GameObject CreateTMP(string name, Transform parent,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.richText = true;
        return go;
    }

    static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        // pivot은 anchor 기준으로 자동 설정
        rt.pivot = new Vector2(
            (anchorMin.x + anchorMax.x) / 2f,
            (anchorMin.y + anchorMax.y) / 2f
        );
    }
}
