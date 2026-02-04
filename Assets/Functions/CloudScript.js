// Auto-generated CloudScript 
handlers.UsePlayerGold = function (args, context) {
    var pendingAmount = args.pendingAmount || 0;
    var useAmount = args.useAmount || 0;

    if (useAmount <= 0) {
        return { error: "Invalid useAmount" };
    }

    try {
        var result;

        // 1. pendingAmount 먼저 반영 (항상 양수라고 가정)
        if (pendingAmount > 0) {
            result = server.AddUserVirtualCurrency({
                PlayFabId: currentPlayerId,
                VirtualCurrency: "GD",
                Amount: pendingAmount
            });
        } else {
            // pending이 없으면 현재 잔액 조회
            var inv = server.GetUserInventory({ PlayFabId: currentPlayerId });
            result = { Balance: inv.VirtualCurrency["GD"] || 0 };
        }

        var currentBalance = result.Balance;

        // 2. 사용 가능 여부 확인
        if (currentBalance < useAmount) {
            return { error: "Not enough gold" };
        }

        // 3. 골드 차감
        result = server.SubtractUserVirtualCurrency({
            PlayFabId: currentPlayerId,
            VirtualCurrency: "GD",
            Amount: useAmount
        });

        // 4. 결과 반환
        return {
            success: true,
            newGold: result.Balance,
            addedPending: pendingAmount,
            used: useAmount
        };

    } catch (e) {
        return { error: "Server error during gold update", detail: e };
    }
};



/**
 * 클라이언트에서 pendingGold(누적된 변화량)를 서버와 동기화
 * args.pendingGold : 클라이언트에서 모아둔 추가 골드 양
 */
handlers.SyncPendingGold = function (args, context) {
    var pending = args.pendingGold || 0;

    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "GD",
        Amount: pending
    });

    return {
        success: true,
        newGold: result.Balance, // 동기화 후 최종 잔액
        added: pending
    };
};handlers.UsePlayerTicket = function (args, context) {
    var pendingAmount = args.pendingAmount || 0;
    var useAmount = args.useAmount || 0;

    if (useAmount <= 0) {
        return { error: "Invalid useAmount" };
    }

    try {
        var result;

        // 1. pendingAmount 먼저 반영 (항상 양수라고 가정)
        if (pendingAmount > 0) {
            result = server.AddUserVirtualCurrency({
                PlayFabId: currentPlayerId,
                VirtualCurrency: "TK",
                Amount: pendingAmount
            });
        } else {
            // pending이 없으면 현재 잔액 조회
            var inv = server.GetUserInventory({ PlayFabId: currentPlayerId });
            result = { Balance: inv.VirtualCurrency["TK"] || 0 };
        }

        var currentBalance = result.Balance;

        // 2. 사용 가능 여부 확인
        if (currentBalance < useAmount) {
            return { error: "Not enough ticket" };
        }

        // 3. 티켓 차감
        result = server.SubtractUserVirtualCurrency({
            PlayFabId: currentPlayerId,
            VirtualCurrency: "TK",
            Amount: useAmount
        });

        // 4. 결과 반환
        return {
            success: true,
            newTicket: result.Balance,
            addedPending: pendingAmount,
            used: useAmount
        };

    } catch (e) {
        return { error: "Server error during ticket update", detail: e };
    }
};

handlers.SyncPendingTicket = function (args, context) {
    var pending = args.pendingTicket || 0;

    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "TK",
        Amount: pending
    });

    return {
        success: true,
        newTicket: result.Balance, // 동기화 후 최종 잔액
        added: pending
    };
};


// 티켓 조회
handlers.GetPlayerTicket = function (args, context) {
    var inv = server.GetUserInventory({ PlayFabId: currentPlayerId });
    var currentTicket = inv.VirtualCurrency["TK"] || 0;

    return {
        success: true,
        currentTicket: currentTicket
    };
};
// ===== 공통 유틸 함수 =====

