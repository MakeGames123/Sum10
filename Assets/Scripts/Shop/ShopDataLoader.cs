using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShopDataLoader : MonoBehaviour
{
    private const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vSAy4_XuF853XvFJebjCEz4QBB8ohAbsazu3m4suh8EmGiqTPLWg4rSsKuwcl5RWVHyT5vVLrpyaE5Z/pub?output=tsv";
    private const string AD_REMOVE_URL =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vSAy4_XuF853XvFJebjCEz4QBB8ohAbsazu3m4suh8EmGiqTPLWg4rSsKuwcl5RWVHyT5vVLrpyaE5Z/pub?gid=1284289808&single=true&output=tsv";

    private const string CACHE_KEY = "ShopDataCache";
    private const string AD_REMOVE_CACHE_KEY = "AdRemoveDataCache";

    private List<ShopItemData> _items = new List<ShopItemData>();
    public List<ShopItemData> Items => _items;

    private Dictionary<string, string> _adRemoveData = new Dictionary<string, string>();
    public Dictionary<string, string> AdRemoveData => _adRemoveData;

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

        // 광고제거 데이터 로드
        yield return LoadAdRemoveData();

        OnDataLoaded?.Invoke();
    }

    private IEnumerator LoadAdRemoveData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(AD_REMOVE_URL))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string tsv = request.downloadHandler.text;
                ParseKeyValueTSV(tsv, _adRemoveData);
                PlayerPrefs.SetString(AD_REMOVE_CACHE_KEY, tsv);
                PlayerPrefs.Save();
                Debug.Log($"ShopDataLoader: 광고제거 데이터 로드 완료 ({_adRemoveData.Count}개 항목)");
            }
            else
            {
                Debug.LogWarning($"ShopDataLoader: 광고제거 네트워크 실패, 캐시 사용");
                string cached = PlayerPrefs.GetString(AD_REMOVE_CACHE_KEY, "");
                if (!string.IsNullOrEmpty(cached))
                {
                    ParseKeyValueTSV(cached, _adRemoveData);
                }
            }
        }
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

    private void ParseKeyValueTSV(string tsv, Dictionary<string, string> dict)
    {
        dict.Clear();
        string[] lines = tsv.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split('\t');
            if (cols.Length < 2) continue;
            dict[cols[0]] = cols[1];
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
