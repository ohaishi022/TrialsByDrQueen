using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnData
{
    public string unitPrefab;
    public List<Vector2> spawnPosition;
    public float spawnTime = 0f;
}

[System.Serializable]
public class WaveGroup
{
    public string groupId;
    public string[] spawnAfterGroupsCleared;
    public EnemySpawnData[] enemies;
}
[System.Serializable]
public class WaveData
{
    public int wave;
    public WaveGroup[] groups;
}
[System.Serializable]
public class EndlessWaveDataList
{
    public List<WaveData> waves;
}