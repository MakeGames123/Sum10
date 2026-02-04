handlers.createGhostAccounts = function (args) {
    const COUNT = 25; 
    const results = [];

    for (let i = 1; i <= COUNT; i++) {
        // 1. 서버용 커스텀 ID 로그인 (계정 생성)
        // 이 API는 Server API의 핵심이라 이름이 틀릴 리 없습니다.
        const loginResult = server.LoginWithServerCustomId({
            ServerCustomId: "GhostUser_V1_" + i, // 버전 관리를 위해 V1 추가
            CreateAccount: true
        });

        results.push(loginResult.PlayFabId);
    }

    // 닉네임 설정은 나중에 리더보드 점수 넣을 때 같이 해도 됩니다.
    return { 
        count: results.length,
        ghostIds: results 
    };
};
handlers.fillGhostLeaderboard = function () {

    const STAT = "HighScore";
    const ghostIds = [ "1D65FB95B6F1D8BF",
                "E093AA698FCFFBCF",
                "90C92346FB5223F7",
                "C41C9E6052D1454D",
                "F82455EA3B10C574",
                "56A018AC15F5F4C0",
                "7F8010DEF2DCF999",
                "E2DCBDB030C339A1",
                "F91725936ED1CCE2",
                "E1835A8B65BFA146",
                "13A8A904908CB125",
                "6662C2231D9CC5C0",
                "4D2A1220FCA22ED6",
                "121E437FD28BE4A8",
                "72E0FF0B174E0A23",
                "B3A7F1C39DBE5A7",
                "83BCD716C34732CB",
                "4D67CE04F4451153",
                "63C7601DB26D76D5",
                "4738EFC88198C05F",
                "82DE78D3C9E43121",
                "44CA3DF05471C3B1",
                "2190AAB81A18C541",
                "756394D94EC53444",
                "BADAAE323F5B3838"];

    for (let i = 0; i < ghostIds.length; i++) {
        const score = Math.floor(Math.random() * 451);

        server.UpdatePlayerStatistics({
            PlayFabId: ghostIds[i],
            Statistics: [{
                StatisticName: STAT,
                Value: score
            }]
        });
    }
    return { result: "ghost leaderboard updated" };
};

function generateGhostScore() {
    // 0 ~ 450 (정수)
    return Math.floor(Math.random() * 451);
}
function getGhostPlayFabId(index) {
    // 예: "GHOST_1", "GHOST_2" 식의 ID를 반환하거나 
    // 실제 존재하는 테스트용 유저 ID 리스트에서 가져오는 로직이 필요합니다.
    return "GHOST_ID_" + index; 
}