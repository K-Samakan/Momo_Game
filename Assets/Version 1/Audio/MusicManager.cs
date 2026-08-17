using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources (ใช้ 2 ตัวสลับกัน Fade)")]
    public AudioSource sourceA;
    public AudioSource sourceB;
    private AudioSource _activeSource;
    private AudioSource _inactiveSource;

    [Header("Music Tracks")]
    public AudioClip explorationMusic;   // เพลงตอนเดินสำรวจในด่านปกติ
    public AudioClip bossMusic;          // เพลงตอนสู้บอส

    [Header("Cutscene Music")]
    public AudioClip introMusic;         // เพลงตอน Cutscene Intro
    public AudioClip outroMusic;         // เพลงตอน Cutscene Outro (บอสตาย)

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;

    private Coroutine _fadeCoroutine;
    private AudioClip _currentClip;

    void Awake()
    {
        // ✅ Singleton pattern — กันซ้อนตอนโหลดฉากใหม่
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // สร้าง AudioSource อัตโนมัติถ้ายังไม่ได้ผูก
        if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();
        if (sourceB == null) sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceB.loop = true;
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;
        sourceA.volume = 0f;
        sourceB.volume = 0f;

        _activeSource = sourceA;
        _inactiveSource = sourceB;
    }

    void Start()
    {
        // เล่นเพลงสำรวจตั้งแต่เริ่มฉาก (ถ้ามี)
        if (explorationMusic != null)
            PlayMusic(explorationMusic);
    }

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>เรียกจาก BossRoomTrigger ตอนผู้เล่นเข้าห้องบอส</summary>
    public void PlayBossMusic()
    {
        PlayMusic(bossMusic);
    }

    /// <summary>เรียกกลับเพลงสำรวจปกติ (เช่น ผู้เล่นออกจากห้องบอสก่อนสู้)</summary>
    public void PlayExplorationMusic()
    {
        PlayMusic(explorationMusic);
    }

    /// <summary>เรียกตอนเริ่ม Cutscene Intro — เล่นครั้งเดียวไม่วนซ้ำ</summary>
    public void PlayIntroMusic()
    {
        PlayMusic(introMusic, loop: false);
    }

    /// <summary>เรียกตอนบอสตาย/เข้า Cutscene Outro — เล่นครั้งเดียวไม่วนซ้ำ</summary>
    public void PlayOutroMusic()
    {
        PlayMusic(outroMusic, loop: false);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || clip == _currentClip) return;   // กันเล่นเพลงเดิมซ้ำ

        _currentClip = clip;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(CrossFade(clip, loop));
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Fade Logic
    // ════════════════════════════════════════════════════════════

    IEnumerator CrossFade(AudioClip newClip, bool loop)
    {
        // เตรียมเพลงใหม่บน source ที่ไม่ได้ใช้งาน
        _inactiveSource.clip = newClip;
        _inactiveSource.volume = 0f;
        _inactiveSource.loop = loop;
        _inactiveSource.Play();

        float t = 0f;
        float startVolActive = _activeSource.volume;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / fadeDuration;

            _activeSource.volume = Mathf.Lerp(startVolActive, 0f, progress);
            _inactiveSource.volume = Mathf.Lerp(0f, musicVolume, progress);

            yield return null;
        }

        _activeSource.volume = 0f;
        _activeSource.Stop();
        _inactiveSource.volume = musicVolume;

        // สลับตัวแปร active/inactive
        var temp = _activeSource;
        _activeSource = _inactiveSource;
        _inactiveSource = temp;
    }

    #endregion
}