// 1) Internal Title Data 로드
function GetProbabilityData(prefix, level) {
    var key = prefix + "_" + level;
    var titleData = server.GetTitleInternalData({ Keys: [key] }).Data;

    if (!titleData || !titleData[key]) {
        throw "Probability data for level " + level + " not found";
    }

    return JSON.parse(titleData[key]);
}

// 2) 누적 확률 계산
function GetCumulativeProbability(probArray) {
    var total = 0;
    var cumulative = [];
    for (var i = 0; i < probArray.length; i++) {
        total += probArray[i];
        cumulative.push(total);
    }
    return cumulative;
}

// 3) 누적 확률 배열에서 랜덤 인덱스 선택
function GetPickedIndex(cumulative) {
    var rand = Math.random() * 100;
    for (var i = 0; i < cumulative.length; i++) {
        if (rand < cumulative[i]) {
            return i;
        }
    }
    return 0; // fallback
}

function GrantItemAndSyncInventory(playFabId, itemId) {
    var grantResult;
    try {
        grantResult = server.GrantItemsToUser({
            PlayFabId: playFabId,
            ItemIds: [itemId],
            CatalogVersion: "AllBulletCatalog"
        });
    } catch (e) {
        return { success: false, error: "GrantItemsToUser failed: " + e };
    }

    if (!grantResult || !grantResult.ItemGrantResults || grantResult.ItemGrantResults.length === 0) {
        return { success: false, error: "GrantItemsToUser returned no results" };
    }

    var granted = grantResult.ItemGrantResults[0];
    return {
        success: true,
        grantedItemId: granted.ItemId,
        instanceId: granted.ItemInstanceId
    };
}

function ConsumeItemFully(playFabId, itemId, consumeCount) {
    var inventoryResult = server.GetUserInventory({ PlayFabId: playFabId });
    var targetItem = inventoryResult.Inventory.find(item => item.ItemId === itemId);

    if (!targetItem || targetItem.RemainingUses < consumeCount) {
        return {
            success: false,
            error: "Not enough item in inventory",
            currentCount: targetItem ? targetItem.RemainingUses : 0
        };
    }

    try {
        server.ConsumeItem({
            PlayFabId: playFabId,
            ItemInstanceId: targetItem.ItemInstanceId,
            ConsumeCount: consumeCount
        });
    } catch (e) {
        return { success: false, error: "ConsumeItem failed: " + JSON.stringify(e) };
    }

    return { success: true, remainingInventory: targetItem.RemainingUses - consumeCount };
}

// ===== 뽑기 핸들러 =====

