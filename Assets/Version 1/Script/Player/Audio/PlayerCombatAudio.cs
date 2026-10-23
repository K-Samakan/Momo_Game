// ============================================================
// FILE: PlayerCombatAudio.cs
// PATH: Assets/Version 1/Script/Player/Audio/PlayerCombatAudio.cs
// ติดที่ GameObject เดียวกับ SmartAttackSystem (Player root)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public enum AttackKind { Punch, Kick }

[DisallowMultipleComponent]
public class PlayerCombatAudio : MonoBehaviour
{
    [Header("Default SFX — ใช้ตอนไม่มีไอเทม หรือไอเทมไม่มีเสียงโจมตี (Map2)")]
    public AttackSfxSet punchSfx;
    public AttackSfxSet kickSfx;

    [Header("Audio Source (เว้นว่าง = สร้างให้อัตโนมัติ)")]
    public AudioSource source;
    [Range(0f, 1f)] public float spatialBlend = 0.5f;

    [Header("VSE / CC (Optional) — ต้องเพิ่ม Tag นี้ใน VSE Manager ก่อน")]
    public bool emitVSE = false;
    public SoundEmitter attackEmitter;

    [Header("Debug")]
    public bool showDebugLog = true;

    private readonly List<IAttackSfxProvider> _providers = new List<IAttackSfxProvider>();

    void Awake()
    {
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 25f;
    }

    void Start() => RefreshProviders();

    /// <summary>เรียกซ้ำได้ถ้ามีไอเทมใหม่ถูก spawn ทีหลัง</summary>
    public void RefreshProviders()
    {
        _providers.Clear();
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in all)
            if (mb is IAttackSfxProvider p) _providers.Add(p);

        if (showDebugLog) Debug.Log($"[CombatAudio] พบไอเทมที่มีเสียงโจมตี {_providers.Count} ชิ้น");
    }

    /// <summary>ไอเทมที่สวมอยู่และมี priority สูงสุด (null = ใช้เสียงเดิม)</summary>
    public IAttackSfxProvider GetActiveOverride()
    {
        IAttackSfxProvider best = null;
        foreach (var p in _providers)
        {
            if (p == null || (p as Object) == null) continue;   // กันของที่ถูก Destroy
            if (!p.IsEquipped || p.AttackSfx == null) continue;
            if (best == null || p.SfxPriority > best.SfxPriority) best = p;
        }
        return best;
    }

    AttackSfxSet Resolve(AttackKind kind)
    {
        var item = GetActiveOverride();
        if (item != null) return item.AttackSfx;              // ✅ เสียงไอเทม → เสียงต่อย/เตะไม่ถูกเล่น
        return kind == AttackKind.Kick ? kickSfx : punchSfx;  // ✅ ไม่มีไอเทม/Map2 → เสียงเดิม
    }

    // ───────── Public API ─────────

    public void PlaySwing(AttackKind kind, bool finisher)
    {
        var set = Resolve(kind);
        if (set == null) return;

        Play(set.GetSwing(finisher), finisher ? set.finisherVolume : set.volume, set.RandomPitch());
        if (emitVSE) attackEmitter?.Emit();

        if (showDebugLog) Debug.Log($"[CombatAudio] 🔊 Swing {kind} finisher={finisher} → {set.name}");
    }

    public void PlayHit(AttackKind kind)
    {
        var set = Resolve(kind);
        if (set == null) return;

        Play(set.GetHit(), set.hitVolume, set.RandomPitch());

        if (showDebugLog) Debug.Log($"[CombatAudio] 💥 Hit {kind} → {set.name}");
    }

    /// <summary>ไอเทมที่ไม่ได้โจมตีผ่าน SmartAttackSystem (ธนู) เรียกตรงนี้</summary>
    public void PlayFromSet(AttackSfxSet set, bool finisher)
    {
        if (set == null) return;

        Play(set.GetSwing(finisher), finisher ? set.finisherVolume : set.volume, set.RandomPitch());
        if (emitVSE) attackEmitter?.Emit();

        if (showDebugLog) Debug.Log($"[CombatAudio] 🏹 {set.name} finisher={finisher}");
    }

    public void PlayClip(AudioClip clip, float volume = 1f) => Play(clip, volume, 1f);

    void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || source == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }
}