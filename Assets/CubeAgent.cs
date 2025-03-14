using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using NUnit.Framework.Interfaces;
using UnityEditor.SearchService;

public class CubeAgent : Agent
{
    Rigidbody rBody;
    public GameObject food;
    public Transform platform;
    public float spawnRange = 25f;
    public float maxSteps = 1000f;
    private float stepCount;
    private EpisodeManager episodeManager;
    public int numFood = 10;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Freeze rotation on X and Z to prevent tilting
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        episodeManager = FindObjectOfType<EpisodeManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            // Add a positive reward for collecting food
            AddReward(3.0f/GetNumFood());

            // Log to console for debugging
            Debug.Log("Food collected: " + 1.0f / GetNumFood());

            Destroy(other.gameObject);
        }
        else if(other.CompareTag("Wall")) {
            AddReward(-.01f);

            // Log to console for debugging
            Debug.Log("Wall hit: -0.01");
        }
    }

    public override void OnEpisodeBegin()
    {
        stepCount = 0;
        Vector3 startPosition = platform.transform.position + new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rBody.linearVelocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;

        foreach (Transform child in transform.parent)
        {
            // Check if the child has the "Food" tag
            if (child.CompareTag("Food"))
            {
                Destroy(child.gameObject); // Destroy the GameObject
            }
        }

        // Instantiate food clones
        float safeRange = spawnRange * 0.8f;
        for (int i = 0; i < numFood; i++) 
        {
            Vector3 spawnOffset = new Vector3(Random.Range(-safeRange, safeRange),0.5f,Random.Range(-safeRange, safeRange));
            Vector3 spawnPosition = platform.transform.position + spawnOffset;
            GameObject newFood = Instantiate(food, spawnPosition, transform.rotation, transform.parent);
        }
    }

    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        stepCount++;
        SetReward(-0.0001f);

        float moveInput = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f); // Forward/backward speed
        float rotateInput = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f); // Rotation speed

        // Move in the direction the agent is facing (forward in 2D: X-Z plane)
        Vector3 moveDirection = transform.forward * moveInput; // Forward is along agent's local Z-axis
        Vector3 velocity = new Vector3(moveDirection.x, 0, moveDirection.z) * moveSpeed; // No normalization
        rBody.linearVelocity = new Vector3(velocity.x, rBody.linearVelocity.y, velocity.z); // Preserve Y velocity if any

        // Rotate using Quaternion
        if (Mathf.Abs(rotateInput) > 0.1f) // Only rotate if input is significant
        {
            // Rotation speed scaled similarly to moveSpeed
            float rotationDelta = rotateInput * rotationSpeed * Time.deltaTime * 36f; // Adjust scale with multiplier
            Quaternion rotationChange = Quaternion.Euler(0, rotationDelta, 0); // Rotate around Y-axis
            transform.rotation = transform.rotation * rotationChange; // Apply rotation incrementally
        }
        // All food collected
        if (GetNumFood() == 0 || stepCount == maxSteps)
        {
            AddReward(1.0f);
            episodeManager.RequestEpisodeReset(this);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    int GetNumFood()
    {
        // Find all GameObjects with the tag in the scene
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("Food");
        int count = 0;

        // Filter to only include children of the parent
        foreach (GameObject obj in taggedObjects)
        {
            if (obj.transform.IsChildOf(transform.parent))
            {
                count++;
            }
        }

        return count;
    }
}