// 일반 뽑기
handlers.NormalBulletBatch = function (args, context) {
    var addList = args.addList || [];
    var removeList = args.removeList || [];

    try {
        // 1) 아이템 지급
        if (addList.length > 0) {
            server.GrantItemsToUser({
                PlayFabId: currentPlayerId,
                ItemIds: addList,
                CatalogVersion: "AllBulletCatalog"
            });
        }

        if (removeList.length > 0) {
            var inventory = server.GetUserInventory({ PlayFabId: currentPlayerId }).Inventory;
            for (var i = 0; i < removeList.length; i++) {
                var target = inventory.find(item => item.ItemId === removeList[i]);
                if (target) {
                    server.RevokeInventoryItem({
                        PlayFabId: currentPlayerId,
                        ItemInstanceId: target.ItemInstanceId
                    });
                }
            }
        }

        return { success: true };
    } catch (e) {
        return { success: false, error: e.toString() };
    }
};
// 스페셜 뽑기
handlers.DrawSpecialBullet = function (args, context) {
    var itemToConsume = "1000_rank10";
    var consumeAmount = args.consumeAmount || 1;
    var level = args.level;

    if (!level) {
        return { success: false, error: "Level not specified" };
    }

    try {
        var consumeResult = ConsumeItemFully(currentPlayerId, itemToConsume, consumeAmount);

        if (!consumeResult.success) {
            return { success: false, error: consumeResult.error };
        }

        // 2️⃣ 확률 데이터 로드 및 계산
        var drawPercentage = GetProbabilityData("SpecialBulletRate", level);
        var cumulative = GetCumulativeProbability(drawPercentage);
        var pickedIndex = GetPickedIndex(cumulative);

        // 3️⃣ 카탈로그 로드
        var catalogItems = server.GetCatalogItems({ CatalogVersion: "SpecialBulletCatalog" }).Catalog;
        if (!catalogItems || catalogItems.length === 0) {
            throw "Catalog 'SpecialBulletCatalog' is empty or not found";
        }

        // 4️⃣ pickedIndex × 랜덤 배수 (1~4배)
        var randomMultiplier = Math.floor(Math.random() * 4) * 9;
        var finalIndex = pickedIndex + randomMultiplier;

        if (finalIndex >= catalogItems.length) {
            finalIndex = catalogItems.length - 1;
        }

        var bulletItemId = catalogItems[finalIndex].ItemId;

        // 5️⃣ 아이템 지급
        var result = GrantItemAndSyncInventory(currentPlayerId, bulletItemId);

        if (!result.success) {
            return { success: false, error: result.error };
        }

        return {
            success: true,
            message: "Special draw completed successfully",
            grantedItemId: bulletItemId
        };
    } catch (e) {
        return { success: false, error: e.toString() };
    }
};
// File: CloudScript.js
// 특수탄환 합성만
handlers.MergeSpecialBullet = function (args, context) {
    var itemCode = args.itemCode; // ex) 1000
    var rank = args.rank;         // ex) 1
    var itemId = itemCode + "_rank" + rank;
    var nextItemId = itemCode + "_rank" + (rank + 1);

    // 1️⃣ 인벤토리 한 번만 조회
    var inventoryResult = server.GetUserInventory({ PlayFabId: currentPlayerId });
    var targetItem = inventoryResult.Inventory.find(item => item.ItemId === itemId);

    if (!targetItem || targetItem.RemainingUses < 2) {
        return {
            success: false,
            error: "Not enough items to merge. Need 2, Found: " + (targetItem ? targetItem.RemainingUses : 0)
        };
    }

    try {
        // 2️⃣ 한 번만 소모 (2개)
        server.ConsumeItem({
            PlayFabId: currentPlayerId,
            ItemInstanceId: targetItem.ItemInstanceId,
            ConsumeCount: 2
        });

        // 3️⃣ 다음 랭크 아이템 지급
        var grantResult = server.GrantItemsToUser({
            PlayFabId: currentPlayerId,
            ItemIds: [nextItemId],
            CatalogVersion: "AllBulletCatalog"
        });

        if (!grantResult || !grantResult.ItemGrantResults || grantResult.ItemGrantResults.length === 0) {
            return { success: false, error: "GrantItemsToUser returned no results" };
        }

        return {
            success: true,
            newItemId: nextItemId
        };

    } catch (e) {
        return { success: false, error: JSON.stringify(e) };
    }
};

