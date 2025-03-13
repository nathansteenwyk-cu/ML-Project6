using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using NUnit.Framework.Interfaces;
using UnityEditor.SearchService;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class CubeAgent : Agent
{
    Rigidbody rBody;
    public GameObject food;
    public Transform platform;
    
    public float maxSteps = 1000f;
    private float stepCount;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Freeze rotation on X and Z to prevent tilting
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            // Add a positive reward for collecting food
            AddReward(1.0f/food_maintain.getCount());

            // Log to console for debugging
            Debug.Log("Food collected: " + 1.0f / food_maintain.getCount());

            Destroy(other.gameObject);
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

        food_maintain.clearFood();
        food_maintain.spawnFood(platform, food);
        print("calling spawn");
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
        if (food_maintain.getCount() == 0)
        {
            EndEpisode();
        }
        if (stepCount == maxSteps)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }
}