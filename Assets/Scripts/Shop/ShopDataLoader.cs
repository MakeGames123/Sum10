using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShopDataLoader : MonoBehaviour
{
    private const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vSAy4_XuF853XvFJebjCEz4QBB8ohAbsazu3m4suh8EmGiqTPLWg4rSsKuwcl5RWVHyT5vVLrpyaE5Z/pub?output=tsv";

    private const string CACHE_KEY = "ShopDataCache";

    private List<ShopItemData> _items = new List<ShopItemData>();
    public List<ShopItemData> Items => _items;

    public event Action OnDataLoaded;

    public static ShopDataLoader Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(LoadShopData());
    }

    private IEnumerator LoadShopData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(SHEET_URL))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string tsv = request.downloadHandler.text;
                ParseTSV(tsv);

                // 성공 시 로컬 캐시 저장
                PlayerPrefs.SetString(CACHE_KEY, tsv);
                PlayerPrefs.Save();

                Debug.Log($"ShopDataLoader: 시트에서 {_items.Count}개 상품 로드 완료");
            }
            else
            {
                Debug.LogWarning($"ShopDataLoader: 네트워크 실패 ({request.error}), 캐시 사용");
                LoadFromCache();
            }
        }

        OnDataLoaded?.Invoke();
    }

    private void LoadFromCache()
    {
        string cached = PlayerPrefs.GetString(CACHE_KEY, "");
        if (!string.IsNullOrEmpty(cached))
        {
            ParseTSV(cached);
            Debug.Log($"ShopDataLoader: 캐시에서 {_items.Count}개 상품 로드");
        }
        else
        {
            Debug.LogError("ShopDataLoader: 캐시도 없음, 데이터 로드 실패");
        }
    }

    private void ParseTSV(string tsv)
    {
        _items.Clear();
        string[] lines = tsv.Split('\n');

        // 첫 줄은 헤더, 두 번째 줄부터 데이터
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split('\t');
            if (cols.Length < 8) continue;

            var item = new ShopItemData
            {
                slot = int.Parse(cols[0]),
                displayString = cols[1],
                bonusTag = cols[2],
                baseQty = int.Parse(cols[3]),
                bonusQty = int.Parse(cols[4]),
                totalQty = int.Parse(cols[5]),
                productId = cols[6],
                priceText = cols[7]
            };

            _items.Add(item);
        }
    }
}