handlers.AdminGrantTestItem = function(args, context) {
    var itemId = args.itemId || "1000_rank10";
    var grantCount = args.count || 10;

    try {
        var itemArray = [];
        for (var i = 0; i < grantCount; i++) {
            itemArray.push(itemId);
        }

        var grantResult = server.GrantItemsToUser({
            PlayFabId: currentPlayerId,
            ItemIds: itemArray,
            CatalogVersion: "AllBulletCatalog"
        });

        if (!grantResult || !grantResult.ItemGrantResults || grantResult.ItemGrantResults.length === 0) {
            return { success: false, error: "GrantItemsToUser returned no results" };
        }

        return {
            success: true,
            grantedItemId: itemId,
            grantedCount: grantCount
        };
    } catch (e) {
        return { success: false, error: JSON.stringify(e) };
    }
};handlers.UpgradeMainStats = function (args, context) {
    var stats = args.stats; // [{ statType, levelUpCount, playerCost }, ...]
    var pendingGold = args.pendingGold || 0; // 클라에서 보내는 pending 골드

    var BASE_COST = 50;
    var COST_MULTIPLIER = 1.15;
    var CURRENCY_CODE = "GD";

    if (!stats || !Array.isArray(stats) || stats.length === 0) {
        return { success: false, message: "요청된 스탯 데이터가 없습니다." };
    }

    // 1️⃣ 필요한 InternalData 키 수집
    var keys = stats.map(s => s.statType + "_Level");

    // 2️⃣ 현재 스탯 데이터 불러오기
    var playerData = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: keys
    }).Data || {};

    var totalServerCost = 0;
    var levelResults = {};
    var costDetails = [];

    // 3️⃣ 각 스탯별 검증 및 비용 계산
    for (var i = 0; i < stats.length; i++) {
        var stat = stats[i];
        var statType = stat.statType;
        var levelUpCount = parseInt(stat.levelUpCount) || 0;
        var clientCost = parseInt(stat.playerCost) || 0;

        if (!statType || levelUpCount <= 0) {
            return { success: false, message: "잘못된 요청: " + statType };
        }

        var currentLevel = playerData[statType + "_Level"]
            ? parseInt(playerData[statType + "_Level"].Value) || 0
            : 0;

        // 서버에서 실제 비용 계산
        var serverCost = 0;
        for (var j = 0; j < levelUpCount; j++) {
            serverCost += Math.floor(BASE_COST * Math.pow(COST_MULTIPLIER, currentLevel + j));
        }

        // 클라 검증
        if (clientCost !== serverCost) {
            return {
                success: false,
                message: "검증 실패: 클라이언트 계산값 불일치 (" + statType + ")",
                statType: statType,
                clientCost: clientCost,
                serverCost: serverCost
            };
        }

        totalServerCost += serverCost;
        levelResults[statType] = {
            oldLevel: currentLevel,
            newLevel: currentLevel + levelUpCount,
            cost: serverCost
        };
        costDetails.push({ statType: statType, cost: serverCost });
    }

    // 4️⃣ 현재 골드 불러오기
    var inventory = server.GetUserInventory({ PlayFabId: currentPlayerId });
    var currentGD = inventory.VirtualCurrency && inventory.VirtualCurrency[CURRENCY_CODE] || 0;

    var totalAvailable = currentGD + pendingGold;

    // 5️⃣ 골드 부족 체크
    if (totalAvailable < totalServerCost) {
        return {
            success: false,
            message: "재화 부족",
            needed: totalServerCost,
            currentGD: currentGD,
            pendingGold: pendingGold
        };
    }

    // 6️⃣ 실제 차감 계산
    var useFromPending = 0;
    var useFromServer = 0;

    if (pendingGold >= totalServerCost) {
        // pending으로만 처리 가능
        useFromPending = totalServerCost;
    } else {
        useFromPending = pendingGold;
        useFromServer = totalServerCost - pendingGold;
    }

    // 7️⃣ 서버 골드 차감 (필요할 때만)
    if (useFromServer > 0) {
        try {
            server.SubtractUserVirtualCurrency({
                PlayFabId: currentPlayerId,
                VirtualCurrency: CURRENCY_CODE,
                Amount: useFromServer
            });
        } catch (e) {
            return { success: false, message: "서버 골드 차감 실패: " + e };
        }
    }

    // 8️⃣ 강화 후 남는 pendingGold 계산
    var leftoverPending = Math.max(0, pendingGold - totalServerCost);

    // 남은 pendingGold는 서버 골드로 반영 (Add)
    var addResult = null;
    if (leftoverPending > 0) {
        try {
            addResult = server.AddUserVirtualCurrency({
                PlayFabId: currentPlayerId,
                VirtualCurrency: CURRENCY_CODE,
                Amount: leftoverPending
            });
        } catch (e) {
            return { success: false, message: "pending 반영 실패: " + e };
        }
    }

    // 9️⃣ InternalData 업데이트
    var updateData = {};
    for (var key in levelResults) {
        updateData[key + "_Level"] = levelResults[key].newLevel.toString();
    }

    try {
        server.UpdateUserInternalData({
            PlayFabId: currentPlayerId,
            Data: updateData
        });
    } catch (e) {
        return { success: false, message: "레벨 업데이트 실패: " + e };
    }

    // 10️⃣ 결과 반환
    var remainingGD = addResult ? addResult.Balance : (inventory.VirtualCurrency[CURRENCY_CODE] - useFromServer);

    var simpleDetails = {};
    for (var key in levelResults) {
        simpleDetails[key] = levelResults[key].newLevel;
    }

    return {
        success: true,
        message: "강화 성공 (" + stats.length + "개 스탯)",
        totalCost: totalServerCost,
        usedPending: useFromPending,
        usedServer: useFromServer,
        leftoverPending: leftoverPending,
        remainingCurrency: remainingGD,
        details: simpleDetails,
        costBreakdown: costDetails
    };
};


