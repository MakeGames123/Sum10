using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
public class TutorialStatusManager : MonoBehaviour
{
    // 외부에서 접근 가능한 상태 변수
    public bool isTutorialCompleted { get; private set; }
    public static TutorialStatusManager Instance;
    public PlayFabLoginManager loginManager;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }

        loginManager.onLogined.AddListener(LoadTutorialStatus);
    }
    public void UpdateStatus(bool flag)
    {
        isTutorialCompleted = flag;
        if(flag) SaveTutorialStatus();
    }
    // 데이터 저장 (True/False)
    public void SaveTutorialStatus()
    {
        Debug.Log(isTutorialCompleted);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "TutorialFinished", isTutorialCompleted.ToString() } }
        };
        PlayFabClientAPI.UpdateUserData(request,
            res => Debug.Log("서버 저장 성공"),
            err => Debug.LogError(err.GenerateErrorReport()));
    }

    // 데이터 불러오기 (서버 값을 내부 변수에 동기화)
    public void LoadTutorialStatus()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), res =>
        {
            if (res.Data != null && res.Data.ContainsKey("TutorialFinished"))
            {
                isTutorialCompleted = bool.Parse(res.Data["TutorialFinished"].Value);
            }
            else
            {
                isTutorialCompleted = false; // 데이터가 없으면 미완료로 간주
            }
            Debug.Log($"서버 데이터 동기화 완료: {isTutorialCompleted}");
        }, err => Debug.LogError(err.GenerateErrorReport()));
    }
}
