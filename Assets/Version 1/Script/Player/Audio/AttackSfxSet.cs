// ============================================================
// FILE: AttackSfxSet.cs
// PATH: Assets/Version 1/Script/Player/Audio/AttackSfxSet.cs
// ชุดเสียงโจมตี 1 ชุด (ScriptableObject) — สร้างผ่าน
// Create → MOMO → Audio → Attack SFX Set
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "SFX_Attack_", menuName = "MOMO/Audio/Attack SFX Set")]
public class AttackSfxSet : ScriptableObject
{
    [Header("เสียงเหวี่ยง/ปล่อยท่า (ท่า 1-2) — ใส่หลายไฟล์ได้ จะสุ่มเล่น")]
    public AudioClip[] swingClips;

    [Header("เสียงท่าจบ (ท่า 3) — เว้นว่าง = ใช้ swingClips")]
    public AudioClip[] finisherClips;

    [Header("เสียงตอนโดนเป้า — เว้นว่างได้")]
    public AudioClip[] hitClips;

    [Header("Mix")]
    [Range(0f, 1f)] public float volume = 0.85f;
    [Range(0f, 1f)] public float finisherVolume = 1f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [System.NonSerialized] private int _lastIndex = -1;

    public AudioClip GetSwing(bool finisher)
    {
        bool useFinisher = finisher && finisherClips != null && finisherClips.Length > 0;
        return Pick(useFinisher ? finisherClips : swingClips);
    }

    public AudioClip GetHit() => Pick(hitClips);

    public float RandomPitch() => Random.Range(pitchRange.x, pitchRange.y);

    // สุ่มแบบไม่ให้ไฟล์เดิมซ้ำติดกัน (ถ้ามีมากกว่า 1 ไฟล์)
    AudioClip Pick(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        if (pool.Length == 1) return pool[0];

        int i = Random.Range(0, pool.Length);
        if (i == _lastIndex) i = (i + 1) % pool.Length;
        _lastIndex = i;
        return pool[i];
    }
}