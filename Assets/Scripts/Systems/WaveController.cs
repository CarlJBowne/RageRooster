using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class WaveController : MonoBehaviour
{
    [Header("Wave Settings")]
    public ObjectPool enemyPool;
    public int waves = 3;
    public int enemiesPerWave = 5;
    public float timeBetweenWaves = 3f;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaCenter;
    [SerializeField] private Vector3 spawnAreaSize;

    [Header("UI")]
    public TextMeshProUGUI waveTimerText;

    [Header("Override Settings")]
    public bool SaveOverride = false;

    [Header("World & Trigger")]
    public GameObject[] wallsToDisable;
    public WorldChange worldChange;
    public PlayerEnterTrigger3 trigger;

    private int currentWave = 0;
    private int activeEnemies = 0;
    private bool isActive = false;

    private void Start()
    {
        enemyPool.Initialize();

        if (!SaveOverride && worldChange != null && worldChange.Enabled)
        {
            Destroy(gameObject);
        }
        else if (trigger != null)
        {
            UltEvents.UltEvent.AddDynamicCall(ref trigger.Event, StartWave);
        }
    }

    public void StartWave()
    {
        if (isActive || (!SaveOverride && worldChange != null && worldChange.Enabled))
        {
            Debug.Log("[WaveController] StartWave called, but controller is already active or world change is enabled.");
            return;
        }

        Debug.Log("[WaveController] Starting wave sequence.");
        isActive = true;
        foreach (var wall in wallsToDisable) wall.SetActive(true);
        StartCoroutine(HandleWaves());
    }

    IEnumerator HandleWaves()
    {
        while (currentWave < waves)
        {
            Debug.Log($"[WaveController] Starting wave {currentWave + 1}");
            SpawnWave();
            currentWave++;

            while (activeEnemies > 0)
            {
                yield return null;
            }

            Debug.Log($"[WaveController] Wave {currentWave} cleared.");

            float timer = timeBetweenWaves;
            if (waveTimerText != null) waveTimerText.gameObject.SetActive(true);
            while (timer > 0)
            {
                if (waveTimerText != null)
                    waveTimerText.text = $"Next wave in {Mathf.Ceil(timer)}s";
                timer -= Time.deltaTime;
                yield return null;
            }
            if (waveTimerText != null) waveTimerText.gameObject.SetActive(false);
        }

        EndWave();
    }

    void SpawnWave()
    {
        Debug.Log($"[WaveController] Spawning {enemiesPerWave} enemies within area.");
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 randomPosition = spawnAreaCenter + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            PoolableObject pooledEnemy = enemyPool.Pump();

            if (pooledEnemy != null)
            {
                pooledEnemy.SetPosition(randomPosition);
                pooledEnemy.SetRotation(Vector3.zero);

                if (pooledEnemy.TryGetComponent(out EnemyHealth health))
                {
                    activeEnemies++;
                    health.depleteEvent += () =>
                    {
                        activeEnemies--;
                        pooledEnemy.Disable();
                        Debug.Log($"[WaveController] Enemy defeated. {activeEnemies} remaining.");
                    };
                }
                else
                {
                    Debug.LogError("[WaveController] Spawned enemy lacks EnemyHealth component!");
                }
            }
            else
            {
                Debug.LogError("[WaveController] Enemy pool exhausted or unable to spawn enemy!");
            }
        }
    }

    void EndWave()
    {
        Debug.Log("[WaveController] All waves completed. Ending wave sequence.");

        foreach (var wall in wallsToDisable)
        {
            Destroy(wall);
        }

        if (worldChange != null)
        {
            Debug.Log("[WaveController] Triggering world change.");
            worldChange.Enable();
        }

        Destroy(this);
    }

    private void Update()
    {
        enemyPool.Update();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}