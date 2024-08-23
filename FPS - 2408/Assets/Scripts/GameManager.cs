using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GameObject menuActive;
    [SerializeField] private GameObject menuPause;
    [SerializeField] private GameObject menuWin;
    [SerializeField] private GameObject menuLose;

    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text GrenadeInteractTxt;
    #region Player

    public GameObject playerSpawnPos { get; private set; }
    public GameObject player { get; private set; }
    public PlayerController playerScript { get; private set; }
    public Image healthBar;
    public Image fuelBar;
    public GameObject damageFlash;

    #endregion

    public GameObject checkpointPopup;

    public bool isPaused;

    private int enemyCount;

    // GK: Custom timeScale, should be 1 by default.
    [SerializeField] private int timeScale = 1;

    // Awake allows for initialization before other game objects
    private void Awake()
    {
        instance = this;
        Time.timeScale = timeScale;
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<PlayerController>();
        playerSpawnPos = GameObject.FindWithTag("Player Spawn Pos");
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (!menuActive)
            {
                StatePause();
                menuActive = menuPause;
                menuActive.SetActive(isPaused);
            }
            else if (menuActive == menuPause)
            {
                StateUnpause();
            }
        }
    }

    /// <summary>
    /// Set the state of the game to "Paused"
    /// </summary>
    public void StatePause()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // GK: Personally dislike exclusive windowed, so it's None.
    }

    /// <summary>
    /// Set the state of the game to "Unpaused"
    /// </summary>
    public void StateUnpause()
    {
        isPaused = !isPaused;
        Time.timeScale = timeScale; // GK: Utilize stored timescale.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(isPaused);
        menuActive = null;
    }

    /// <summary>
    /// Update the HP bar fill amount.
    /// </summary>
    ///<param name="hpCurr"> Players current HP</param>
    ///<param name="hpMax"> Players max HP</param>
    public void UpdateHealthBar(float hpCurr, int hpMax)
    {
        // Debug.Log($"Updating Health Bar: Current HP = {hpCurr}/{hpMax}");
        if (healthBar)
        {
            healthBar.fillAmount = hpCurr / hpMax;
            healthText.text = $"{(int) hpCurr} / {hpMax}";
            // Debug.Log("Health Bar Fill Amount: " + healthBar.fillAmount);
        }
        else
        {
            Debug.LogError("Health Bar reference is missing!");
        }
    }

    /// <summary>
    /// Update the Fuel bar fill amount.
    /// </summary>
    ///<param name="fuelCurr"> Players current Fuel</param>
    ///<param name="fuelMax"> Players max Fuel</param>
    public void UpdateFuelBar(float fuelCurr, int fuelMax)
    {
        Debug.Log($"Updating Fuel Bar: Current Fuel = {fuelCurr}/{fuelMax}");
        if (fuelBar)
        {
            fuelBar.fillAmount = fuelCurr / fuelMax;
            Debug.Log("Fuel Bar Fill Amount: " + fuelBar.fillAmount);
        }
        else
        {
            Debug.LogError("Fuel Bar reference is missing!");
        }
    }

    /// <summary>
    /// Update the goal amount.
    /// </summary>
    /// <param name="amount">Amount of enemies killed</param>
    public void UpdateGoal(int amount)
    {
        enemyCount += amount;
        enemyCountText.text = enemyCount.ToString("F0");

        if (enemyCount <= 0)
        {
            StatePause();
            menuActive = menuWin;
            menuActive.SetActive(isPaused);
        }
    }

    /// <summary>
    /// Set state to lost
    /// </summary>
    public void StateLost()
    {
        StatePause();
        menuActive = menuLose;
        menuActive.SetActive(isPaused);
    }

    public void UpdateGrenadeInteractText(string text)
    {
        GrenadeInteractTxt.text = text;
    }
}