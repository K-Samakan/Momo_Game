using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// Sound Data Class
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        public bool loop = false;
        
        [HideInInspector]
        public AudioSource source;
    }
    
    /// <summary>
    /// Audio Manager - จัดการเสียงทั้งหมด
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Background Music")]
        [SerializeField] private List<Sound> bgmTracks = new List<Sound>();
        
        [Header("Sound Effects")]
        [SerializeField] private List<Sound> sfxSounds = new List<Sound>();
        
        [Header("Volume Settings")]
        [SerializeField][Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.7f;
        [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;
        
        [Header("Fade Settings")]
        [SerializeField] private float bgmFadeDuration = 1f;
        
        [Header("Settings")]
        [SerializeField] private int maxSimultaneousSFX = 5;
        
        private static AudioManager instance;
        private AudioSource bgmSource;
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private string currentBGM;
        
        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudioSources();
        }
        
        /// <summary>
        /// สร้าง AudioSource Pool
        /// </summary>
        private void InitializeAudioSources()
        {
            // BGM Source
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.playOnAwake = false;
            
            // SFX Pool
            for (int i = 0; i < maxSimultaneousSFX; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                source.volume = sfxVolume * masterVolume;
                sfxPool.Add(source);
            }
            
            Debug.Log($"[AudioManager] Initialized with {maxSimultaneousSFX} SFX channels");
        }
        
        #region BGM Methods
        
        /// <summary>
        /// เล่น BGM
        /// </summary>
        public void PlayBGM(string name, bool fade = false)
        {
            Sound sound = bgmTracks.Find(s => s.name == name);
            
            if (sound == null)
            {
                Debug.LogWarning($"[AudioManager] BGM '{name}' not found!");
                return;
            }
            
            if (currentBGM == name && bgmSource.isPlaying)
            {
                return; // Already playing
            }
            
            currentBGM = name;
            
            if (fade)
            {
                StartCoroutine(FadeBGM(sound));
            }
            else
            {
                PlayBGMImmediate(sound);
            }
        }
        
        private void PlayBGMImmediate(Sound sound)
        {
            bgmSource.clip = sound.clip;
            bgmSource.volume = sound.volume * bgmVolume * masterVolume;
            bgmSource.pitch = sound.pitch;
            bgmSource.loop = sound.loop;
            bgmSource.Play();
            
            Debug.Log($"[AudioManager] Playing BGM: {sound.name}");
        }
        
        private System.Collections.IEnumerator FadeBGM(Sound newSound)
        {
            // Fade out
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            
            while (elapsed < bgmFadeDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (bgmFadeDuration / 2));
                yield return null;
            }
            
            // Switch track
            PlayBGMImmediate(newSound);
            bgmSource.volume = 0f;
            
            // Fade in
            elapsed = 0f;
            float targetVolume = newSound.volume * bgmVolume * masterVolume;
            
            while (elapsed < bgmFadeDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (bgmFadeDuration / 2));
                yield return null;
            }
            
            bgmSource.volume = targetVolume;
        }
        
        public void StopBGM(bool fade = false)
        {
            if (fade)
            {
                StartCoroutine(StopBGMWithFade());
            }
            else
            {
                bgmSource.Stop();
            }
        }
        
        private System.Collections.IEnumerator StopBGMWithFade()
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            
            while (elapsed < bgmFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / bgmFadeDuration);
                yield return null;
            }
            
            bgmSource.Stop();
            bgmSource.volume = startVolume;
        }
        
        public void PauseBGM()
        {
            bgmSource.Pause();
        }
        
        public void ResumeBGM()
        {
            bgmSource.UnPause();
        }
        
        #endregion
        
        #region SFX Methods
        
        /// <summary>
        /// เล่น SFX
        /// </summary>
        public void PlaySFX(string name)
        {
            Sound sound = sfxSounds.Find(s => s.name == name);
            
            if (sound == null)
            {
                Debug.LogWarning($"[AudioManager] SFX '{name}' not found!");
                return;
            }
            
            AudioSource source = GetAvailableSFXSource();
            
            if (source != null)
            {
                source.clip = sound.clip;
                source.volume = sound.volume * sfxVolume * masterVolume;
                source.pitch = sound.pitch;
                source.loop = sound.loop;
                source.Play();
            }
        }
        
        /// <summary>
        /// เล่น SFX ที่ตำแหน่งใน World Space
        /// </summary>
        public void PlaySFXAtPosition(string name, Vector3 position)
        {
            Sound sound = sfxSounds.Find(s => s.name == name);
            
            if (sound == null)
            {
                Debug.LogWarning($"[AudioManager] SFX '{name}' not found!");
                return;
            }
            
            AudioSource.PlayClipAtPoint(
                sound.clip, 
                position, 
                sound.volume * sfxVolume * masterVolume
            );
        }
        
        private AudioSource GetAvailableSFXSource()
        {
            AudioSource available = sfxPool.FirstOrDefault(s => !s.isPlaying);
            
            if (available == null)
            {
                available = sfxPool[0];
            }
            
            return available;
        }
        
        public void StopAllSFX()
        {
            foreach (var source in sfxPool)
            {
                source.Stop();
            }
        }
        
        #endregion
        
        #region Volume Control
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = bgmVolume * masterVolume;
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            
            foreach (var source in sfxPool)
            {
                source.volume = sfxVolume * masterVolume;
            }
        }
        
        private void UpdateAllVolumes()
        {
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
        }
        
        public float GetMasterVolume() => masterVolume;
        public float GetBGMVolume() => bgmVolume;
        public float GetSFXVolume() => sfxVolume;
        
        #endregion
        
        #region Static Methods
        
        public static void PlaySFXStatic(string name)
        {
            instance?.PlaySFX(name);
        }
        
        public static void PlayBGMStatic(string name)
        {
            instance?.PlayBGM(name);
        }
        
        #endregion
    }
}