// 스탯 정보 불러오기 핸들러
handlers.GetPlayerStats = function (args, context) {
    // 고정 스탯 목록
    var statTypes = ["AttackPower", "Endurance", "Technic"];

    // InternalData에서 스탯 조회
    var keys = statTypes.map(s => s + "_Level");

    var dataResult = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: keys
    }).Data;

    // 결과 정리
    var stats = {};
    for (var i = 0; i < statTypes.length; i++) {
        var key = keys[i];
        stats[statTypes[i]] = dataResult[key] ? parseInt(dataResult[key].Value) : 0;
    }

    return {
        success: true,
        message: "스탯 반환 성공",
        stats: stats
    };
};handlers.SetUserDataValue = function (args, context) {
    var key = args.key;
    var value = args.value;
    var isPrivate = args.private || false;

    if (!key || value === undefined || value === null) {
        throw "Invalid key or value";
    }

    var update = {};
    update[key] = value.toString();

    server.UpdateUserData({
        PlayFabId: currentPlayerId,
        Data: update,
        Permission: isPrivate ? "Private" : "Public"
    });

    return { message: "Data updated successfully", key: key, value: value };
};

handlers.GetUserDataValue = function (args, context) {
    // args.key: 가져올 데이터 키

    var key = args.key;
    if (!key) {
        throw "Key is required";
    }

    var dataResult = server.GetUserData({
        PlayFabId: currentPlayerId,
        Keys: [key]
    });

    var value = dataResult.Data[key] ? dataResult.Data[key].Value : null;

    return {
        key: key,
        value: value
    };
};// CloudScript (PlayFab Server API)를 가정한 코드
// 핸들러명: PullAttempt, MergeAttempt

handlers.DrawWeapon = function (args, context) {
    // args: { level: number, count: number, costPerDraw: number }
    var level = args.level;
    var cost = args.cost || 1; // 1회당 CP 비용 (클라이언트 전달)

    if (level === undefined || level === null) {
        return { error: "level is required" };
    }

    var playFabId = currentPlayerId;

    // 1) CP 차감
    try {
        var subtractResp = server.SubtractUserVirtualCurrency({
            PlayFabId: playFabId,
            VirtualCurrency: "CP",
            Amount: cost
        });
    } catch (e) {
        return { error: "failed to subtract CP", detail: e };
    }

    // 2) 확률 데이터 로드
    var drawPercentage = GetProbabilityData("WeaponRate", level);
    var cumulative = GetCumulativeProbability(drawPercentage);

    // 3) 인벤토리 로드
    var getUserDataResp = server.GetUserData({
        PlayFabId: playFabId,
        Keys: ["Weapon"]
    });

    var inventory = [];
    if (getUserDataResp.Data && getUserDataResp.Data.Weapon && getUserDataResp.Data.Weapon.Value) {
        try {
            inventory = JSON.parse(getUserDataResp.Data.Weapon.Value);
        } catch (e) {
            inventory = [];
        }
    }

    // 4) 여러 번 뽑기
    var pulledItems = [];

    for (var n = 0; n < cost; n++) {
        var pickedIndex = GetPickedIndex(cumulative);
        var idIndex = (pickedIndex + 1) + Math.floor(Math.random() * 4) * 10;//pickedIndex + 1 바닐라 무기 제외이므로
        var weaponId = "Weapon_" + idIndex;

        // 인벤토리에 추가 또는 count 증가
        var found = false;
        for (var i = 0; i < inventory.length; i++) {
            if (inventory[i].id === weaponId) {
                inventory[i].count = (inventory[i].count || 0) + 1;
                found = true;
                break;
            }
        }

        if (!found) {
            inventory.push({ id: weaponId, count: 0, level: 0 });
        }

        pulledItems.push(weaponId);
    }

    // 5) 인벤토리 저장
    try {
        server.UpdateUserData({
            PlayFabId: playFabId,
            Data: { Weapon: JSON.stringify(inventory) }
        });
    } catch (e) {
        try {
            server.AddUserVirtualCurrency({
                PlayFabId: playFabId,
                VirtualCurrency: "CP",
                Amount: cost
            });
        } catch (e2) {
            return {
                error: "failed to save inventory and refund CP",
                detail: { saveError: e, refundError: e2 }
            };
        }
        return { error: "failed to save inventory, CP refunded", detail: e };
    }

    return {
        success: true,
        pulledItems: pulledItems,
    };
};


