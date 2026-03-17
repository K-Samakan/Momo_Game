using System.Collections;
using UnityEngine;

namespace LocalizationPackageExtensions
{
    internal class AutoAudioUnloader : MonoBehaviour
    {
        internal static AutoAudioUnloader Instance;
        static AutoAudioUnloader _instance;

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            var go = new GameObject("[Localization Package Extensions] Auto Audio Unloader")
                .AddComponent<AutoAudioUnloader>().gameObject;
            go.hideFlags = HideFlags.HideAndDontSave;
        }
        void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        internal static void Unload(AudioClip clip)
        {
            if(!_instance) return;
            _instance.StartCoroutine(_instance.WaitAndUnload(clip));
        }

        IEnumerator WaitAndUnload(AudioClip clip)
        {
            yield return new WaitForSeconds(clip.length);
            clip.UnloadAudioData();
        }
    }
}