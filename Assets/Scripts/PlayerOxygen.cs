using System.Collections;
using UnityEngine;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float idleDrainPerSecond = 1f;
    [SerializeField] private float boostDrainPerSecond = 5f;
    [SerializeField] private float shootDrain = 2f;
    [SerializeField] private bool showDebug = false;

    [Header("References")]
    [SerializeField] private PlayerOxygenBar oxygenBar;
    [SerializeField] private PlayerHealth playerHealth; // To call Die() on zero oxygen

    private float currentOxygen;
    private bool isDead = false;
    private bool isBoosting = false;

    void Awake()
    {
        currentOxygen = maxOxygen;
        if (oxygenBar == null)
        {
            oxygenBar = FindObjectOfType<PlayerOxygenBar>();
        }
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
        UpdateOxygenBar();
    }

    void Update()
    {
        if (isDead) return;
        // Drain oxygen over time
        DrainOxygen(idleDrainPerSecond * Time.deltaTime);
        // If boosting, drain extra
        if (isBoosting)
        {
            DrainOxygen(boostDrainPerSecond * Time.deltaTime);
        }
    }

    public void SetBoosting(bool boosting)
    {
        isBoosting = boosting;
    }

    public void DrainOxygen(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentOxygen -= amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
        UpdateOxygenBar();
        if (showDebug) Debug.Log($"[Oxygen] Drained {amount}, now {currentOxygen}/{maxOxygen}");
        if (currentOxygen <= 0f && !isDead)
        {
            isDead = true;
            if (playerHealth != null)
            {
                Debug.Log("[Oxygen] Triggering player death due to oxygen depletion.");
                playerHealth.ForceDie();
            }
            else
            {
                Debug.LogError("[Oxygen] playerHealth is null! Cannot trigger death.");
            }
        }
    }

    public void DrainOxygenForShot()
    {
        DrainOxygen(shootDrain);
    }

    public void RefillOxygen(float amount)
    {
        if (amount <= 0f) return;
        currentOxygen += amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
        isDead = false;
        UpdateOxygenBar();
        if (showDebug) Debug.Log($"[Oxygen] Refilled {amount}, now {currentOxygen}/{maxOxygen}");
    }

    private void UpdateOxygenBar()
    {
        if (oxygenBar != null)
        {
            oxygenBar.UpdateOxygen(currentOxygen / maxOxygen);
        }
    }

    public float GetCurrentOxygen() => currentOxygen;
    public float GetMaxOxygen() => maxOxygen;
} 