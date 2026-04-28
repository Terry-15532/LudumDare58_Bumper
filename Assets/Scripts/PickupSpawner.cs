using System.Collections.Generic;
using UnityEngine;

// Spawner — periodically drops a random pickup near the losing player.
// Attach to any GameObject in the scene and assign the three prefabs in the inspector.
public class PickupSpawner : MonoBehaviour {
    public GameObject speedBoostPrefab;
    public GameObject doubleScorePrefab;
    public GameObject invincibilityPrefab;

    public float spawnInterval = 8f;
    public float minRadius = 3f;
    public float maxRadius = 5f;
    public float spawnY = 2.5f;
    public int maxAlive = 2;

    // Map bounds matching coin spawn area in Coin.cs.
    public float boundsX = 9.5f;
    public float boundsZ = 5.5f;

    static readonly HashSet<PickupBase> alive = new HashSet<PickupBase>();

    float timer;

    public static void NotifyConsumed(PickupBase pickup){
        alive.Remove(pickup);
    }

    void OnDisable(){
        alive.Clear();
        timer = 0f;
    }

    void Update(){
        if (Game.instance == null || !Game.instance.matchRunning) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;
        if (alive.Count >= maxAlive) return;
        SpawnOne();
    }

    void SpawnOne(){
        var prefab = PickRandomPrefab();
        if (prefab == null) return;

        var loser = (Game.instance.scores.x <= Game.instance.scores.y)
            ? Game.instance.playerBlue
            : Game.instance.playerRed;

        Vector3 origin = loser.transform.position;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);
        float x = Mathf.Clamp(origin.x + Mathf.Cos(angle) * radius, -boundsX, boundsX);
        float z = Mathf.Clamp(origin.z + Mathf.Sin(angle) * radius, -boundsZ, boundsZ);
        Vector3 pos = new Vector3(x, spawnY, z);

        var go = Instantiate(prefab, pos, Quaternion.identity);
        var pickup = go.GetComponent<PickupBase>();
        if (pickup != null) alive.Add(pickup);
    }

    GameObject PickRandomPrefab(){
        var pool = new List<GameObject>(3);
        if (speedBoostPrefab) pool.Add(speedBoostPrefab);
        if (doubleScorePrefab) pool.Add(doubleScorePrefab);
        if (invincibilityPrefab) pool.Add(invincibilityPrefab);
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }
}
