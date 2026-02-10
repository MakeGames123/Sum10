using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfileList : MonoBehaviour
{
    public static ProfileList Instance { get; private set; }
    public List<Sprite> profileList = new();
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
}
