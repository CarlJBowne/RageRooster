using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private LayerMask collisionLayer;

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
    private CoroutinePlus coroutine;

    private void Start()
    {
        enemyPool.onCreateInstance += (PoolableObject newEnemy) => 
            { newEnemy.GetComponent<EnemyHealth>().depleteEvent += () => 
                { activeEnemies--; }; };

        enemyPool.Initialize();

        if (!SaveOverride && worldChange != null && worldChange.Enabled) Destroy(gameObject);
        else if (trigger != null) UltEvents.UltEvent.AddDynamicCall(ref trigger.Event, StartArena);
    }

    public void StartArena()
    {
        if (isActive || (!SaveOverride && worldChange != null && worldChange.Enabled)) return;

        isActive = true;
        SetWalls(true);
        Gameplay.onPlayerRespawn += ResetArena;
        HandleWaves().Begin(this);
    }

    IEnumerator HandleWaves()
    {
        while (currentWave < waves)
        {
            SpawnWave();
            currentWave++;

            while (activeEnemies > 0) yield return null;

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

        //End Reached!
        SetWalls(false);
        if (worldChange != null) worldChange.Enable();
        Destroy(this);
    }

    void SpawnWave()
    {
        List<Vector3> spawnPoints = new();
        int attempts = 0;

        while(spawnPoints.Count < enemiesPerWave && attempts < 100)
        {
            Vector3 randomPosition = spawnAreaCenter + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            if (!Physics.Raycast(transform.position, randomPosition - transform.position, (randomPosition - transform.position).magnitude, collisionLayer, QueryTriggerInteraction.Ignore))
                spawnPoints.Add(randomPosition);

            attempts++;
        }


        for (int i = 0; i < spawnPoints.Count; i++)
        {
            PoolableObject pooledEnemy = enemyPool.Pump();
            activeEnemies++;

            pooledEnemy.SetPosition(spawnPoints[i]);
            pooledEnemy.SetRotation((Gameplay.Player.transform.position - spawnPoints[i]).DirToRot());
        }
    }

    private void Update() => enemyPool.Update();

    public void SetWalls(bool value)
    {for (int i = 0; i < wallsToDisable.Length; i++) wallsToDisable[i].SetActive(value);}

    public void ResetArena()
    {
        SetWalls(false);
        currentWave = 0;
        activeEnemies = 0;
        isActive = false;
        CoroutinePlus.Stop(ref coroutine);
        Gameplay.onPlayerRespawn -= ResetArena;
    }

    private void OnDestroy() => Gameplay.onPlayerRespawn -= ResetArena;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}