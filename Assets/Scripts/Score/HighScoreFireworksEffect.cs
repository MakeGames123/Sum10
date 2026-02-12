using UnityEngine;

/// <summary>
/// 하이스코어 달성 시 폭죽 효과
/// 파티클 재생 + 사운드 (선택) 담당
/// </summary>
public class HighScoreFireworksEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] fireworksParticles;  // 복수의 파티클 지원
    [SerializeField] private AudioSource fireworksSound;           // 폭죽 사운드 (선택)

    [Header("Settings")]
    [SerializeField] private bool playAllParticles = true;  // 모든 파티클 동시 재생

    /// <summary>
    /// 폭죽 효과 재생
    /// </summary>
    public void Play()
    {
        if (fireworksParticles != null && fireworksParticles.Length > 0)
        {
            if (playAllParticles)
            {
                foreach (var particle in fireworksParticles)
                {
                    if (particle != null)
                        particle.Play();
                }
            }
            else
            {
                // 첫 번째 파티클만 재생
                if (fireworksParticles[0] != null)
                    fireworksParticles[0].Play();
            }
        }

        if (fireworksSound != null)
            fireworksSound.Play();
    }

    /// <summary>
    /// 폭죽 효과 중지
    /// </summary>
    public void Stop()
    {
        if (fireworksParticles != null)
        {
            foreach (var particle in fireworksParticles)
            {
                if (particle != null)
                    particle.Stop();
            }
        }

        if (fireworksSound != null)
            fireworksSound.Stop();
    }

#if UNITY_EDITOR
    [ContextMenu("Test - Play Fireworks")]
    private void TestPlay()
    {
        Play();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            Play();
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Stop();
            foreach (var particle in fireworksParticles)
                if (particle != null)
                    particle.Clear();
            Play();
        }
    }
#endif
}
