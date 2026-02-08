using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using DG.Tweening;

/// <summary>
/// 랭킹 제치기 애니메이션 (Bronze League 스타일)
/// </summary>
public class RankingOvertakeAnimation : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameOverScoreUnit myRowPrefab;
    [SerializeField] private GameOverScoreUnit otherRowPrefab;
    [SerializeField] private RectTransform rowContainer;

    [Header("Layout Settings")]
    [SerializeField] private int visibleRowCount = 6;
    [SerializeField] private int myFixedIndex = 3;
    [SerializeField] private float rowHeight = 80f;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private float startYOffset = 0f;
    [SerializeField] private float maxScrollOverflow = 100f;

    [Header("Animation Settings")]
    [SerializeField] private float initialDisplayDelay = 0.3f;
    [SerializeField] private float extractDuration = 0.25f;
    [SerializeField] private float minScrollDuration = 1.0f;   // 1칸일 때 스크롤 시간
    [SerializeField] private float maxScrollDuration = 2.0f;   // 15칸일 때 스크롤 시간
    [SerializeField] private int maxVisualScrollSteps = 15;    // 시각적 스크롤 최대 칸수 (999→1도 15칸만 스크롤)

    public int MaxVisualScrollSteps => maxVisualScrollSteps;
    [SerializeField] private float insertDuration = 0.3f;
    [SerializeField] private float myRowPopScale = 1.08f;
    [SerializeField] private Ease scrollEase = Ease.InOutCubic;  // Slow-Fast-Slow
    [SerializeField] private Ease insertEase = Ease.OutBack;

    [Header("Recycle Animation")]
    [SerializeField] private float recycleTopOffset = 1.0f;    // 상단 재활용 기준 (화면 위 몇 칸)
    [SerializeField] private float recycleBottomOffset = 1.0f; // 하단 재활용 기준 (화면 아래 몇 칸)
    [SerializeField] private float recyclePopScale = 0.85f;
    [SerializeField] private float recyclePopDuration = 0.15f;

    [Header("Swap Animation (1~4등 변동용)")]
    [SerializeField] private float swapDuration = 0.3f;
    [SerializeField] private Ease swapEase = Ease.InOutQuad;

    [Header("Test (Editor Only)")]
    [SerializeField] private int testPrevRank = 10;
    [SerializeField] private int testCurrentRank = 5;
    [SerializeField] private int testMyScore = 100;
    [SerializeField] private bool runTest = false;

    // Pool
    private GameOverScoreUnit myRow;
    private RectTransform myRowRect;
    private List<GameOverScoreUnit> otherPool = new();
    private List<RectTransform> otherPoolRects = new();

    // State
    private float totalRowHeight;
    private Coroutine animCoroutine;
    private List<PlayerLeaderboardEntry> leaderboardData;
    private int currentDisplayRank;
    private string currentPlayerId;
    private int currentMyScore;
    private float extractMyY;  // Extract 시점의 내 Row Y 위치 (위/아래 구분 기준)
    private Dictionary<int, PlayerLeaderboardEntry> rankMap = new(); // 점수 기반 순위 맵 (Position 전파 지연 우회)

    // Recycling state
    private int[] poolDisplayRanks;  // 각 pool row가 현재 표시 중인 rank
    private float visibleTopY;       // 보이는 영역 상단
    private float visibleBottomY;    // 보이는 영역 하단

    private void Awake()
    {
        totalRowHeight = rowHeight + rowSpacing;
        InitializePool();
    }

    private void Update()
    {
        if (runTest)
        {
            runTest = false;
            TestPlay();
        }
    }

    private void InitializePool()
    {
        // 타인 Row 풀 먼저 생성 (뒤에 배치)
        int poolSize = visibleRowCount - 1;  // 내 Row 제외한 나머지
        poolDisplayRanks = new int[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            var row = Instantiate(otherRowPrefab, rowContainer);
            var rect = row.GetComponent<RectTransform>();

            otherPool.Add(row);
            otherPoolRects.Add(rect);
            row.gameObject.SetActive(false);
            poolDisplayRanks[i] = -1;
        }

        // 내 Row 나중에 생성 (앞에 배치)
        myRow = Instantiate(myRowPrefab, rowContainer);
        myRowRect = myRow.GetComponent<RectTransform>();
        myRow.gameObject.SetActive(false);

        // 보이는 영역 계산
        visibleTopY = startYOffset + maxScrollOverflow;
        visibleBottomY = startYOffset - (visibleRowCount - 1) * totalRowHeight - maxScrollOverflow;
    }

    public void Play(int prevRank, int currentRank, List<PlayerLeaderboardEntry> data,
                     int myScore, string myPlayerId, System.Action onComplete = null)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        // Reset
        myRow.gameObject.SetActive(false);
        myRow.transform.localScale = Vector3.one;
        for (int i = 0; i < otherPool.Count; i++)
        {
            otherPool[i].gameObject.SetActive(false);
            otherPool[i].transform.localScale = Vector3.one;
            if (poolDisplayRanks != null && i < poolDisplayRanks.Length)
                poolDisplayRanks[i] = -1;
        }

        leaderboardData = data;
        currentPlayerId = myPlayerId;
        BuildRankMap(currentRank);

        // 주간 최고기록 표시: 리더보드 점수 우선, 없으면 max(방금판, 리더보드)
        var myEntry = FindEntryByPlayerId(myPlayerId);
        currentMyScore = myEntry != null
            ? Mathf.Max(myEntry.StatValue, myScore)
            : myScore;

        animCoroutine = StartCoroutine(AnimationSequence(prevRank, currentRank, onComplete));
    }

    private IEnumerator AnimationSequence(int prevRank, int currentRank, System.Action onComplete)
    {
        int actualSteps = Mathf.Abs(prevRank - currentRank);
        bool isRising = prevRank > currentRank;

        // 시각적 스크롤 제한: 999→1도 최대 15칸만 스크롤
        // 하지만 숫자는 999→1로 표시
        int visualSteps = Mathf.Min(actualSteps, maxVisualScrollSteps);
        int visualPrevRank = isRising ? currentRank + visualSteps : currentRank - visualSteps;
        visualPrevRank = Mathf.Max(1, visualPrevRank);

        // 케이스 판정 (myFixedIndex + 1 = 4등이 경계)
        int boundary = myFixedIndex + 1;
        bool prevInSwapZone = visualPrevRank <= boundary;
        bool currentInSwapZone = currentRank <= boundary;

        // 케이스 1: 둘 다 4등 밖 → Extract/Scroll/Insert
        // 케이스 2: 둘 다 1~4등 범위 내 → Swap only
        // 케이스 3: 4등 밖 → 1~3등 (상승) → Extract/Scroll/Insert + Swap
        // 케이스 4: 1~3등 → 4등 밖 (하락) → 케이스 1처럼 바로 Extract/Scroll/Insert
        bool isCase2 = prevInSwapZone && currentInSwapZone;
        bool isCase3 = !prevInSwapZone && currentInSwapZone;  // 4등 밖 → 1~3등
        bool isCase4 = prevInSwapZone && !currentInSwapZone;  // 1~3등 → 4등 밖

        // Phase 0: Initial Display
        // 화면 배치는 visualPrevRank 기준, 숫자는 실제 prevRank 표시
        SetupInitialDisplay(visualPrevRank, currentRank, isCase2 || isCase3);
        currentDisplayRank = prevRank;  // 숫자는 실제 순위로 표시
        myRow.SetCondition(prevRank, currentMyScore, currentPlayerId);
        yield return new WaitForSeconds(initialDisplayDelay);

        if (actualSteps == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (isCase2)
        {
            // 케이스 2: 인접 행 교체 애니메이션만
            yield return StartCoroutine(PhaseSwapAnimation(visualPrevRank, currentRank, isRising, visualSteps));
        }
        else if (isCase3)
        {
            // 케이스 3: 4등 밖 → 1~3등 (상승)
            // 스크롤: prevRank → boundary (숫자: 999 → 4)
            // Swap: boundary → currentRank (숫자: 4 → 1)
            int scrollSteps = visualPrevRank - boundary;
            int swapSteps = boundary - currentRank;
            int scrollActualSteps = prevRank - boundary;  // 숫자 애니메이션: 999 → 4

            yield return StartCoroutine(PhaseExtract(visualPrevRank));
            yield return StartCoroutine(PhaseSmoothScroll(prevRank, boundary, true, scrollSteps, scrollActualSteps));
            yield return StartCoroutine(PhaseInsert(boundary));
            yield return StartCoroutine(PhaseSwapAnimation(boundary, currentRank, true, swapSteps));
        }
        else if (isCase4)
        {
            // 케이스 4: 1~3등 → 4등 밖 (하락) - 케이스 1처럼 바로 처리
            yield return StartCoroutine(PhaseExtract(visualPrevRank));
            yield return StartCoroutine(PhaseSmoothScroll(prevRank, currentRank, false, visualSteps, actualSteps));
            yield return StartCoroutine(PhaseInsert(currentRank));
        }
        else
        {
            // 케이스 1: 기존 Extract → Scroll → Insert
            yield return StartCoroutine(PhaseExtract(visualPrevRank));
            yield return StartCoroutine(PhaseSmoothScroll(prevRank, currentRank, isRising, visualSteps, actualSteps));
            yield return StartCoroutine(PhaseInsert(currentRank));
        }

        myRow.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 1);
        onComplete?.Invoke();
    }

    private void SetupInitialDisplay(int myRank, int finalRank, bool isCase2 = false)
    {
        currentDisplayRank = myRank;
        int myIndex = Mathf.Min(myRank - 1, myFixedIndex);
        float myY = startYOffset - myIndex * totalRowHeight;

        // 내 Row (이전 순위로 시작)
        myRow.gameObject.SetActive(true);
        myRowRect.anchoredPosition = new Vector2(0, myY);
        myRow.SetCondition(myRank, currentMyScore, currentPlayerId);
        myRow.transform.SetAsLastSibling();

        // poolDisplayRanks 초기화
        for (int i = 0; i < poolDisplayRanks.Length; i++)
            poolDisplayRanks[i] = -1;

        // 다른 Row 배치 - 케이스에 따라 다르게
        int otherIdx = 0;
        for (int i = 0; i < visibleRowCount; i++)
        {
            if (i == myIndex) continue;

            // 케이스 2는 실제 순위 기준, 케이스 1은 시작 순위 기준
            int displayRank = myRank + (i - myIndex);
            if (displayRank < 1) continue;

            float y = startYOffset - i * totalRowHeight;

            if (otherIdx < otherPool.Count)
            {
                otherPool[otherIdx].gameObject.SetActive(true);
                otherPool[otherIdx].transform.localScale = Vector3.one;
                otherPoolRects[otherIdx].anchoredPosition = new Vector2(0, y);
                SetRowContent(otherIdx, displayRank);
                poolDisplayRanks[otherIdx] = displayRank;
                otherIdx++;
            }
        }
    }

    private IEnumerator PhaseExtract(int myRank)
    {
        int myIndex = Mathf.Min(myRank - 1, myFixedIndex);
        extractMyY = startYOffset - myIndex * totalRowHeight;

        // 내 Row 팝아웃
        myRow.transform.SetAsLastSibling();
        myRow.transform.DOScale(myRowPopScale, extractDuration).SetEase(Ease.OutBack);

        // 위 Row는 0.5칸 아래로, 아래 Row는 0.5칸 위로 → 갭 메움 (간격 유지)
        float halfHeight = totalRowHeight * 0.5f;
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (!otherPool[i].gameObject.activeSelf) continue;
            float currentY = otherPoolRects[i].anchoredPosition.y;

            if (currentY > extractMyY + 0.1f)  // 위에 있는 Row -> 아래로 0.5칸
            {
                otherPoolRects[i].DOAnchorPosY(currentY - halfHeight, extractDuration).SetEase(Ease.OutQuad);
            }
            else if (currentY < extractMyY - 0.1f)  // 아래에 있는 Row -> 위로 0.5칸
            {
                otherPoolRects[i].DOAnchorPosY(currentY + halfHeight, extractDuration).SetEase(Ease.OutQuad);
            }
        }

        yield return new WaitForSeconds(extractDuration);
    }

    private IEnumerator PhaseSmoothScroll(int prevRank, int currentRank, bool isRising, int visualSteps, int actualSteps)
    {
        // 스크롤 방향: 순위 상승(숫자 감소) = rows가 아래로, 순위 하락(숫자 증가) = rows가 위로
        float scrollDir = isRising ? -1f : 1f;
        float totalScrollDistance = scrollDir * visualSteps * totalRowHeight;  // 시각적 스크롤은 visualSteps만큼

        // 스크롤 시간: 1칸=1초, 15칸=2초, 선형 보간 (Extract 시간 제외)
        float stepRatio = Mathf.Clamp01((float)(visualSteps - 1) / (maxVisualScrollSteps - 1));
        float totalAnimDuration = Mathf.Lerp(minScrollDuration, maxScrollDuration, stepRatio);
        float scrollDuration = Mathf.Max(0.5f, totalAnimDuration - extractDuration);

        // 각 Row의 시작 Y 위치 저장
        float[] startYPositions = new float[otherPool.Count];
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (otherPool[i].gameObject.activeSelf)
                startYPositions[i] = otherPoolRects[i].anchoredPosition.y;
        }

        // 재활용 경계 (Inspector에서 조정 가능)
        float recycleTopThreshold = startYOffset + recycleTopOffset * totalRowHeight;
        float recycleBottomThreshold = startYOffset - (visibleRowCount - 1 + recycleBottomOffset) * totalRowHeight;

        // 스크롤 중 내 순위 숫자 업데이트 (80% 시점에 완료)
        // 숫자는 actualSteps만큼 변화 (999→1)
        float rankAnimDuration = scrollDuration * 0.8f;
        float elapsed = 0f;
        int lastStep = 0;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scrollDuration);
            float easedT = DOVirtual.EasedValue(0f, 1f, t, scrollEase);
            float currentScrollOffset = easedT * totalScrollDistance;

            // 각 Row 위치 업데이트 + 재활용 체크
            for (int i = 0; i < otherPool.Count; i++)
            {
                if (!otherPool[i].gameObject.activeSelf) continue;

                float newY = startYPositions[i] + currentScrollOffset;
                otherPoolRects[i].anchoredPosition = new Vector2(0, newY);

                // 재활용 체크 (완전히 벗어났을 때만)
                if (isRising && newY < recycleBottomThreshold)
                {
                    // 아래로 벗어남 → 위로 재활용
                    RecycleRowToTop(i, ref startYPositions[i], currentScrollOffset);
                }
                else if (!isRising && newY > recycleTopThreshold)
                {
                    // 위로 벗어남 → 아래로 재활용
                    RecycleRowToBottom(i, ref startYPositions[i], currentScrollOffset);
                }
            }

            // 내 순위 숫자 애니메이션 (80% 시점까지) - actualSteps 사용
            if (elapsed <= rankAnimDuration)
            {
                float progress = Mathf.Clamp01(elapsed / rankAnimDuration);
                int currentStep = Mathf.FloorToInt(progress * actualSteps);

                if (currentStep > lastStep)
                {
                    currentDisplayRank = isRising ? prevRank - currentStep : prevRank + currentStep;
                    currentDisplayRank = Mathf.Max(1, currentDisplayRank);  // 순위는 1 미만 불가
                    myRow.SetCondition(currentDisplayRank, currentMyScore, currentPlayerId);
                    lastStep = currentStep;
                }

                if (progress >= 1f && currentDisplayRank != currentRank)
                {
                    currentDisplayRank = currentRank;
                    myRow.SetCondition(currentDisplayRank, currentMyScore, currentPlayerId);
                }
            }

            yield return null;
        }

        // 최종 위치 확정
        currentDisplayRank = currentRank;
        myRow.SetCondition(currentDisplayRank, currentMyScore, currentPlayerId);
    }

    private void RecycleRowToTop(int poolIdx, ref float startY, float currentOffset)
    {
        // 현재 가장 위에 있는 Row 찾기
        float highestY = float.MinValue;
        int highestRank = int.MaxValue;
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (!otherPool[i].gameObject.activeSelf || i == poolIdx) continue;
            float y = otherPoolRects[i].anchoredPosition.y;
            if (y > highestY)
            {
                highestY = y;
                highestRank = poolDisplayRanks[i];
            }
        }

        // 새 위치와 순위 계산
        int newRank = highestRank - 1;
        if (newRank < 1)
        {
            otherPool[poolIdx].gameObject.SetActive(false);
            return;
        }

        float newY = highestY + totalRowHeight;
        startY = newY - currentOffset;

        // 즉시 위치 이동 + Scale in
        otherPoolRects[poolIdx].anchoredPosition = new Vector2(0, newY);
        poolDisplayRanks[poolIdx] = newRank;
        SetRowContent(poolIdx, newRank);

        otherPool[poolIdx].transform.localScale = Vector3.one * recyclePopScale;
        otherPool[poolIdx].transform.DOScale(1f, recyclePopDuration).SetEase(Ease.OutBack);
    }

    private void RecycleRowToBottom(int poolIdx, ref float startY, float currentOffset)
    {
        // 현재 가장 아래에 있는 Row 찾기
        float lowestY = float.MaxValue;
        int lowestRank = int.MinValue;
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (!otherPool[i].gameObject.activeSelf || i == poolIdx) continue;
            float y = otherPoolRects[i].anchoredPosition.y;
            if (y < lowestY)
            {
                lowestY = y;
                lowestRank = poolDisplayRanks[i];
            }
        }

        // 새 위치와 순위 계산
        int newRank = lowestRank + 1;
        float newY = lowestY - totalRowHeight;
        startY = newY - currentOffset;

        // 즉시 위치 이동 + Scale in
        otherPoolRects[poolIdx].anchoredPosition = new Vector2(0, newY);
        poolDisplayRanks[poolIdx] = newRank;
        SetRowContent(poolIdx, newRank);

        otherPool[poolIdx].transform.localScale = Vector3.one * recyclePopScale;
        otherPool[poolIdx].transform.DOScale(1f, recyclePopDuration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 케이스 2: 인접 행 교체 애니메이션 (1~4등 내 변동)
    /// 4→1등: 4↔3, 3↔2, 2↔1 순서로 교체
    /// </summary>
    private IEnumerator PhaseSwapAnimation(int prevRank, int currentRank, bool isRising, int totalSteps)
    {
        // 내 Row 현재 인덱스 (케이스 2에서는 실제 순위-1이 인덱스)
        int myCurrentIndex = prevRank - 1;

        for (int step = 0; step < totalSteps; step++)
        {
            // 교체할 인접 인덱스 결정
            int adjacentIndex = isRising ? myCurrentIndex - 1 : myCurrentIndex + 1;
            if (adjacentIndex < 0 || adjacentIndex >= visibleRowCount) break;

            // 인접 행 찾기 (해당 인덱스에 있는 Row)
            float targetY = startYOffset - adjacentIndex * totalRowHeight;
            float myCurrentY = startYOffset - myCurrentIndex * totalRowHeight;

            int adjacentPoolIdx = -1;
            for (int i = 0; i < otherPool.Count; i++)
            {
                if (!otherPool[i].gameObject.activeSelf) continue;
                float y = otherPoolRects[i].anchoredPosition.y;
                if (Mathf.Abs(y - targetY) < 1f)
                {
                    adjacentPoolIdx = i;
                    break;
                }
            }

            // 내 Row와 인접 Row 위치 교체 애니메이션
            myRow.transform.SetAsLastSibling();
            myRowRect.DOAnchorPosY(targetY, swapDuration).SetEase(swapEase);

            if (adjacentPoolIdx >= 0)
            {
                otherPoolRects[adjacentPoolIdx].DOAnchorPosY(myCurrentY, swapDuration).SetEase(swapEase);

                // 인접 행 순위 숫자만 업데이트 (플레이어 정보는 유지, 순위만 변경)
                int adjacentNewRank = Mathf.Max(1, currentDisplayRank);  // 순위는 1 미만 불가
                poolDisplayRanks[adjacentPoolIdx] = adjacentNewRank;
                otherPool[adjacentPoolIdx].SetRank(adjacentNewRank);
            }

            // 내 순위 숫자 업데이트
            int newRank = isRising ? currentDisplayRank - 1 : currentDisplayRank + 1;
            currentDisplayRank = Mathf.Max(1, newRank);  // 순위는 1 미만 불가
            myRow.SetCondition(currentDisplayRank, currentMyScore, currentPlayerId);

            yield return new WaitForSeconds(swapDuration);

            // 인덱스 업데이트
            myCurrentIndex = adjacentIndex;
        }

        // 최종 순위 확인
        currentDisplayRank = currentRank;
        myRow.SetCondition(currentDisplayRank, currentMyScore, currentPlayerId);
    }

    private IEnumerator PhaseInsert(int finalRank)
    {
        // 모든 트윈 정리 (위치 + 스케일)
        for (int i = 0; i < otherPool.Count; i++)
        {
            DOTween.Kill(otherPoolRects[i]);
            DOTween.Kill(otherPool[i].transform);
            otherPool[i].transform.localScale = Vector3.one;
        }

        int finalMyIndex = Mathf.Min(finalRank - 1, myFixedIndex);
        float finalMyY = startYOffset - finalMyIndex * totalRowHeight;

        // 내 Row 최종 위치
        myRow.transform.SetAsLastSibling();
        myRow.transform.DOScale(1f, insertDuration).SetEase(Ease.OutQuad);
        myRowRect.DOAnchorPos(new Vector2(0, finalMyY), insertDuration).SetEase(insertEase);

        // 타겟 슬롯 목록 (위에서 아래 순서)
        List<(float y, int rank)> targetSlots = new();
        for (int i = 0; i < visibleRowCount; i++)
        {
            if (i == finalMyIndex) continue;

            int rank = finalRank + (i - finalMyIndex);
            if (rank < 1) continue;

            float y = startYOffset - i * totalRowHeight;
            targetSlots.Add((y, rank));
        }

        // 활성 Row들을 현재 Y 위치로 정렬 (위쪽이 먼저)
        List<int> sortedIndices = new();
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (otherPool[i].gameObject.activeSelf)
                sortedIndices.Add(i);
        }
        sortedIndices.Sort((a, b) =>
            otherPoolRects[b].anchoredPosition.y.CompareTo(otherPoolRects[a].anchoredPosition.y));

        // 정렬된 순서대로 타겟 슬롯에 배치
        for (int t = 0; t < targetSlots.Count && t < sortedIndices.Count; t++)
        {
            int poolIdx = sortedIndices[t];
            float targetY = targetSlots[t].y;
            int rank = targetSlots[t].rank;

            SetRowContent(poolIdx, rank);
            otherPoolRects[poolIdx].DOAnchorPosY(targetY, insertDuration).SetEase(insertEase);
        }

        // 남는 Row 비활성화
        for (int t = targetSlots.Count; t < sortedIndices.Count; t++)
            otherPool[sortedIndices[t]].gameObject.SetActive(false);

        yield return new WaitForSeconds(insertDuration);
    }

    private void SetRowContent(int poolIndex, int rank)
    {
        rank = Mathf.Max(1, rank);  // 순위는 1 미만 불가
        var entry = FindEntryByRankExcludePlayer(rank, currentPlayerId);
        if (entry != null)
            otherPool[poolIndex].SetCondition(rank, entry.StatValue, entry.PlayFabId);
        else
            otherPool[poolIndex].SetCondition(rank, 0, "---");
    }

    // ========== Helper Methods ==========

    /// <summary>
    /// 순위 맵 생성: "제거 후 삽입" 방식 (PlayFab Position 전파 지연 + 동점자 타이브레이킹 차이 모두 우회)
    /// 1단계: 데이터에서 플레이어를 제거 → 아래 엔트리가 한 칸 올라감
    /// 2단계: currentRank 위치에 플레이어 삽입 → 해당 순위 이하 엔트리가 한 칸 밀림
    /// </summary>
    private void BuildRankMap(int currentRank)
    {
        rankMap.Clear();
        if (leaderboardData == null) return;

        // 플레이어의 데이터상 Position 찾기
        int playerPos = int.MaxValue;
        foreach (var entry in leaderboardData)
        {
            if (entry.PlayFabId == currentPlayerId)
            {
                playerPos = entry.Position;
                break;
            }
        }

        foreach (var entry in leaderboardData)
        {
            if (entry.PlayFabId == currentPlayerId) continue;

            // Step 1: 플레이어 제거 후 순위 (플레이어보다 아래 엔트리는 한 칸 올라감)
            int rankAfterRemove = entry.Position < playerPos
                ? entry.Position + 1
                : entry.Position;

            // Step 2: currentRank에 플레이어 삽입 (해당 순위 이상은 한 칸 밀림)
            int displayRank = rankAfterRemove < currentRank
                ? rankAfterRemove
                : rankAfterRemove + 1;

            rankMap[displayRank] = entry;
        }
    }

    private PlayerLeaderboardEntry FindEntryByRankExcludePlayer(int rank, string excludePlayerId)
    {
        return rankMap.TryGetValue(rank, out var entry) ? entry : null;
    }

    private PlayerLeaderboardEntry FindEntryByPlayerId(string playerId)
    {
        if (leaderboardData == null) return null;
        foreach (var entry in leaderboardData)
        {
            if (entry.PlayFabId == playerId)
                return entry;
        }
        return null;
    }

    // ========== Test ==========

    [ContextMenu("Test Overtake Animation")]
    public void TestPlay()
    {
        Stop();

        if (myRow == null || poolDisplayRanks == null)
        {
            totalRowHeight = rowHeight + rowSpacing;
            InitializePool();
        }

        // 보이는 영역 재계산 (Inspector 값 변경 대응)
        visibleTopY = startYOffset + maxScrollOverflow;
        visibleBottomY = startYOffset - (visibleRowCount - 1) * totalRowHeight - maxScrollOverflow;

        var fakeData = new List<PlayerLeaderboardEntry>();
        int fromRank = Mathf.Max(1, Mathf.Min(testPrevRank, testCurrentRank) - 10);
        int toRank = Mathf.Max(testPrevRank, testCurrentRank) + 10;

        for (int rank = fromRank; rank <= toRank; rank++)
        {
            var entry = new PlayerLeaderboardEntry
            {
                Position = rank - 1,
                PlayFabId = rank == testCurrentRank ? "TEST_MY_ID" : $"Player_{rank}",
                StatValue = Mathf.Max(0, 1000 - (rank - 1) * 10)
            };
            fakeData.Add(entry);
        }

        Play(testPrevRank, testCurrentRank, fakeData, testMyScore, "TEST_MY_ID", () =>
        {
            Debug.Log("[Test] Animation completed!");
        });
    }

    public void Stop()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if (myRowRect != null) DOTween.Kill(myRowRect);
        if (myRow != null) DOTween.Kill(myRow.transform);
        for (int i = 0; i < otherPool.Count; i++)
        {
            if (otherPoolRects[i] != null) DOTween.Kill(otherPoolRects[i]);
            if (otherPool[i] != null) DOTween.Kill(otherPool[i].transform);
        }
    }

    private void OnDestroy() => Stop();
}