handlers.UpgradeAllWeaponsMax = function (args, context) {
    var playFabId = currentPlayerId;

    // k 배열 (rank별 강화비용 보정)
    var k = [1, 1, 1, 1, 1, 2, 3, 4, 5];

    // 1) UserData 로드
    var getData = server.GetUserData({ PlayFabId: playFabId, Keys: ["Weapon"] });
    var weaponList = [];
    if (getData.Data && getData.Data.Weapon && getData.Data.Weapon.Value) {
        try {
            weaponList = JSON.parse(getData.Data.Weapon.Value);
        } catch (e) {
            weaponList = [];
        }
    }

    if (weaponList.length === 0)
        return { error: "No weapons found in inventory" };

    // 2) 모든 무기에 대해 최대 업그레이드 시도
    var changedWeapons = [];

    for (var i = 0; i < weaponList.length; i++) {
        var weapon = weaponList[i];

        // id에서 rank 추출
        var idNum = parseInt(weapon.id.split("_")[1]);
        var rank = idNum % 10;
        if (isNaN(rank) || rank <= 0 || rank > 9)
            continue;

        var changed = false;

        // Upgrade loop
        while (true) {
            var cost = Math.floor((weapon.level + 1) / k[rank - 1]);
            if (cost <= 0) cost = 1;

            if (weapon.count < cost) break;

            weapon.count -= cost;
            weapon.level += 1;
            changed = true;
        }

        if (changed)
            changedWeapons.push(weapon);
    }

    // 3) 변경사항 저장
    if (changedWeapons.length > 0) {
        try {
            server.UpdateUserData({
                PlayFabId: playFabId,
                Data: { Weapon: JSON.stringify(weaponList) }
            });
        } catch (e) {
            return { error: "failed to save Weapon data", detail: e };
        }
    }

    // 4) 결과 반환
    return {
        success: true,
        upgradedWeapons: changedWeapons,
        updatedInventory: weaponList
    };
};

