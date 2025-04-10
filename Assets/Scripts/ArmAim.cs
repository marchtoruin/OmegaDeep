using UnityEngine;

public class ArmAim : MonoBehaviour
{
    public Transform armPivot;      // The pivot point for aiming rotation
    public SpriteRenderer rightArm; // The right arm sprite renderer
    public SpriteRenderer leftArm;  // The left arm sprite renderer
    public SpriteRenderer playerSprite; // Reference to player's sprite renderer
    
    [Range(0, 180)]
    public float maxAimAngle = 70f; // Maximum aiming angle from horizontal
    
    // Add an offset to adjust left arm rotation if needed
    public float leftArmRotationOffset = 180f;
    
    // Position offsets for arm positioning
    [Header("Arm Pivot Position Offsets")]
    [Tooltip("Local offset applied to ArmPivot when facing right")]
    public Vector2 rightOffset = Vector2.zero; // Now offsets the ArmPivot itself
    
    [Tooltip("Local offset applied to ArmPivot when facing left")]
    public Vector2 leftOffset = Vector2.zero; // Now offsets the ArmPivot itself

    private Camera mainCam;
    // Changed to public property to expose to other scripts
    private bool _isFacingRight = true;
    public bool IsFacingRight { get { return _isFacingRight; } }
    private Vector3 originalPivotLocalPos; // Store ArmPivot's starting local position

    // Variables to store calculated values from Update
    private float calculatedAngle = 0f;
    private Vector2 currentPivotOffset = Vector2.zero; // Store the chosen offset for the pivot
    private bool calculatedShouldFaceLeft = false;

    void Awake()
    {
        // Verify required references
        if (armPivot == null) Debug.LogError("ArmPivot reference is missing!");
        else originalPivotLocalPos = armPivot.localPosition; // Store original local position

        mainCam = Camera.main;
         if (mainCam == null) Debug.LogError("Main Camera not found!");
    }

    void Start()
    {
       // Initialize visual state
        if (rightArm != null) rightArm.gameObject.SetActive(true);
        if (leftArm != null) leftArm.gameObject.SetActive(false);
        // Initial pivot position will be set in the first LateUpdate
    }
    
    void Update()
    {
        // --- Calculations Only in Update ---
        // Check references needed for calculation
        if (armPivot == null || mainCam == null) return;

        // Calculate aiming direction and angle
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - (Vector2)armPivot.position; // Aiming relative to current pivot position
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        calculatedShouldFaceLeft = mousePos.x < armPivot.position.x;

        if (calculatedShouldFaceLeft)
        {
            currentPivotOffset = leftOffset; // Choose the pivot offset
            calculatedAngle = angle + leftArmRotationOffset;
            if (calculatedAngle > 180f) calculatedAngle -= 360f;
            calculatedAngle = Mathf.Clamp(calculatedAngle, -maxAimAngle, maxAimAngle);
        }
        else // Facing Right
        {
            currentPivotOffset = rightOffset; // Choose the pivot offset
            calculatedAngle = Mathf.Clamp(angle, -maxAimAngle, maxAimAngle);
        }
    }

    void LateUpdate()
    {
        // --- Apply Transforms and Visual State in LateUpdate ---

        // Check reference
        if (armPivot == null) return;

        // --- Apply Position Offset and Rotation ---

        // 1. Calculate the target local position for the ArmPivot
        Vector3 targetPivotLocalPos = originalPivotLocalPos + (Vector3)currentPivotOffset;

        // 2. Apply the calculated local position to ArmPivot
        Vector3 previousLocalPos = armPivot.localPosition;
        float distanceThreshold = 0.0001f;
        if (Vector3.Distance(previousLocalPos, targetPivotLocalPos) > distanceThreshold)
        {
            armPivot.localPosition = targetPivotLocalPos;
        }

        // 3. Apply the aiming rotation to ArmPivot (world rotation)
        armPivot.rotation = Quaternion.Euler(0f, 0f, calculatedAngle);

        // --- Update Visual State ---
         if (calculatedShouldFaceLeft)
         {
            if (_isFacingRight)
            {
                _isFacingRight = false;
                if (playerSprite != null) playerSprite.flipX = true;
            }
            if (rightArm != null && rightArm.gameObject.activeSelf) rightArm.gameObject.SetActive(false);
            if (leftArm != null && !leftArm.gameObject.activeSelf) leftArm.gameObject.SetActive(true);
         }
         else // Facing Right
         {
            if (!_isFacingRight)
            {
                _isFacingRight = true;
                if (playerSprite != null) playerSprite.flipX = false;
            }
            if (rightArm != null && !rightArm.gameObject.activeSelf) rightArm.gameObject.SetActive(true);
            if (leftArm != null && leftArm.gameObject.activeSelf) leftArm.gameObject.SetActive(false);
         }
    }
}
