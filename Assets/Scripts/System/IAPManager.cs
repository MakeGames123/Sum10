using System;
using UnityEngine;
using UnityEngine.Purchasing;
using PlayFab;
using PlayFab.ClientModels;

public class IAPManager : MonoBehaviour, IStoreListener
{
    // --- 싱글톤 설정 ---
    public static IAPManager Instance { get; private set; }

    private IStoreController m_StoreController;
    private IExtensionProvider m_StoreExtensionProvider;

    // 상품 ID 정의 (광고 제거 추가)
    public const string Product_NoAds = "ad_removal_1";

    // 결제 성공 후 실행될 콜백 저장용
    private Action onPurchaseComplete;

    public ShopDataLoader shopDataLoader;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }
        else
        {
            Destroy(gameObject);
        }

        shopDataLoader.OnDataLoaded += InitializePurchasing;
    }

    private void InitializePurchasing()
    {
        // 1. 이미 초기화되었거나, 시트 데이터가 아직 없다면 중단
        if (IsInitialized()) return;
        if (shopDataLoader == null || shopDataLoader.Items.Count == 0)
        {
            Debug.LogWarning("IAPManager: 시트 데이터가 아직 로드되지 않아 초기화를 대기합니다.");
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // 2. 시트에서 로드된 모든 상품을 루프 돌며 등록
        foreach (var item in shopDataLoader.Items)
        {
            if (string.IsNullOrEmpty(item.productId)) continue;

            builder.AddProduct(item.productId, ProductType.Consumable);
            Debug.Log($"IAP 등록 (소모성): {item.productId}");
        }

        builder.AddProduct(Product_NoAds, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized() => m_StoreController != null && m_StoreExtensionProvider != null;

    // --- 범용 결제 함수 ---
    public void BuyProduct(string productId, Action onComplete = null)
    {
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                this.onPurchaseComplete = onComplete; // 콜백 저장
                Debug.Log($"구매 시도: {product.definition.id}");
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError("구매 실패: 상품이 없거나 구매 불가능한 상태입니다.");
            }
        }
        else
        {
            Debug.LogError("IAP가 초기화되지 않았습니다.");
        }
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log($"Raw Receipt: {args.purchasedProduct.receipt}");

        var receiptData = JsonUtility.FromJson<ReceiptWrapper>(args.purchasedProduct.receipt);
        var payloadData = JsonUtility.FromJson<PayloadWrapper>(receiptData.Payload);

        var request = new ValidateGooglePlayPurchaseRequest
        {
            CurrencyCode = args.purchasedProduct.metadata.isoCurrencyCode,
            PurchasePrice = (uint)(args.purchasedProduct.metadata.localizedPrice * 100),
            ReceiptJson = payloadData.json,
            Signature = payloadData.signature
        };

        PlayFabClientAPI.ValidateGooglePlayPurchase(request, result =>
        {
            Debug.LogError("✅ PlayFab 검증 성공!");

            // 검증이 성공했을 때만 전달받은 콜백 실행
            onPurchaseComplete?.Invoke();
            onPurchaseComplete = null; // 사용 후 초기화
        }, error =>
        {
            Debug.LogError($"❌ PlayFab 검증 실패: {error.GenerateErrorReport()}");
            onPurchaseComplete = null;
        });

        // 비소모성(광고제거)인 경우 소모 처리를 하지 않기 위해 Complete 리턴
        // 소모성(다이아)인 경우에도 검증 성공 후 처리를 위해 Complete 리턴
        return PurchaseProcessingResult.Complete;
    }
    // --- 스토어 콜백 구현 ---
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
        Debug.Log("IAP 초기화 완료");
    }

    public void OnInitializeFailed(InitializationFailureReason error) => Debug.LogError($"IAP 초기화 실패: {error}");
    public void OnInitializeFailed(InitializationFailureReason error, string message) => Debug.LogError($"IAP 초기화 실패: {error}, {message}");
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"구매 실패: {product.definition.id}, 사유: {failureReason}");
        onPurchaseComplete = null; // 실패 시 콜백 제거
    }

    [Serializable] public class ReceiptWrapper { public string Payload; }
    [Serializable] public class PayloadWrapper { public string json; public string signature; }
}