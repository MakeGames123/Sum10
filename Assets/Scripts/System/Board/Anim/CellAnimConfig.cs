using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CellAnimConfig", menuName = "ScriptableObject/CellAnimConfig")]
public class CellAnimConfig : ScriptableObject
{
    [Header("Select Animation Settings")]
    public float selectAnimDuration = 0.25f;   // 탄력 효과 보이게
    public float selectScalePunch = 1.12f;     // 처음 커지는 정도
    public float selectJumpHeight = 8f;        // 위로 점프 높이
    public float selectRotation = 6f;          // 살짝 기울어지는 각도

    [Header("Flip Select Animation Settings (B)")]
    public float flipDuration = 0.25f;         // 뒤집기 전체 시간 (1.2배 빠르게)

    [Header("Hint Animation Settings")]
    public float hintBounceDuration = 0.2f;    // 한 번 바운스 시간
    public float hintScalePunch = 1.15f;       // 커지는 정도
    public float hintJumpHeight = 12f;         // 위로 점프 높이
    public int hintBounceCount = 3;            // 연속 바운스 횟수
    public float hintPauseDuration = 1.2f;     // 바운스 세트 사이 멈춤 시간

    [Header("Tile Effect Settings")]
    public float disappearDuration = 0.18f;       // 사라짐 전체 시간
    public float disappearScalePeak = 1.2f;       // 사라질 때 처음 커지는 정도
    public float disappearJumpHeight = 25f;       // 위로 점프 높이 (px)
    public float appearDelay = 0.1f;              // 젤리발 등장 딜레이
    public float appearDuration = 0.15f;          // 젤리발 등장 시간
    public float appearScalePeak = 1.1f;          // 등장 시 톡 튀어나오는 정도

    [Header("Spawn Animation Settings")]
    public float spawnDuration = 0.25f;           // 생성 애니메이션 시간
    public float spawnScalePeak = 1.15f;          // 생성 시 톡 튀어나오는 정도
    public float spawnDropHeight = 30f;            // 위에서 떨어지는 높이
}
