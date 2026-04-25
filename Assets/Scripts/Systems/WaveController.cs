using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using RageRooster.Systems.ObjectPooling;

public class WaveController : MonoBehaviour
{
    [Header("Wave Settings")]
    public ObjectPool<EnemyHealth> enemyPool;
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
    //public RageRooster.Obsolete.WorldChange worldChange;
    //public PlayerEnterTrigger3 trigger; 
    //You shouldn't NEED to reference PlayerEnterTrigger.

    private int currentWave = 0;
    private int activeEnemies = 0;
    private bool isActive = false;
    private Coroutine coroutine;

    private void Start()
    {
        enemyPool.onCreateInstance += (PoolableObject O) =>
            {
                O.GetComponent<EnemyHealth>().depleteEvent += () =>
                { activeEnemies--; };
            };
        //Note: This is a bad hack. Edit the Object Pooling System so that the Action callbacks pass in SOME kind of access to the Components as well.

        enemyPool.Initialize();

        CheckVariable();

        //if (!SaveOverride && worldChange != null && worldChange.Enabled) Destroy(gameObject);
        //else if (trigger != null) UltEvents.UltEvent.AddDynamicCall(ref trigger.Event, StartArena);
    }

    public void CheckVariable()
    {
        if (SaveOverride) return;

        if (true) Destroy(gameObject); //PLACEHOLDER
    }
    public void SetVariable()
    {
        //PLACEHOLDER
    }

    public void StartArena()
    {
        if (isActive) return;

        isActive = true;
        SetWalls(true);
        Player.onRespawn += ResetArena;
        HandleWaves().Begin(this);
    }

    IEnumerator HandleWaves()
    {
        while (currentWave < waves)
        {
            SpawnWave();
            currentWave++;

            while (activeEnemies > 0) yield return null;

            Debug.Log("ALL ENEMIES DEFEATED");
            float timer = timeBetweenWaves;
            if (waveTimerText != null) waveTimerText.gameObject.SetActive(true);

            Debug.Log("TIMER STARTED");

            yield return new WaitForSeconds(timeBetweenWaves);
            Debug.Log("TIMER EXPIRED");

            //if (waveTimerText != null)
            //waveTimerText.text = $"Next wave in {Mathf.Ceil(timer)}s";

            // if (waveTimerText != null) waveTimerText.gameObject.SetActive(false);
        }

        //End Reached!
        Debug.Log("WAVE HAS ENDED");
        SetWalls(false);
        SetVariable();
        Destroy(this);
    }

    void SpawnWave()
    {
        Debug.Log("Spawning next wave");
        List<Vector3> spawnPoints = new();
        int attempts = 0;

        while (spawnPoints.Count < enemiesPerWave && attempts < 100)
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
            enemyPool.Pump(out PoolableObject pooledEnemy, out _);
            activeEnemies++;

            pooledEnemy.SetPosition(spawnPoints[i]);
            pooledEnemy.SetRotation((Player.Transform.position - spawnPoints[i]).DirToRot());
        }
    }

    private void Update() => enemyPool.Update(Time.deltaTime);

    public void SetWalls(bool value)
    { for (int i = 0; i < wallsToDisable.Length; i++) wallsToDisable[i].SetActive(value); }

    public void ResetArena()
    {
        SetWalls(false);
        currentWave = 0;
        activeEnemies = 0;
        isActive = false;
        Coroutine.Stop(ref coroutine);
        Player.onRespawn -= ResetArena;
    }

    private void OnDestroy() => Player.onRespawn -= ResetArena;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}