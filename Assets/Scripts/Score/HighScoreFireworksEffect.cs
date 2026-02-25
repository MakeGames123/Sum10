using UnityEngine;
using System.Collections;

/// <summary>
/// 하이스코어 달성 시 폭죽 효과
/// 파티클 재생 + 폭죽 사운드 (오프셋) + 함성 사운드 (페이드아웃)
/// </summary>
public class HighScoreFireworksEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] fireworksParticles;

    [Header("Fireworks Sound (중간 구간만 재생)")]
    [SerializeField] private AudioSource fireworksSource;
    [SerializeField] private AudioClip fireworksClip;
    [SerializeField] [Range(0f, 10f)] private float fireworksStartOffset = 0f;
    [SerializeField] [Range(0f, 10f)] private float fireworksEndOffset = 0f;
    [SerializeField] [Range(0f, 2f)] private float fireworksVolume = 1f;

    [Header("Cheer Sound (재생 후 페이드아웃)")]
    [SerializeField] private AudioSource cheerSource;
    [SerializeField] private AudioClip cheerClip;
    [SerializeField] [Range(0f, 2f)] private float cheerVolume = 1f;
    [SerializeField] private float cheerPlayDuration = 3f;
    [SerializeField] private float cheerFadeDuration = 3f;

    [Header("Settings")]
    [SerializeField] private bool playAllParticles = true;

    private Coroutine fireworksSoundCoroutine;
    private Coroutine cheerSoundCoroutine;

    /// <summary>
    /// 폭죽 효과 재생 (파티클 + 사운드 모두)
    /// </summary>
    public void Play()
    {
        PlayParticles();
        PlayFireworksSound();
        PlayCheerSound();
    }

    /// <summary>
    /// 폭죽 효과 중지
    /// </summary>
    public void Stop()
    {
        StopParticles();
        StopFireworksSound();
        StopCheerSound();
    }

    // ========== Particles ==========

    private void PlayParticles()
    {
        if (fireworksParticles == null || fireworksParticles.Length == 0) return;

        if (playAllParticles)
        {
            foreach (var particle in fireworksParticles)
                if (particle != null) particle.Play();
        }
        else
        {
            if (fireworksParticles[0] != null)
                fireworksParticles[0].Play();
        }
    }

    private void StopParticles()
    {
        if (fireworksParticles == null) return;
        foreach (var particle in fireworksParticles)
            if (particle != null) particle.Stop();
    }

    private void ClearParticles()
    {
        if (fireworksParticles == null) return;
        foreach (var particle in fireworksParticles)
            if (particle != null) particle.Clear();
    }

    // ========== Fireworks Sound ==========

    private void PlayFireworksSound()
    {
        if (fireworksSource == null || fireworksClip == null) return;

        if (fireworksSoundCoroutine != null) StopCoroutine(fireworksSoundCoroutine);
        fireworksSoundCoroutine = StartCoroutine(FireworksSoundRoutine());
    }

    private void StopFireworksSound()
    {
        if (fireworksSoundCoroutine != null)
        {
            StopCoroutine(fireworksSoundCoroutine);
            fireworksSoundCoroutine = null;
        }
        if (fireworksSource != null) fireworksSource.Stop();
    }

    private IEnumerator FireworksSoundRoutine()
    {
        float endTime = fireworksClip.length - fireworksEndOffset;
        if (endTime <= fireworksStartOffset) yield break;

        fireworksSource.clip = fireworksClip;
        fireworksSource.volume = fireworksVolume;
        fireworksSource.time = fireworksStartOffset;
        fireworksSource.Play();

        // endOffset 시점에서 정지
        while (fireworksSource.isPlaying && fireworksSource.time < endTime)
            yield return null;

        fireworksSource.Stop();
        fireworksSoundCoroutine = null;
    }

    // ========== Cheer Sound ==========

    private void PlayCheerSound()
    {
        if (cheerSource == null || cheerClip == null) return;

        if (cheerSoundCoroutine != null) StopCoroutine(cheerSoundCoroutine);
        cheerSoundCoroutine = StartCoroutine(CheerSoundRoutine());
    }

    private void StopCheerSound()
    {
        if (cheerSoundCoroutine != null)
        {
            StopCoroutine(cheerSoundCoroutine);
            cheerSoundCoroutine = null;
        }
        if (cheerSource != null)
        {
            cheerSource.Stop();
            cheerSource.volume = cheerVolume;
        }
    }

    private IEnumerator CheerSoundRoutine()
    {
        cheerSource.clip = cheerClip;
        cheerSource.volume = cheerVolume;
        cheerSource.time = 0f;
        cheerSource.Play();

        // cheerPlayDuration 동안 풀 볼륨 유지
        yield return new WaitForSeconds(cheerPlayDuration);

        // cheerFadeDuration 동안 페이드아웃
        float t = 0f;
        while (t < cheerFadeDuration)
        {
            t += Time.deltaTime;
            cheerSource.volume = Mathf.Lerp(cheerVolume, 0f, t / cheerFadeDuration);
            yield return null;
        }

        cheerSource.Stop();
        cheerSource.volume = cheerVolume;
        cheerSoundCoroutine = null;
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
            ClearParticles();
            Play();
        }
    }
#endif
}
