using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class WaveController : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public Transform enemyParent;
    public int waves = 3;
    public int enemiesPerWave = 5;
    public float timeBetweenWaves = 3f;

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

            // Wait for all enemies to be cleared
            while (activeEnemies > 0)
            {
                yield return null;
            }

            Debug.Log($"[WaveController] Wave {currentWave} cleared.");

            // Timer before next wave
            float timer = timeBetweenWaves;
            // if (waveTimerText != null) waveTimerText.gameObject.SetActive(true);
            while (timer > 0)
            {
                // if (waveTimerText != null)
                //     waveTimerText.text = $"Next wave in {Mathf.Ceil(timer)}s";
                timer -= Time.deltaTime;
                yield return null;
            }
            // if (waveTimerText != null) waveTimerText.gameObject.SetActive(false);
        }

        EndWave();
    }

    void SpawnWave()
    {
        Debug.Log($"[WaveController] Spawning {enemiesPerWave} enemies.");
        for (int i = 0; i < enemiesPerWave; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var enemy = Instantiate(prefab, point.position, Quaternion.identity, enemyParent);

            if (enemy.TryGetComponent(out EnemyHealth health))
            {
                activeEnemies++;
                health.depleteEvent += () =>
                {
                    activeEnemies--;
                    Debug.Log($"[WaveController] Enemy defeated. {activeEnemies} remaining.");
                };
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
}
