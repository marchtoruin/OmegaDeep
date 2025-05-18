using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private bool usePatrolPoints = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1f;
    
    [Header("Physics Settings")]
    [SerializeField] private bool modifyRigidbodyOnStart = true;
    [SerializeField] private float mass = 10f;
    [SerializeField] private float linearDrag = 3f;
    [SerializeField] private float angularDrag = 2f;
    [SerializeField] private RigidbodyType2D bodyType = RigidbodyType2D.Kinematic;
    
    // Private variables
    private Rigidbody2D rb;
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null && modifyRigidbodyOnStart)
        {
            // Configure Rigidbody2D to prevent being pushed easily
            rb.mass = mass;
            rb.drag = linearDrag;
            rb.angularDrag = angularDrag;
            rb.bodyType = bodyType;  // Kinematic means physics won't move it
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        else if (rb == null)
        {
            Debug.LogWarning("No Rigidbody2D found on " + gameObject.name, this);
        }
        
        // If using patrol points but none assigned, create warning
        if (usePatrolPoints && (patrolPoints == null || patrolPoints.Length == 0))
        {
            Debug.LogWarning("Enemy is set to use patrol points but none are assigned", this);
            usePatrolPoints = false;
        }
    }
    
    private void Update()
    {
        if (usePatrolPoints && patrolPoints.Length > 0)
        {
            PatrolBehavior();
        }
        // Add more movement behaviors here if needed
    }
    
    private void PatrolBehavior()
    {
        if (isWaiting)
            return;
            
        // Get current target patrol point
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        
        if (targetPoint != null)
        {
            // Move towards the point
            Vector2 direction = (targetPoint.position - transform.position).normalized;
            
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                // Use forces for dynamic rigidbody
                rb.AddForce(direction * movementSpeed);
            }
            else
            {
                // Use transform for kinematic or if no rigidbody
                transform.position = Vector2.MoveTowards(
                    transform.position, 
                    targetPoint.position, 
                    movementSpeed * Time.deltaTime
                );
            }
            
            // Check if reached the point
            float distanceToTarget = Vector2.Distance(transform.position, targetPoint.position);
            if (distanceToTarget < 0.1f)
            {
                // Start waiting
                StartCoroutine(WaitAtPoint());
            }
            
            // Flip based on movement direction (assuming sprite faces right originally)
            if (direction.x < 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
    }
    
    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        
        yield return new WaitForSeconds(waitTimeAtPoint);
        
        // Move to next patrol point
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }
} 