// ============================================================
// FILE: SaveData.cs
// PATH: Assets/Version 1/Script/SaveSystem/SaveData.cs
// ============================================================
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
    public string savePointID = "";
    public string sceneName = "";
    public float posX, posY, posZ;

    // ─── Quest Progress ─────────────────────────────────────
    public List<string> completedSideQuests = new List<string>();

    // ─── Monster Kills ──────────────────────────────────────
    public List<string> killedMonsterIDs = new List<string>();

    // ─── Timestamp ──────────────────────────────────────────
    public string savedAt = "";

    // ─── Item Flags ─────────────────────────────────────────
    public bool hasMonkeyGlove = false;
    public bool hasJetpack = false;
    public bool hasFishingRod = false;

    public Vector3 GetRespawnPosition() => new Vector3(posX, posY, posZ);

    public void SetRespawnPosition(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }
}