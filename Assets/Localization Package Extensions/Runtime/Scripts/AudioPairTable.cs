using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace LocalizationPackageExtensions
{
    [Serializable]
    public class AudioStringPair
    {
        public string key;
        public long keyId;
        public LocaleIdentifier localeId;
        public AudioClip audioClip;

        public AudioStringPair(string key, long keyId, LocaleIdentifier localeId, AudioClip clip)
        {
            this.key = key;
            this.keyId = keyId;
            this.localeId = localeId;
            this.audioClip = clip;
        }
    }

    [CreateAssetMenu(fileName = "New Audio Pair Table", menuName = "Localization/Extensions/Audio Pair Table", order = 0)]
    public class AudioPairTable : ScriptableObject
    {
        public string tableCollectionName;

        public List<AudioStringPair> pairMap;
        Dictionary<string, Dictionary<LocaleIdentifier, AudioClip>> _categorizedKeyMap;
        Dictionary<long, Dictionary<LocaleIdentifier, AudioClip>> _categorizedIDMap;
        bool _initialized;
#if UNITY_EDITOR
        long _editorInitializedTime;
#endif

        void Init()
        {
            if(_initialized) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if(_editorInitializedTime == DateTime.Now.Ticks) return;
                _editorInitializedTime = DateTime.Now.Ticks;
            }
#endif
            _categorizedKeyMap = new Dictionary<string, Dictionary<LocaleIdentifier, AudioClip>>();
            if (pairMap == null) pairMap = new List<AudioStringPair>();
            foreach (var pair in pairMap)
            {
                if (!_categorizedKeyMap.ContainsKey(pair.key)) _categorizedKeyMap[pair.key] = new Dictionary<LocaleIdentifier, AudioClip>();
                _categorizedKeyMap[pair.key][pair.localeId] = pair.audioClip;
            }

            
            _categorizedIDMap = new Dictionary<long, Dictionary<LocaleIdentifier, AudioClip>>();
            foreach (var pair in pairMap)
            {
                if (!_categorizedIDMap.ContainsKey(pair.keyId)) _categorizedIDMap[pair.keyId] = new Dictionary<LocaleIdentifier, AudioClip>();
                _categorizedIDMap[pair.keyId][pair.localeId] = pair.audioClip;
            }

#if UNITY_EDITOR
            if(!Application.isPlaying) return;
#endif
            _initialized = true;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += reset_init;
            void reset_init(PlayModeStateChange change)
            {
                if (change == PlayModeStateChange.ExitingPlayMode) _initialized = false;
            }
#endif
        }

        /// <summary>
        /// Adds a given AudioStringPair to the PairMap.
        /// If a pair with the same key value or keyId value as the given Pair already exists, it will be deleted.
        /// </summary>
        public void AddPair(AudioStringPair pair)
        {
            RemovePair(pair.key, pair.localeId);
            pairMap.Add(pair);
        }

        /// <summary>
        /// Removes all pairs with given key or id. It affects to all locales.
        /// </summary>
        public void RemovePair(TableEntryReference entryReference)
        {
            if(entryReference.ReferenceType == TableEntryReference.Type.Name)
                pairMap.RemoveAll(x => x.key == entryReference);
            else if(entryReference.ReferenceType == TableEntryReference.Type.Id)
                pairMap.RemoveAll(x => x.keyId == entryReference);
        }

        /// <summary>
        /// Removes all pairs with given key or id. It only affects to a given locale.
        /// </summary>
        public void RemovePair(TableEntryReference entryReference, LocaleIdentifier localeID)
        {
            if(entryReference.ReferenceType == TableEntryReference.Type.Name)
                pairMap.RemoveAll(x => x.key == entryReference && x.localeId == localeID);
            else if(entryReference.ReferenceType == TableEntryReference.Type.Id)
                pairMap.RemoveAll(x => x.keyId == entryReference && x.localeId == localeID);
        }

        public bool HasClip(TableEntryReference entryReference, LocaleIdentifier localeId)
        {
            Init();
            if (entryReference.ReferenceType == TableEntryReference.Type.Name)
            {
                if (_categorizedKeyMap.TryGetValue(entryReference, out var pair))
                    return pair.ContainsKey(localeId);
            }

            if (entryReference.ReferenceType == TableEntryReference.Type.Id)
            {
                if (_categorizedIDMap.TryGetValue(entryReference, out var pair))
                    return pair.ContainsKey(localeId);
            }

            return false;
        }
        
        /// <summary>
        /// Get AudioClip corresponding the given entryReference and current Locale.
        /// You can use both string Key value and long KeyId value for entryReference. 
        /// </summary>
        public AudioClip GetAudioClip(TableEntryReference entryReference)
        {
            return GetAudioClip(entryReference, LocalizationSettings.SelectedLocale.Identifier);
        }
        
        /// <summary>
        /// Get AudioClip corresponding the given entryReference and Locale.
        /// You can use both string Key value and long KeyId value for entryReference. 
        /// </summary>
        public AudioClip GetAudioClip(TableEntryReference entryReference, LocaleIdentifier localeId)
        {
            Init();

            switch (entryReference.ReferenceType)
            {
                case TableEntryReference.Type.Empty:
                    break;
                case TableEntryReference.Type.Name:
                {
                    if (_categorizedKeyMap.TryGetValue(entryReference, out var map))
                    {
                        if (map.TryGetValue(localeId, out var clip)) return clip;
                    }

                    break;
                }
                case TableEntryReference.Type.Id:
                {
                    if (_categorizedIDMap.TryGetValue(entryReference, out var map))
                    {
                        if (map.TryGetValue(localeId, out var clip)) return clip;
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            

            return null;
        }
        

        /// <summary>
        /// Finds an audio clip corresponding to the given entryReference and current Locale, loads it, and plays it.
        /// Then unloads the audio data again to optimize memory.
        /// If you want to use this feature, keep Preload Audio Data set to false in the import settings of the audio clip.
        /// 
        /// You can use both string Key value and long KeyId value for entryReference. 
        /// </summary>
        public void PlayAndUnload(TableEntryReference entryReference, AudioSource source)
        {
            PlayAndUnload(entryReference, LocalizationSettings.SelectedLocale.Identifier, source);
        }
        /// <summary>
        /// Finds an audio clip corresponding to the given entryReference and Locale, loads it, and plays it.
        /// Then unloads the audio data again to optimize memory.
        /// If you want to use this feature, keep Preload Audio Data set to false in the import settings of the audio clip.
        /// 
        /// You can use both string Key value and long KeyId value for entryReference. 
        /// </summary>
        public void PlayAndUnload(TableEntryReference entryReference, LocaleIdentifier localeIdentifier, AudioSource source)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"Cannot play sound in edit mode.");
                return;
            }
#endif
            var clip = GetAudioClip(entryReference, localeIdentifier);
            if (clip == null)
            {
                Debug.LogWarning($"Cannot find a pair with given key: {entryReference}");
                return;
            }
            if(clip.loadState == AudioDataLoadState.Loaded)
            {
                source.PlayOneShot(clip);
            }
            else
            {
                if(clip.LoadAudioData()) source.PlayOneShot(clip);
            }

            AutoAudioUnloader.Unload(clip);
        }
    }
}