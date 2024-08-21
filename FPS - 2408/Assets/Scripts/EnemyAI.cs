using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Renderer model;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform shootPos;
    [SerializeField] private Transform headPos;
    [SerializeField] private Collider meleeCol;

    [SerializeField] private Image healthBar;
    [SerializeField] private int hp;
    private int maxHp;
    [SerializeField] private int viewAngle;
    [SerializeField] private int facePlayerSpeed;
    [SerializeField] private int roamDistance;
    [SerializeField] private int roamTimer;
    [SerializeField] private int animSpeedTrans;

    [SerializeField] private GameObject bullet;
    [SerializeField] private float shootRate;
    [SerializeField] private int shootAngle;

    private bool isShooting;
    private bool playerInRange;
    private bool isAttacking;
    private bool isRoaming;

    private float angleToPlayer;
    private float stoppingDistanceOriginal;

    private Vector3 playerDir;
    private Vector3 startingPos;

    private Color colorOriginal;

    // Start is called before the first frame update
    private void Start()
    {
        maxHp = hp; // hp should initially be max
        colorOriginal = model.material.color;
        GameManager.instance.UpdateGoal(1);
        healthBar.fillAmount = maxHp;
        stoppingDistanceOriginal = agent.stoppingDistance;
        startingPos = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        var agentSpeed = agent.velocity.normalized.magnitude;
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), agentSpeed, Time.deltaTime * animSpeedTrans));

        if (playerInRange && !CanSeePlayer())
        {
            if (!isRoaming && agent.remainingDistance < 0.05)
            {
                StartCoroutine(Roam());
            }
        }
        else if (!playerInRange)
        {
            if (!isRoaming && agent.remainingDistance < 0.05)
            {
                StartCoroutine(Roam());
            }
        }
    }

    private IEnumerator Roam()
    {
        isRoaming = true;

        yield return new WaitForSeconds(roamTimer);
        agent.stoppingDistance = 0;

        var randomDist = Random.insideUnitSphere * roamDistance;
        randomDist += startingPos;

        if (NavMesh.SamplePosition(randomDist, out var hit, roamDistance, 1))
        {
            agent.SetDestination(hit.position);
        }

        isRoaming = false;
    }

    private bool CanSeePlayer()
    {
        playerDir = GameManager.instance.player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        // Debug.Log(angleToPlayer);
        Debug.DrawRay(headPos.position, playerDir);

        if (Physics.Raycast(headPos.position, playerDir, out var hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= viewAngle)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                if (!isShooting && angleToPlayer <= shootAngle) StartCoroutine(Shoot());
                if (agent.remainingDistance <= agent.stoppingDistance) FacePlayer();

                agent.stoppingDistance = stoppingDistanceOriginal;
                return true;
            }
        }

        agent.stoppingDistance = 0;
        return false;
    }

    private void FacePlayer()
    {
        var rot = Quaternion.LookRotation(playerDir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * facePlayerSpeed);
    }

    /// <inheritdoc/>
    public void TakeDamage(int amount)
    {
        hp -= amount;
        agent.SetDestination(GameManager.instance.player.transform.position);
        StopCoroutine(Roam());

        StartCoroutine(FlashRed());

        healthBar.fillAmount = (float) hp / maxHp;

        if (hp <= 0)
        {
            hp = 0;
            GameManager.instance.UpdateGoal(-1);
            Destroy(gameObject);
        }
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

    /// <summary>
    /// Shoot bullet at player.
    /// </summary>
    /// <returns>Delay</returns>
    private IEnumerator Shoot()
    {
        isShooting = true;
        anim.SetTrigger("Shoot");
        Instantiate(bullet, shootPos.position, transform.rotation);
        yield return new WaitForSeconds(shootRate);
        isShooting = false;
    }

    /// <summary>
    /// Create bullet at specific time in animation.
    /// </summary>
    public void CreateBullet()
    {
        Instantiate(bullet, shootPos.position, transform.rotation);
    }

    public void MeleeColOn()
    {
        meleeCol.enabled = true;
    }

    public void MeleeColOff()
    {
        meleeCol.enabled = false;
    }

    /// <summary>
    /// When the player enters the enemy's range, set playerInRange to true.
    /// </summary>
    /// <param name="other">object entering the trigger</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    /// <summary>
    /// When the player exits the enemy's range, set playerInRange to false.
    /// </summary>
    /// <param name="other">object exiting the trigger</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            agent.stoppingDistance = 0;
        }
    }
}