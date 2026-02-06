using System;
using UnityEngine;

[System.Serializable]
public class TrialWaveDataList
{
    public ZombieCatalogEntry[] zombieCatalog;
    public TrialWaveData[] waves;
}

[System.Serializable]
public class ZombieCatalogEntry
{
    public string id;
    public string prefab;
    public int cost;
}

[System.Serializable]
public class TrialWaveData
{
    public int wave;
    public TrialWaveGroup[] groups;
}

[System.Serializable]
public class TrialWaveGroup
{
    public string groupId;
    public string[] spawnAfterGroupsCleared;

    public Vec2[] spawnPoints;

    public BudgetRule budget;
    public AllowedEnemy[] allowedEnemies;
    public SpawnRule spawnRule;
    public CompleteCondition completeCondition;

    public FixedSpawn[] fixedSpawns;
}

[System.Serializable]
public class BudgetRule
{
    public int startPoint;
    public float gainPerSecond;
    public int maxPoint;
    public float duration;
}

[System.Serializable]
public class AllowedEnemy
{
    public string zombieId;
    public int weight = 1;
    public int minWavePointToUse = 0;
}

[System.Serializable]
public class SpawnRule
{
    public float cooldown = 0.5f;
    public int maxAlive = 999;
    public int spawnCountLimit = 999;
}

[System.Serializable]
public class CompleteCondition
{
    public string type; // "BudgetSpentAndAllDead", "TimeUpAndAllDead", ...
}

[System.Serializable]
public class FixedSpawn
{
    public float time;
    public string zombieId;
    public Vec2 position;
}

[System.Serializable]
public struct Vec2
{
    public float x;
    public float y;
}
