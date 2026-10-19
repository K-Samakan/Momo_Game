using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneBGM
    {
        public string sceneName;          // ต้องตรงกับชื่อ Scene เป๊ะ เช่น "Map1_intro"
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        public bool loop = true;          // Intro/Outro ที่เป็นคัตซีนอาจปิดไว้
    }

    [SerializeField] private SceneBGM[] playlist;
    [SerializeField] private float fadeTime = 1f;

    private static BGMManager instance;
    private AudioSource source;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var entry in playlist)
        {
            if (entry.sceneName == scene.name)
            {
                StopAllCoroutines();
                StartCoroutine(SwitchTo(entry));
                return;
            }
        }
        // ถ้าไม่มีชื่อ Scene ในลิสต์ จะเล่นเพลงเดิมต่อไป
    }

    private IEnumerator SwitchTo(SceneBGM entry)
    {
        // เพลงเดียวกันอยู่แล้ว: ปรับแค่ระดับเสียง ไม่เริ่มเพลงใหม่
        if (source.clip == entry.clip && source.isPlaying)
        {
            source.loop = entry.loop;
            yield return Fade(entry.volume);
            yield break;
        }

        if (source.isPlaying) yield return Fade(0f);

        source.clip = entry.clip;
        source.loop = entry.loop;
        if (entry.clip != null) source.Play();

        yield return Fade(entry.volume);
    }

    private IEnumerator Fade(float target)
    {
        float start = source.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        source.volume = target;
    }

    // เรียกจากที่อื่นได้ เช่น ตอน Pause หรือ Game Over
    public static void Stop() { if (instance) instance.StartCoroutine(instance.Fade(0f)); }
}