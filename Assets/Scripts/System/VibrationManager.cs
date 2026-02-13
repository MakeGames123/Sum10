using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }
    private void Awake()
    {
        // 싱글톤 구현: 이미 존재하면 파괴, 없으면 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public bool isOn = false;

    public void Light()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                {
                    // Android 8.0 이상 (API 26+) - 세기 조절 가능
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        // createOneShot(시간ms, 세기0~255)
                        // 시간 30ms, 세기 30 정도로 설정하면 아주 미약한 진동이 옵니다.
                        long milliseconds = 30;
                        int amplitude = 30; 

                        AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                        vibrator.Call("vibrate", effect);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vibration Error: " + e.Message);
        }
    }
}
