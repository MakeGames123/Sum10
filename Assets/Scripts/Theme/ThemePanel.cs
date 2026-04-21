using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class ThemePanel : MonoBehaviour, IMainPanel
{
    public Button rightButton;
    public Button leftButton;
    public Button applyButton;
    public GameObject equippedButton;
    public Button buyButton;
    public TextMeshProUGUI price;
    public List<ThemeData> themeDatas = new();
    public Image boardBackground;
    public Image boardInnerPanelPreview;
    public Image boardLowSpecial;
    public Image boardDecor1Preview;
    public Image boardDecor2Preview;
    public BoardSettingManager setting;
    public BoardManager board;
    private int index = 0;
    private int appliedIndex = 0;
    private ThemeData appliedTheme;
    public RectTransform diaShop;

    void Awake()
    {
        rightButton.onClick.AddListener(MoveToRight);
        leftButton.onClick.AddListener(MoveToLeft);
        applyButton.onClick.AddListener(Apply);
        buyButton.onClick.AddListener(RequestUnlockTheme);
    }
    public void SetCondition()
    {
        // 현재 적용된 테마 기억 (닫을 때 복원용)
        appliedTheme = ThemeManager.Instance.selectedTheme;

        // 현재 적용된 테마의 index 찾기
        int found = themeDatas.IndexOf(appliedTheme);
        if (found >= 0) index = found;
        appliedIndex = index;

        UpdateUI();
    }
    public void OnDisable()
    {
        board.gameObject.SetActive(false);

        // Apply 안 하고 닫았으면 원래 테마로 복원 (원래 index로)
        if (appliedTheme != null)
            ThemeManager.Instance.ChangeTheme(appliedTheme, appliedIndex);
    }
    private void MoveToRight()
    {
        index++;
        if (index >= themeDatas.Count) index = 0;

        UpdateUI();
    }
    private void MoveToLeft()
    {
        index--;
        if (index < 0) index = themeDatas.Count - 1;

        UpdateUI();
    }
    private void UpdateUI()
    {
        // 프리뷰용: selectedTheme을 미리 변경하여 CellView.Init()이 올바른 스프라이트를 읽도록 함
        ThemeManager.Instance.selectedTheme = themeDatas[index];

        boardBackground.sprite = themeDatas[index].boardSkin;
        if (boardInnerPanelPreview != null)
        {
            boardInnerPanelPreview.sprite = themeDatas[index].boardInnerPanel;
            boardInnerPanelPreview.enabled = themeDatas[index].boardInnerPanel != null;
        }
        boardLowSpecial.sprite = themeDatas[index].boardLowSkin;
        if (boardDecor1Preview != null)
        {
            boardDecor1Preview.sprite = themeDatas[index].boardDecor1;
            boardDecor1Preview.enabled = themeDatas[index].boardDecor1 != null;
            var mover1 = boardDecor1Preview.GetComponent<BoardDecorMoving>();
            if (mover1 != null) mover1.enabled = themeDatas[index].boardDecor1Moving;
        }
        if (boardDecor2Preview != null)
        {
            boardDecor2Preview.sprite = themeDatas[index].boardDecor2;
            boardDecor2Preview.enabled = themeDatas[index].boardDecor2 != null;
            var mover2 = boardDecor2Preview.GetComponent<BoardDecorMoving>();
            if (mover2 != null) mover2.enabled = themeDatas[index].boardDecor2Moving;
        }

        var bg = ThemeManager.Instance.background;
        if (bg != null && themeDatas[index].background != null)
            bg.sprite = themeDatas[index].background;

        int status = (index >= 0 && index < ThemeManager.Instance.themeStatus.Count)
            ? ThemeManager.Instance.themeStatus[index]
            : 0;

        if (status == 1 && appliedIndex != index)
        {
            applyButton.gameObject.SetActive(true);
            equippedButton.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(false);
        }
        else if (status == 1 && appliedIndex == index)
        {
            applyButton.gameObject.SetActive(false);
            equippedButton.gameObject.SetActive(true);
            buyButton.gameObject.SetActive(false);
        }
        else
        {
            applyButton.gameObject.SetActive(false);
            equippedButton.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(true);
        }

        if (price != null)
        {
            if (ThemeDataLoader.Instance != null
                && index >= 0
                && index < ThemeDataLoader.Instance.dataList.Count)
                price.text = ThemeDataLoader.Instance.dataList[index].diaPrice.ToString();
            else
                price.text = "-";
        }

        SetUp();
    }
    public void SetUp()
    {
        board.gameObject.SetActive(true);
        setting.SetupBoardWithSize(3);
        board.SetupBoardWithSize(3);
    }
    private void Apply()
    {
        ThemeManager.Instance.ChangeTheme(themeDatas[index], index);
        appliedTheme = themeDatas[index]; // 적용 완료 → 닫을 때 복원 안 함
        appliedIndex = index;
        applyButton.gameObject.SetActive(false);
        equippedButton.gameObject.SetActive(true);
    }

    public void RequestUnlockTheme()
    {
        // 가격 데이터 로드 전엔 구매 차단
        if (ThemeDataLoader.Instance == null
            || index < 0
            || index >= ThemeDataLoader.Instance.dataList.Count)
            return;

        if (PlayerData.Instance.GetDiamond() < ThemeDataLoader.Instance.dataList[index].diaPrice)
        {
            diaShop.anchoredPosition = Vector2.zero;
            return;
        }

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UnlockTheme",
            FunctionParameter = new { index = index }
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            result =>
            {
                if (result.FunctionResult != null)
                {
                    var data = result.FunctionResult as IDictionary<string, object>;
                    Debug.Log((bool)data["success"]);
                    if ((bool)data["success"])
                    {
                        var diamond = System.Convert.ToInt32(data["newDiamond"]);
                        PlayerData.Instance.SetDiamond(diamond);

                        var statusList = data["themeStatus"] as List<object>;

                        ThemeManager.Instance.themeStatus.Clear();

                        foreach (var s in statusList)
                        {
                            ThemeManager.Instance.themeStatus.Add(
                                System.Convert.ToInt32(s)
                            );
                        }

                        UpdateUI();
                    }
                }
            },
            error =>
            {
                Debug.LogError("해금 요청 실패: " + error.GenerateErrorReport());
            }
        );
    }

#if UNITY_EDITOR
    private void Update()
    {
        // F7: SO 값 변경 후 즉시 프리뷰 리빌드
        if (Input.GetKeyDown(KeyCode.F7))
            UpdateUI();
    }
#endif
}