handlers.DrawGadget = function (args, context) {
    // args: { level: number, count: number, costPerDraw: number }
    var level = args.level;
    var cost = args.cost || 1; // 1회당 CP 비용 (클라이언트 전달)

    if (level === undefined || level === null) {
        return { error: "level is required" };
    }

    var playFabId = currentPlayerId;

    // 1) CP 차감
    try {
        var subtractResp = server.SubtractUserVirtualCurrency({
            PlayFabId: playFabId,
            VirtualCurrency: "CP",
            Amount: cost
        });
    } catch (e) {
        return { error: "failed to subtract CP", detail: e };
    }

    // 2) 확률 데이터 로드
    var drawPercentage = GetProbabilityData("WeaponRate", level);
    var cumulative = GetCumulativeProbability(drawPercentage);

    // 3) 인벤토리 로드
    var getUserDataResp = server.GetUserData({
        PlayFabId: playFabId,
        Keys: ["Gadget"]
    });

    var inventory = [];
    if (getUserDataResp.Data && getUserDataResp.Data.Gadget && getUserDataResp.Data.Gadget.Value) {
        try {
            inventory = JSON.parse(getUserDataResp.Data.Gadget.Value);
        } catch (e) {
            inventory = [];
        }
    }

    // 4) 여러 번 뽑기
    var pulledItems = [];

    for (var n = 0; n < cost; n++) {
        var pickedIndex = GetPickedIndex(cumulative);
        var idIndex = (pickedIndex) + Math.floor(Math.random() * 20) * 9; //파츠종류 * 랭크 * 총기 종류
        var gadgetId = "Gadget_" + idIndex;

        // 인벤토리에 추가 또는 count 증가
        var found = false;
        for (var i = 0; i < inventory.length; i++) {
            if (inventory[i].id === gadgetId) {
                inventory[i].count = (inventory[i].count || 0) + 1;
                found = true;
                break;
            }
        }

        if (!found) {
            inventory.push({ id: gadgetId, count: 0, level: 0 });
        }

        pulledItems.push(gadgetId);
    }

    // 5) 인벤토리 저장
    try {
        server.UpdateUserData({
            PlayFabId: playFabId,
            Data: { Gadget: JSON.stringify(inventory) }
        });
    } catch (e) {
        try {
            server.AddUserVirtualCurrency({
                PlayFabId: playFabId,
                VirtualCurrency: "CP",
                Amount: cost
            });
        } catch (e2) {
            return {
                error: "failed to save inventory and refund CP",
                detail: { saveError: e, refundError: e2 }
            };
        }
        return { error: "failed to save inventory, CP refunded", detail: e };
    }

    return {
        success: true,
        pulledItems: pulledItems,
    };
};


handlers.UpgradeAllGadgetsMax = function (args, context) {
    var playFabId = currentPlayerId;

    // k 배열 (rank별 강화비용 보정)
    var k = [1, 1, 1, 1, 1, 2, 3, 4, 5];

    // 1) UserData 로드
    var getData = server.GetUserData({ PlayFabId: playFabId, Keys: ["Gadget"] });
    var gadgetList = [];
    if (getData.Data && getData.Data.Gadget && getData.Data.Gadget.Value) {
        try {
            gadgetList = JSON.parse(getData.Data.Gadget.Value);
        } catch (e) {
            gadgetList = [];
        }
    }

    if (gadgetList.length === 0)
        return { error: "No gadgets found in inventory" };

    // 2) 모든 무기에 대해 최대 업그레이드 시도
    var changedGadgets = [];

    for (var i = 0; i < gadgetList.length; i++) {
        var gadget = gadgetList[i];

        // id에서 rank 추출
        var idNum = parseInt(gadget.id.split("_")[1]);
        var rank = idNum % 10;
        if (isNaN(rank) || rank <= 0 || rank > 9)
            continue;

        var changed = false;

        // Upgrade loop
        while (true) {
            var cost = Math.floor((gadget.level + 1) / k[rank - 1]);
            if (cost <= 0) cost = 1;

            if (gadget.count < cost) break;

            gadget.count -= cost;
            gadget.level += 1;
            changed = true;
        }

        if (changed)
            changedGadgets.push(gadget);
    }

    // 3) 변경사항 저장
    if (changedGadgets.length > 0) {
        try {
            server.UpdateUserData({
                PlayFabId: playFabId,
                Data: { Gadget: JSON.stringify(gadgetList) }
            });
        } catch (e) {
            return { error: "failed to save Weapon data", detail: e };
        }
    }

    // 4) 결과 반환
    return {
        success: true,
        upgradedGadgets: changedGadgets,
        updatedInventory: gadgetList
    };
};
handlers.JoinGlobalChat = function (args, context) {
    var groupId = "GlobalChat";

    try {
        var result = server.AddSharedGroupMembers({
            SharedGroupId: groupId,
            PlayFabIds: [currentPlayerId]
        });
        return { success: true, message: "Joined GlobalChat!" };
    } catch (e) {
        return { success: false, error: e };
    }
};
