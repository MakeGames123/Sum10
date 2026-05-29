using System;

[Serializable]
public class ShopItemData
{
    public int slot;
    public string displayString;   // "19+4"
    public string bonusTag;        // "보너스 4개!"
    public int baseQty;
    public int bonusQty;
    public int totalQty;
    public string productId;       // IAP Product ID
    public string priceText;       // "₩1,100" (나중에 IAP localizedPriceString으로 교체)
    public int bonusStringId;       
}
