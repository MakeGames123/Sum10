handlers.RewardDiamondByAd = function () {
    var MAX_DAILY_COUNT = 2;
    var REWARD_AMOUNT = 20;
    var CURRENCY_CODE = "DM";

    var now = Date.now();
    var todayKey = getTodayKey(now);

    var data = server.GetUserData({
        PlayFabId: currentPlayerId,
        Keys: ["adDMCount", "adDMDate"]
    }).Data || {};

    var savedDate = data.adSSDate ? data.adSSDate.Value : "";
    var count = data.adSSCount
        ? parseInt(data.adSSCount.Value)
        : 0;

    // 날짜 바뀌면 리셋
    if (savedDate !== todayKey) {
        count = 0;
    }

    // 서버 안전장치
    if (count >= MAX_DAILY_COUNT) {
        return {
            success: false,
            reason: "DAILY_LIMIT",
            remaining: 0
        };
    }

    // 가상화폐 지급
    server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: CURRENCY_CODE,
        Amount: REWARD_AMOUNT
    });

    count++;

    server.UpdateUserData({
        PlayFabId: currentPlayerId,
        Data: {
            adSSCount: count.toString(),
            adSSDate: todayKey
        }
    });

    return {
        success: true,
        currency: CURRENCY_CODE,
        amount: REWARD_AMOUNT,
        used: count,
        remaining: MAX_DAILY_COUNT - count
    };
};