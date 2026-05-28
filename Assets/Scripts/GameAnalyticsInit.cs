using GameAnalyticsSDK;
using UnityEngine;
public class GameAnalyticsInit : MonoBehaviour
{
    private void Start()
    {
        GameAnalytics.Initialize();
    }
}