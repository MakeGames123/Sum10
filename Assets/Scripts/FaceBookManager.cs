using UnityEngine;
using Facebook.Unity;
using System.Collections.Generic;

public class FacebookManager : MonoBehaviour
{
    void Awake()
    {
        if (!FB.IsInitialized)
        {
            // SDK 초기화
            FB.Init(InitCallback, OnHideResponse);
        }
        else
        {
            // 이미 초기화되어 있다면 활성화
            FB.ActivateApp();
        }
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            // 초기화 성공 시 앱 활성화 신호 전송
            FB.ActivateApp();
            Debug.Log("Facebook SDK 초기화 성공!");
        }
        else
        {
            Debug.Log("Facebook SDK 초기화 실패");
        }
    }

    private void OnHideResponse(bool isGameShown)
    {
        if (!isGameShown)
        {
            // 게임이 일시정지(백그라운드) 되었을 때 시간 멈춤 등 처리
            Time.timeScale = 0;
        }
        else
        {
            // 다시 게임으로 돌아왔을 때
            Time.timeScale = 1;
        }
    }
}