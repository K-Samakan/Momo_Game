using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // ─── Player State ───────────────────────────────────────
    public int playerCurrentHP = 5;
    public int playerMaxHP = 5;

    // ─── Save Point Location ────────────────────────────────
    public string savePointID = "";       // ID ของ Save Point ที่ Save ล่าสุด
    public string sceneName = "";       // Scene ที่อยู่
    public float posX, posY, posZ;            // ตำแหน่ง respawn

    // ─── Quest Progress ─────────────────────────────────────
    public List<string> completedSideQuests = new List<string>();   // ภารกิจรองที่ทำแล้ว

    // ─── Monster Kills (per scene) ──────────────────────────
    public List<string> killedMonsterIDs = new List<string>();   // Monster ที่ตายแล้ว

    // ─── Timestamp ──────────────────────────────────────────
    public string savedAt = "";   // เวลาที่ Save ล่าสุด

    public Vector3 GetRespawnPosition() => new Vector3(posX, posY, posZ);

    public void SetRespawnPosition(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }
}