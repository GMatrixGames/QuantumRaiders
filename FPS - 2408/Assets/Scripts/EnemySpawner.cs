using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class EnemySpawner : MonoBehaviour, IDamage
{
    [SerializeField] private int spawnerHP;
    [SerializeField] private Renderer model;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private GameObject enemyToSpawn;
    [SerializeField] private int maxEnemiesToSpawn;
    [SerializeField] private int timeBetweenSpawns;
    [SerializeField] private int distanceToSpawn;
    [SerializeField] private Image hpBar;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownDuration = 10f; // Duration to destroy the spawner

    private float countdownTimer;
    private bool timerActive;
    private bool isFlashing;

    private int hp;
    private int enemiesOnField;
    private bool hasSpawnedRecently;
    private bool isInsideRadius;
    private List<GameObject> spawnedEnemies = new();

    private Color colorOriginal;
    private Color countdownColorOriginal;

    private void Start()
    {
        hp = spawnerHP;
        hpBar.fillAmount = 1;
        colorOriginal = model.material.color;
        countdownColorOriginal = countdownText.color;
        GameManager.instance.UpdateSpawnersMax(1);
        countdownTimer = countdownDuration;

        // Ensure countdownText is disabled initially
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(false); 
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (isInsideRadius && hasSpawnedRecently == false && enemiesOnField < maxEnemiesToSpawn)
        {
            StartCoroutine(SpawnEnemies());
        }
        // Handle the countdown timer
        if (timerActive)
        {
            countdownTimer -= Time.deltaTime;
            countdownText.text = "Destroy in: " + Mathf.CeilToInt(countdownTimer);
            // Check if the timer is below or equal to 3 seconds
            if (countdownTimer <= 3f && !isFlashing)
            {
                StartCoroutine(FlashCountdownText());
            }

            if (countdownTimer <= 0)
            {
                OnCountdownEnd();
            }
        }
    }

    private IEnumerator FlashCountdownText()
    {
        isFlashing = true;
        while (countdownTimer is > 0 and <= 3f)
        {
            // Toggle the text color between red and the original color
            countdownText.color = countdownText.color == Color.red ? countdownColorOriginal : Color.red;

            yield return new WaitForSeconds(0.5f); 
        }

        // Restore original color when done
        countdownText.color = countdownColorOriginal;
        isFlashing = false;
    }

    private void LateUpdate()
    {
        hpBar.transform.forward = Camera.main.transform.forward;
    }

    private IEnumerator SpawnEnemies()
    {
        hasSpawnedRecently = true;
        yield return new WaitForSeconds(timeBetweenSpawns);

        if (enemiesOnField < maxEnemiesToSpawn)
        {
            var randomPos = spawnPosition.position + Random.insideUnitSphere * distanceToSpawn;
            if (NavMesh.SamplePosition(randomPos, out var hit, distanceToSpawn, NavMesh.AllAreas))
            {
                var enemy = Instantiate(enemyToSpawn, hit.position, Random.rotation);
                enemy.GetComponent<EnemyAI>().SetSpawner(this);
                spawnedEnemies.Add(enemy);
                enemiesOnField++;
            }
        }

        hasSpawnedRecently = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        isInsideRadius = true;
        if (!timerActive) // Start the countdown when the player enters the radius
        {
            StartCountdown();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isInsideRadius = false;
    }

    private void StartCountdown()
    {
        if (countdownDuration != -1)
        {
            countdownTimer = countdownDuration;
            timerActive = true;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
            }
        }
    }

    private void OnCountdownEnd()
    {
        timerActive = false;
        countdownTimer = 0;
        // Hide the countdown when it ends
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false); 
        }
        IncreaseEnemySpawning(); // Increase enemy spawning after the timer ends
    }

    private void UpdateCountdownDisplay()
    {
        if (countdownText)
        {
            countdownText.text = Mathf.Max(0, Mathf.Round(countdownTimer)).ToString("0");
        }
    }

    public void IncreaseEnemySpawning()
    {
        maxEnemiesToSpawn += 5; // Increase the limit of enemies to spawn
    }
    /// <summary>
    /// Flash red when taking damage.
    /// </summary>
    /// <returns>Delay</returns>
    private IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOriginal;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        hpBar.fillAmount = (float) hp / spawnerHP;

        StartCoroutine(FlashRed());

        if (hp <= 0)
        {
            GameManager.instance.UpdateSpawnersGoal(1);
            // Stop the timer and hide the countdown UI when the spawner is destroyed
            if (timerActive) 
            {
                timerActive = false; // Stop the timer 

                // Hide the countdown UI if the spawner is destroyed before the time runs out
                if (countdownText != null) 
                {
                    countdownText.gameObject.SetActive(false); // Hide countdown UI 
                }
            }
            Destroy(gameObject);
        }
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
            enemiesOnField--;
        }

        Destroy(enemy);
    }
}