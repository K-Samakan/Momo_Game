// ============================================================
// FILE: IAttackSfxProvider.cs
// PATH: Assets/Version 1/Script/Player/Audio/IAttackSfxProvider.cs
// ============================================================

/// <summary>
/// ไอเทมที่ "มีเสียงโจมตีของตัวเอง" ให้ implement interface นี้
/// (GloveController / FireBowController / FishingRodController)
/// ไอเทมที่ไม่มีการโจมตี (เช่น โล่ Map2) ไม่ต้อง implement → ใช้เสียงต่อย/เตะเดิมอัตโนมัติ
/// </summary>
public interface IAttackSfxProvider
{
    bool IsEquipped { get; }
    AttackSfxSet AttackSfx { get; }
    int SfxPriority { get; }   // ค่ามากชนะ: Bow 30 > Rod 20 > Glove 10
}