using UnityEngine;

public class ArmAim : MonoBehaviour
{
    [Header("Core References")]
    public Transform armPivot;      // The pivot point for aiming rotation
    public SpriteRenderer rightArm; // The right arm sprite renderer
    public SpriteRenderer leftArm;  // The left arm sprite renderer
    public SpriteRenderer playerSprite; // Reference to player's sprite renderer

    // Add references for other flippable objects
    [Header("Other Flippable Child Objects")]
    [Tooltip("Assign the 'FaceMaskLightPivot' GameObject")]
    [SerializeField] private Transform faceMaskLightPivot;
    [Tooltip("Assign the 'bloodSpatter_1' GameObject")]
    [SerializeField] private Transform bloodSplat;
    [Tooltip("Assign the 'HelmetBubbleEmitter' GameObject")]
    [SerializeField] private Transform helmetBubbleEmitter;
    [Tooltip("Assign the 'Flashlight' GameObject (optional, needed for direct control)")]
    [SerializeField] private FlashlightController flashlightController; // Keep existing reference logic
    [Tooltip("Assign the 'FlashlightPoint' SpriteRenderer (or it will be found)")]
    [SerializeField] private SpriteRenderer flashlightPointRenderer; // Reference to the problematic child

    [Header("Child Component References")]
    [Tooltip("Assign the GameObject with the HelmetBubbleEmitter script, or it will be found on the assigned Transform.")]
    [SerializeField] private HelmetBubbleEmitter helmetBubbleEmitterComponent;

    [Header("Other Flippable Particle Systems")]
    [Tooltip("Assign the 'BoostBubbleEmitter' GameObject's Transform")]
    [SerializeField] private Transform boostBubbleTransform;
    [Tooltip("Cached reference to the BoostBubble ParticleSystem component")]
    [SerializeField] private ParticleSystem boostBubbleParticleSystem;

    [Header("Aiming Settings")]
    [Range(0, 180)]
    public float maxAimAngle = 70f; // Maximum aiming angle from horizontal
    public float leftArmRotationOffset = 180f;

    [Header("Arm Pivot Position Offsets")]
    [Tooltip("Local offset applied to ArmPivot when facing right")]
    public Vector2 rightOffset = Vector2.zero;
    [Tooltip("Local offset applied to ArmPivot when facing left")]
    public Vector2 leftOffset = Vector2.zero;

    [Header("Flicker Prevention")]
    [Tooltip("Buffer zone to prevent rapid switching when mouse is near the player")]
    public float directionSwitchThreshold = 0.5f;

    private Camera mainCam;
    private bool _isFacingRight = true;
    public bool IsFacingRight { get { return _isFacingRight; } }
    private Vector3 originalPivotLocalPos;

    // Variables to store calculated values from Update
    private float calculatedAngle = 0f;
    private Vector2 currentPivotOffset = Vector2.zero;
    private bool calculatedShouldFaceLeft = false;

    // Store original states for child objects
    private float faceMaskLightOriginalOffsetX = 0f;
    private Quaternion faceMaskLightOriginalRotation;
    [SerializeField] private float bloodSplatOffsetX = 0.5f; // Make this configurable like in DiverMovement
    private Vector3 originalHelmetBubbleScale;
    private bool helmetScaleInitialized = false;
    private Quaternion originalBoostBubbleRotation; // Store original rotation
    private Vector3 originalBoostBubbleScale; // Store original scale

