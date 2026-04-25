using Firebase;
using Firebase.Extensions;
using UnityEngine;
public class FirebaseManager : MonoBehaviour
{
    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                // Firebase 사용 가능
                Debug.Log("Firebase Ready");
            }
            else
            {
                Debug.LogError("Firebase Init Failed: " + status);
            }
        });
    }
}
