using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamage
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private LayerMask ignoreMask;
    [SerializeField] private int hpMax;
    [SerializeField] private float hpCurrent;
    [SerializeField] private float healthRegenRate = 1f;
    [SerializeField] private int speed;
    [SerializeField] private int sprintMod;
    [SerializeField] private int jumpMax;
    [SerializeField] private int jumpSpeed;
    [SerializeField] private int gravity;
    [SerializeField] private CameraShake cameraShake;

    // Thank you Garrett for teaching me that this region stuff was a thing. This is very nice for decluttering. 

    #region WallRunning

    [SerializeField] private int wallRunGravity;
    [SerializeField] private int wallKickMax;
    [SerializeField] private int wallKickSpeed;
    [SerializeField] private bool runningOnWall;

    private GameObject lastTouchedWall;
    private bool hasWallKicked;
    private int wallKickCount;

    #endregion

    #region HealthRegen

    private bool isTakingDamage;

    #endregion

    #region JetPack Variables

    //Currently deciding whether or not this should be implemented. I'm leaning towards no atm. We'll see. 
    [SerializeField] int maxFuel;
    [SerializeField] float fuel;
    [SerializeField] float fuelRecoveryRate;
    [SerializeField] float fuelWaitTime;
    [SerializeField] float jetPackFuelCost;

    bool isUsingFuel;
    bool hasExhaustedFuel;

    private float origFuel;
    
    [SerializeField] float maxAcceleration;
    [SerializeField] float accelerationTime;
 
    #endregion

    [SerializeField] private float shootRate;
    [SerializeField] private int shootDist;

    private Vector3 move;
    private Vector3 playerVelocity;

    private int jumpCount;
    private float hpOrig;
    

    private bool isSprinting;
    private bool isShooting;

    #region Damage & Dropoff

    [SerializeField] private int minDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private float dropOffStart;
    [SerializeField] private float dropOffEnd;

    private Vector3 startPos;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        hpOrig = hpCurrent;
        GameManager.instance.UpdateHealthBar(hpCurrent, hpMax);
        GameManager.instance.UpdateFuelBar(fuel, maxFuel);
        origFuel = fuel;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
        
        if (!GameManager.instance.isPaused) // Don't handle movement/shooting if the game is paused.
        {
            Movement();
        }

        Sprint();
        if (!isTakingDamage)
        {
            // hpCurrent = Mathf.Min(hpCurrent + (int)(healthRegenRate * Time.deltaTime), hpOrig);
            Debug.Log("Regenerating health: " + healthRegenRate * Time.deltaTime);
            hpCurrent += healthRegenRate * Time.deltaTime;
            if (hpCurrent > hpOrig)
            {
                hpCurrent = hpOrig;
            }

            Debug.Log("New health: " + hpCurrent);
            GameManager.instance.UpdateHealthBar(hpCurrent, hpMax);
        }
    }

    /// <summary>
    /// Handling of sprinting mechanic.
    /// </summary>
    void Movement()
    {
        if (controller.isGrounded)
        {
            jumpCount = 0;
            playerVelocity = Vector3.zero;
            lastTouchedWall = null;
            hasExhaustedFuel = false;
            if (fuel < maxFuel && !isUsingFuel)
            {
                StartCoroutine(RegainFuel());
            }
        }

        // move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        // transform.position += move * speed * Time.deltaTime;

        move = Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right;
        controller.Move(move * (speed * Time.deltaTime));

        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            jumpCount++;
            playerVelocity.y = jumpSpeed;
        }

        //Walljumping is here. Checks if you're running on the wall, and if you have a wallkick left. If so, you can kick. 
        if (Input.GetButtonDown("Jump") && runningOnWall == true && wallKickCount < wallKickMax)
        {
            Debug.Log($"Wallkick {wallKickCount}");
            wallKickCount++;
            playerVelocity.y = wallKickSpeed;
            hasWallKicked = true;
        }

        controller.Move(playerVelocity * Time.deltaTime);

        //Wallkick stuff, basically checks if you're wallrunning, and you haven't already kicked off this wall. If
        //you haven't, your gravity gets slowed for wallrunning. 
        if (runningOnWall && hasWallKicked == false)
        {
            playerVelocity.y -= wallRunGravity * Time.deltaTime;
        } //Otherwise you use normal gravity
        else
        {
            playerVelocity.y -= gravity * Time.deltaTime;
        }

        if (Input.GetButton("Shoot") && !isShooting)
        {
            StartCoroutine(Shoot());
        }

        if(Input.GetButton("Jetpack") && !hasExhaustedFuel)
        {
            JetPack();
        }

        GameManager.instance.UpdateFuelBar(fuel, maxFuel);
    }

    /// <summary>
    /// Handling of sprinting mechanic.
    /// </summary>
    void Sprint()
    {
        if (Input.GetButtonDown("Sprint"))
        {
            speed *= sprintMod;
            isSprinting = true;
        }
        else if (Input.GetButtonUp("Sprint"))
        {
            speed /= sprintMod;
            isSprinting = false;
        }
    }

    /// <summary>
    /// Handling of shooting mechanic via raycast.
    /// </summary>
    /// <returns>Time wait based on shootRate</returns>
    IEnumerator Shoot()
    {
        isShooting = true;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hit, shootDist, ~ignoreMask))
        {
            var damage = CalcDamage(hit.distance);

            Debug.Log($"Damage @ Distance: {damage} @ {(int) hit.distance}");

            var dmg = hit.collider.GetComponent<IDamage>();
            dmg?.TakeDamage(damage);
        }

        yield return new WaitForSeconds(shootRate);
        isShooting = false;
    }

    /// <summary>
    /// Calculate the damage based on the distance traveled by the bullet.
    /// If distance is less than the dropoff start, return max damage.
    /// If distance is greater than the dropoff end, return minimum damage.
    /// </summary>
    /// <param name="distance">Distance the bullet has traveled</param>
    /// <returns>calculated damage</returns>
    private int CalcDamage(float distance)
    {
        if (distance <= dropOffStart) return maxDamage;
        if (distance > dropOffEnd) return 0 /*minDamage*/; // Once drop off end is reached, any bullets past that don't damage. 

        var range = dropOffEnd - dropOffStart;
        var normalizedDistance = (distance - dropOffStart) / range;

        return Mathf.FloorToInt(Mathf.Lerp(maxDamage, minDamage, normalizedDistance));
    }

    /// <inheritdoc/>
    public void TakeDamage(int amount)
    {
        hpCurrent -= amount;
        Debug.Log("Player took damage: " + amount + ", Current HP: " + hpCurrent);
        isTakingDamage = true;

        StartCoroutine(Flash());

        if (hpCurrent <= 0)
        {
            GameManager.instance.StateLost();
            Debug.Log("Player died.");
        }
        
        // Trigger camera shake when damaged.
        if (cameraShake && !GameManager.instance.isPaused)
        {
            cameraShake.TriggerShake();
        }

        GameManager.instance.UpdateHealthBar(hpCurrent, hpMax);

        StartCoroutine(EnableHealthRegen());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            // Debug.Log("Yep.");
            runningOnWall = true;
            if (other.gameObject != lastTouchedWall)
            {
                hasWallKicked = false;
                wallKickCount = 0;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            // Debug.Log("Nope.");
            runningOnWall = false;
            lastTouchedWall = other.gameObject;
            hasWallKicked = true;
        }
    }

    IEnumerator Flash()
    {
        GameManager.instance.damageFlash.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        GameManager.instance.damageFlash.SetActive(false);
    }
    
    IEnumerator EnableHealthRegen()
    {
        yield return new WaitForSeconds(5f);
        isTakingDamage = false;
    }


    #region JetPack Methods
    IEnumerator RegainFuel()
    {
        yield return new WaitForSeconds(.1f);
        fuel += fuelRecoveryRate;

        if(fuel > maxFuel)
        {
            fuel = maxFuel;
        }
    }

    IEnumerator StopRegainingFuel()
    {
        yield return new WaitForSeconds(fuelWaitTime);
        isUsingFuel = false;
    }

    void JetPack()
    {
        isUsingFuel = true;
        StartCoroutine(StopRegainingFuel());

        playerVelocity.y = Mathf.Lerp(playerVelocity.y, maxAcceleration, accelerationTime);

        fuel -= jetPackFuelCost * Time.deltaTime;

        if (fuel <= 0)
        {
            hasExhaustedFuel = true;
        }
    }
    #endregion
}