    void Awake()
    {
        // Verify required references
        if (armPivot == null) Debug.LogError("ArmPivot reference is missing!");
        else originalPivotLocalPos = armPivot.localPosition;

        mainCam = Camera.main;
        if (mainCam == null) Debug.LogError("Main Camera not found!");

        // Get the FlashlightController if not assigned (keep existing logic)
        if (flashlightController == null)
        {
            flashlightController = GetComponentInChildren<FlashlightController>(true);
            if (flashlightController == null)
            {
                 Debug.LogWarning("ArmAim: Could not find FlashlightController in children.");
            }
        }

        // Find FlashlightPoint Renderer if not assigned
        if (flashlightPointRenderer == null && flashlightController != null)
        {
            Transform pointTransform = flashlightController.transform.Find("FlashlightPoint");
            if (pointTransform != null)
            {
                flashlightPointRenderer = pointTransform.GetComponent<SpriteRenderer>();
            }
            if (flashlightPointRenderer == null)
            {
                Debug.LogWarning("[ArmAim] Could not find SpriteRenderer on FlashlightPoint child of FlashlightController.");
            }
        }
        else if (flashlightPointRenderer == null)
        {
            Debug.LogWarning("[ArmAim] FlashlightPoint Renderer not assigned and FlashlightController not found.");
        }

        // Initialize original states for other flippable objects
        if (faceMaskLightPivot != null)
        {
            faceMaskLightOriginalOffsetX = faceMaskLightPivot.localPosition.x;
            faceMaskLightOriginalRotation = faceMaskLightPivot.localRotation;
            Debug.Log($"[ArmAim] Stored FaceMaskLight original offsetX: {faceMaskLightOriginalOffsetX}, originalRot: {faceMaskLightOriginalRotation.eulerAngles}", this);
        }
        else
        {
             Debug.LogWarning("[ArmAim] FaceMaskLightPivot not assigned in inspector! Light flipping won't work.", this);
        }

        if (helmetBubbleEmitter != null)
        {
            originalHelmetBubbleScale = helmetBubbleEmitter.localScale;
            helmetScaleInitialized = true;
        }
         else
        {
             Debug.LogWarning("[ArmAim] HelmetBubbleEmitter not assigned in inspector! Emitter flipping won't work.", this);
        }

         if (bloodSplat == null)
         {
              Debug.LogWarning("[ArmAim] BloodSplat transform not assigned in inspector! Blood flipping won't work.", this);
         }

        // Get HelmetBubbleEmitter component if not assigned
        if (helmetBubbleEmitterComponent == null && helmetBubbleEmitter != null)
        {
            helmetBubbleEmitterComponent = helmetBubbleEmitter.GetComponent<HelmetBubbleEmitter>();
        }
        if (helmetBubbleEmitterComponent == null)
        {
            Debug.LogWarning("[ArmAim] HelmetBubbleEmitter component could not be found! Emitter flipping won't work.", this);
        }

        if (boostBubbleTransform != null)
        {
            originalBoostBubbleRotation = boostBubbleTransform.localRotation;
            originalBoostBubbleScale = boostBubbleTransform.localScale; // Store scale too
            Debug.Log($"[ArmAim] Stored BoostBubble original rotation: {originalBoostBubbleRotation.eulerAngles}, original scale: {originalBoostBubbleScale}", this);
            
            // Get the particle system component
            if (boostBubbleParticleSystem == null)
            {
                boostBubbleParticleSystem = boostBubbleTransform.GetComponent<ParticleSystem>();
                if (boostBubbleParticleSystem == null)
                {
                    Debug.LogWarning("[ArmAim] Couldn't find ParticleSystem on BoostBubbleEmitter.", this);
                }
            }
        }
        else
        {
            Debug.LogWarning("[ArmAim] BoostBubbleTransform not assigned in inspector! Boost bubble flipping won't work.", this);
        }

        SetupArmRenderSettings();
    }
    
