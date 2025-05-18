using UnityEngine;
using UnityEngine.UI;

public class ConeSnailHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private GameObject redOrbPrefab;
    [SerializeField] private Vector3 orbSpawnOffset = Vector3.zero;
    private int currentHealth;
    private bool isDead = false;

    [Header("Health Bar UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;
    [SerializeField] private float healthBarVisibilityDuration = 3f;
    [SerializeField] private bool alwaysShowHealthBar = false;

    private RectTransform healthBarRectTransform;
    private Vector2 originalSizeDelta;
    private bool isHealthBarVisible = false;
    private float healthBarTimer = 0f;

    void Start()
    {
        if (healthBarFill != null)
        {
            healthBarRectTransform = healthBarFill.rectTransform;
            healthBarRectTransform.pivot = new Vector2(1, 0.5f);
            healthBarRectTransform.anchorMin = new Vector2(1, 0.5f);
            healthBarRectTransform.anchorMax = new Vector2(1, 0.5f);
            healthBarRectTransform.anchoredPosition = Vector2.zero;
            originalSizeDelta = healthBarRectTransform.sizeDelta;
        }
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(alwaysShowHealthBar);
        }
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    void Awake()
    {
        // Only assign references, move setup to Start for consistency
    }

    void Update()
    {
        if (isHealthBarVisible && !alwaysShowHealthBar)
        {
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f)
            {
                isHealthBarVisible = false;
                if (healthBarObject != null)
                    healthBarObject.SetActive(false);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        UpdateHealthBar();
        ShowHealthBar();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarRectTransform != null)
        {
            float percent = (float)currentHealth / maxHealth;
            percent = Mathf.Clamp01(percent);
            healthBarRectTransform.sizeDelta = new Vector2(originalSizeDelta.x * percent, originalSizeDelta.y);
        }
    }

    private void ShowHealthBar()
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(true);
            isHealthBarVisible = true;
            healthBarTimer = healthBarVisibilityDuration;
        }
    }

    private void Die()
    {
        isDead = true;
        if (redOrbPrefab != null)
        {
            Instantiate(redOrbPrefab, transform.position + orbSpawnOffset, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}