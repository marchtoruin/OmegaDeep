using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerOxygenBar : MonoBehaviour
{
    [Header("Oxygen Bar UI")]
    public Image oxygenBarFill; // Reference to the fill image component
    public Image oxygenBarBackground; // Optional reference to the background
    public TextMeshProUGUI oxygenPercentText; // Reference to the percentage label

    [Header("Settings")]
    [SerializeField] private Color fillColor = Color.cyan;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private float explicitWidth = 353f;
    [SerializeField] private bool useExplicitWidth = true;

    private RectTransform fillRectTransform;
    private Vector2 originalSize;
    private bool isInitialized = false;

    void Awake()
    {
        gameObject.SetActive(true);
    }

    void Start()
    {
        InitializeOxygenBar();
    }

    public void InitializeOxygenBar()
    {
        // Find or ensure components exist
        if (oxygenBarFill == null)
        {
            Transform fillTransform = transform.Find("Fill");
            if (fillTransform != null)
            {
                oxygenBarFill = fillTransform.GetComponent<Image>();
            }
            else
            {
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(transform, false);
                fillObj.transform.SetSiblingIndex(1);
                oxygenBarFill = fillObj.AddComponent<Image>();
                oxygenBarFill.color = fillColor;
            }
        }
        fillRectTransform = oxygenBarFill.rectTransform;
        if (fillRectTransform != null)
        {
            fillRectTransform.sizeDelta = new Vector2(explicitWidth, fillRectTransform.sizeDelta.y > 0 ? fillRectTransform.sizeDelta.y : 20);
            originalSize = fillRectTransform.sizeDelta;
        }
        isInitialized = true;
    }

    public void UpdateOxygen(float percent)
    {
        if (!isInitialized) InitializeOxygenBar();
        percent = Mathf.Clamp01(percent);
        if (oxygenBarFill == null || fillRectTransform == null) return;
        fillRectTransform.sizeDelta = new Vector2(originalSize.x * percent, fillRectTransform.sizeDelta.y);
        if (oxygenPercentText != null)
            oxygenPercentText.text = Mathf.RoundToInt(percent * 100) + "%";
        Debug.Log($"[OxygenBar] UpdateOxygen called: percent={percent}, width={fillRectTransform.sizeDelta.x}, height={fillRectTransform.sizeDelta.y}, color={oxygenBarFill.color}, alpha={oxygenBarFill.color.a}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