    private void SetupArmRenderSettings()
    {
        // Ensure the arms have consistent Z position
        if (rightArm != null)
        {
            // Ensure consistent sorting settings
            rightArm.sortingOrder = 10; // Adjust this value as needed
            
            // Ensure the Z scale is exactly 1
            Transform rightArmTransform = rightArm.transform;
            Vector3 rightScale = rightArmTransform.localScale;
            rightScale.z = 1f;
            rightArmTransform.localScale = rightScale;
        }
        
        if (leftArm != null)
        {
            // Ensure consistent sorting settings
            leftArm.sortingOrder = 10; // Same as right arm
            
            // Ensure the Z scale is exactly 1
            Transform leftArmTransform = leftArm.transform;
            Vector3 leftScale = leftArmTransform.localScale;
            leftScale.z = 1f;
            leftArmTransform.localScale = leftScale;
        }
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
        
        // Add hysteresis to prevent flickering when mouse is near the player horizontally
        float horizontalDistance = mousePos.x - armPivot.position.x;
        
        // Only change direction if the mouse moves beyond the threshold in the opposite direction
        if (_isFacingRight && horizontalDistance < -directionSwitchThreshold)
        {
            calculatedShouldFaceLeft = true;
        }
        else if (!_isFacingRight && horizontalDistance > directionSwitchThreshold)
        {
            calculatedShouldFaceLeft = false;
        }
        else
        {
            // Keep current direction if within threshold
            calculatedShouldFaceLeft = !_isFacingRight;
        }

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

        // 3. Apply the aiming rotation to ArmPivot (strictly in 2D)
        // FIXED: Use eulerAngles for 2D rotation instead of Quaternion to prevent Z drift
        Vector3 eulerRotation = armPivot.eulerAngles;
        eulerRotation.z = calculatedAngle;
        eulerRotation.x = 0f;
        eulerRotation.y = 0f;
        armPivot.eulerAngles = eulerRotation;

        // --- Update Visual State (Arms & Player Sprite) ---
        bool facingDidChange = false;
         if (calculatedShouldFaceLeft)
         {
            if (_isFacingRight)
            {
                _isFacingRight = false;
                facingDidChange = true;
                if (playerSprite != null) playerSprite.flipX = true;
                if (rightArm != null) rightArm.gameObject.SetActive(false);
                if (leftArm != null) leftArm.gameObject.SetActive(true);
            }
         }
         else // Facing Right
         {
            if (!_isFacingRight)
            {
                _isFacingRight = true;
                facingDidChange = true;
                if (playerSprite != null) playerSprite.flipX = false;
                if (rightArm != null) rightArm.gameObject.SetActive(true);
                if (leftArm != null) leftArm.gameObject.SetActive(false);
            }
         }

         // --- Flip Other Child Objects (if facing direction changed) ---
         if (facingDidChange)
         {
             FlipChildObjects();
         }

         // --- Update Other Flippable Objects Based on _isFacingRight ---

         // Tell Flashlight Controller its state (existing logic)
         flashlightController?.UpdateFlipState(_isFacingRight);

         // --- Add Debug Checks for FlashlightPoint when facing LEFT ---
         if (!_isFacingRight && flashlightPointRenderer != null)
         {
             Debug.Log($"[ArmAim Debug LEFT]: FP Enabled={flashlightPointRenderer.enabled}, FP GO Active={flashlightPointRenderer.gameObject.activeInHierarchy}, FP World Pos={flashlightPointRenderer.transform.position}");
         }
         else if (_isFacingRight && flashlightPointRenderer != null)
         {
             // Optional: Log when facing right for comparison
             // Debug.Log($"[ArmAim Debug RIGHT]: FP Enabled={flashlightPointRenderer.enabled}, FP GO Active={flashlightPointRenderer.gameObject.activeInHierarchy}, FP World Pos={flashlightPointRenderer.transform.position}");
         }
    }

    private void FlipChildObjects()
    {
        // Flip Face Mask Light Pivot
        if (faceMaskLightPivot != null)
        {
            float targetPosX = _isFacingRight ? faceMaskLightOriginalOffsetX : -faceMaskLightOriginalOffsetX;
            Quaternion targetRot = _isFacingRight ? faceMaskLightOriginalRotation : Quaternion.Euler(
                faceMaskLightOriginalRotation.eulerAngles.x,
                faceMaskLightOriginalRotation.eulerAngles.y + 180f,
                faceMaskLightOriginalRotation.eulerAngles.z
            );
            faceMaskLightPivot.localPosition = new Vector3(targetPosX, faceMaskLightPivot.localPosition.y, faceMaskLightPivot.localPosition.z);
            faceMaskLightPivot.localRotation = targetRot;
        }

        // Flip Blood Spatter Position AND Scale
        if (bloodSplat != null)
        {
            float targetPosX = _isFacingRight ? bloodSplatOffsetX : -bloodSplatOffsetX;
            float targetScaleX = _isFacingRight ? Mathf.Abs(bloodSplat.localScale.x) : -Mathf.Abs(bloodSplat.localScale.x);
            bloodSplat.localPosition = new Vector3(targetPosX, bloodSplat.localPosition.y, bloodSplat.localPosition.z);
            bloodSplat.localScale = new Vector3(targetScaleX, bloodSplat.localScale.y, bloodSplat.localScale.z);
        }

        // *** ADDED: Call SetFacingDirection on HelmetBubbleEmitter component ***
        helmetBubbleEmitterComponent?.SetFacingDirection(_isFacingRight);
        
        // *** MODIFIED: Remove scale flip, ADD rotation flip for BoostBubbleEmitter ***
        if (boostBubbleTransform != null)
        {
            // --- Scale flipping REMOVED ---
            // float targetScaleX = _isFacingRight ? -Mathf.Abs(originalBoostBubbleScale.x) : Mathf.Abs(originalBoostBubbleScale.x);
            // boostBubbleTransform.localScale = new Vector3(targetScaleX, originalBoostBubbleScale.y, originalBoostBubbleScale.z);
            
            // Rotate around Y axis based on facing direction
            bool shouldBeFlipped = !_isFacingRight; // True if facing left
            Quaternion targetRotation = shouldBeFlipped 
                ? Quaternion.Euler(0f, 180f, 0f) 
                : Quaternion.identity;
            boostBubbleTransform.localRotation = targetRotation;
        }
    }
}
