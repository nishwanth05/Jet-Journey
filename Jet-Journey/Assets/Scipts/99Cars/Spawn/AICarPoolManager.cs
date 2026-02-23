using UnityEngine;
using System.Collections.Generic;

public class AICarPoolManager : MonoBehaviour
{
    /* ================= POOL ================= */
    [Header("Pool")]
    public GameObject aiCarPrefab;
    public int poolSize = 99;

    /* ================= SPAWN AREA ================= */
    [Header("Spawn Area (relative to player)")]
    public float forwardMin = 20f;
    public float forwardMax = 220f;
    public float sideRange = 8f;

    /* ================= SPAWN SAFETY ================= */
    [Header("Spawn Safety")]
    public float minSpawnDistance = 3.5f;
    public int maxSpawnAttempts = 25;

    /* ================= MICRO OFFSET ================= */
    [Header("Micro Offset (visual breakup only)")]
    public float lateralOffsetMin = -0.15f;
    public float lateralOffsetMax = 0.15f;
    public float forwardOffsetMin = -0.3f;
    public float forwardOffsetMax = 0.3f;

    /* ================= SPAWN PUSH ================= */
    [Header("Spawn Push (0.5 cube separation)")]
    public Vector3 separationBoxHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    public float pushStrength = 1.2f;
    public LayerMask carLayer;

    Transform player;

    readonly List<GameObject> pool = new List<GameObject>();
    readonly List<Vector3> usedSpawnPositions = new List<Vector3>();

    /* ================= INITIALIZATION ================= */

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!player)
        {
            Debug.LogError("AICarPoolManager: Player not found!");
            enabled = false;
            return;
        }

        CreatePool();
        SpawnAllCars();
    }

    void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject car = Instantiate(aiCarPrefab);
            car.SetActive(false);
            pool.Add(car);
        }
    }

    /* ================= SPAWNING ================= */

    void SpawnAllCars()
    {
        usedSpawnPositions.Clear();

        foreach (var car in pool)
        {
            SpawnCar(car);
        }
    }

    void SpawnCar(GameObject car)
    {
        Vector3 spawnPos;
        bool found = false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            spawnPos = GetRandomSpawnPosition();

            if (!IsPositionValid(spawnPos))
                continue;

            // 🔹 MICRO OFFSETS (visual only)
            spawnPos += player.right * Random.Range(lateralOffsetMin, lateralOffsetMax);
            spawnPos += player.forward * Random.Range(forwardOffsetMin, forwardOffsetMax);

            car.transform.position = spawnPos;
            car.transform.rotation = Quaternion.LookRotation(player.forward);

            usedSpawnPositions.Add(spawnPos);
            car.SetActive(true);

            // 🔹 PUSH OTHER CARS AWAY IF TOO CLOSE
            ResolveSpawnOverlap(car.transform);

            found = true;
            break;
        }

        // Fallback (very rare)
        if (!found)
        {
            Vector3 fallbackPos =
                player.position +
                player.forward * forwardMax +
                player.right * Random.Range(-sideRange, sideRange);

            car.transform.position = fallbackPos;
            car.transform.rotation = Quaternion.LookRotation(player.forward);
            car.SetActive(true);
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        float forward = Random.Range(forwardMin, forwardMax);
        float side = Random.Range(-sideRange, sideRange);

        return player.position +
               player.forward * forward +
               player.right * side;
    }

    bool IsPositionValid(Vector3 pos)
    {
        for (int i = 0; i < usedSpawnPositions.Count; i++)
        {
            if (Vector3.Distance(pos, usedSpawnPositions[i]) < minSpawnDistance)
                return false;
        }
        return true;
    }

    /* ================= SPAWN PUSH LOGIC ================= */

    void ResolveSpawnOverlap(Transform spawnedCar)
    {
        Collider[] hits = Physics.OverlapBox(
            spawnedCar.position,
            separationBoxHalfExtents,
            spawnedCar.rotation,
            carLayer
        );

        foreach (Collider hit in hits)
        {
            if (hit.transform == spawnedCar)
                continue;

            // Direction from spawned car to the other car
            Vector3 dir = hit.transform.position - spawnedCar.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.01f)
                dir = spawnedCar.right;

            hit.transform.position += dir.normalized * pushStrength;
        }
    }

    /* ================= GIZMOS ================= */

    void OnDrawGizmosSelected()
    {
        Transform p = player;
        if (!p)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go) p = go.transform;
        }
        if (!p) return;

        // Spawn area
        Vector3 center =
            p.position + p.forward * ((forwardMin + forwardMax) * 0.5f);

        Vector3 size = new Vector3(
            sideRange * 2f,
            1f,
            forwardMax - forwardMin
        );

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.matrix = Matrix4x4.TRS(
            center,
            Quaternion.LookRotation(p.forward),
            Vector3.one
        );
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, size);

        // Separation box preview
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireCube(p.position, separationBoxHalfExtents * 2f);
    }
}