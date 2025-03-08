using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using NUnit.Framework.Interfaces;
using UnityEditor.SearchService;
using System.Collections.Generic;

public class CubeAgent : Agent
{
    Rigidbody rBody;
    public GameObject food;
    public Transform platform;
    public float spawnRange = 25f;
    private List<GameObject> spawnedFood;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Freeze rotation on X and Z to prevent tilting
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        spawnedFood = new List<GameObject>();
    }

    private void checkRays()
    {
        RayPerceptionSensorComponent3D sensorComponent = GetComponent<RayPerceptionSensorComponent3D>();

        // sensorResults is an array of type RayPerceptionOutput.RayOutput
        var sensorResults = RayPerceptionSensor.Perceive(sensorComponent.GetRayPerceptionInput(), false).RayOutputs;

        for (int i = 0; i < sensorResults.Length; i++)
        {
            RayPerceptionOutput.RayOutput result = sensorResults[i];
            if (result.HitTagIndex != -1 && result.HitFraction <= 0.1f)
            {
                GameObject hitObject = result.HitGameObject;
                if (spawnedFood.Contains(hitObject))
                {
                    spawnedFood.Remove(hitObject);
                    Destroy(hitObject);
                    SetReward(1.5f);
                }
            }
        }
    }

    private RayPerceptionOutput.RayOutput[] getRayResults()
    {
        RayPerceptionSensorComponent3D sensorComponent = GetComponent<RayPerceptionSensorComponent3D>();

        // sensorResults is an array of type RayPerceptionOutput.RayOutput
        var sensorResults = RayPerceptionSensor.Perceive(sensorComponent.GetRayPerceptionInput(), false).RayOutputs;

        return sensorResults;
    }

    public override void OnEpisodeBegin()
    {
        Vector3 startPosition = platform.transform.position + new Vector3(0f, 0.5f, 0f);
        transform.position = startPosition;
        rBody.linearVelocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;

        foreach (var foodItem in spawnedFood)
        {
            if (foodItem != null) Destroy(foodItem);
        }
        spawnedFood.Clear();

        // Instantiate food clones
        float safeRange = spawnRange * 0.8f;
        for (int i = 0; i < 10; i++)
        {
            Vector3 spawnOffset = new Vector3(
                Random.Range(-safeRange, safeRange),
                0.5f,
                Random.Range(-safeRange, safeRange)
            );
            Vector3 spawnPosition = platform.transform.position + spawnOffset;
            GameObject newFood = Instantiate(food, spawnPosition, transform.rotation);
            spawnedFood.Add(newFood);
        }
    }

    public float speed = 5f;
    public float rotationSpeed = 10f; // Adjust for smooth rotation

    public override void CollectObservations(VectorSensor sensor)
    {

        // Agent velocity
        sensor.AddObservation(rBody.linearVelocity.x);
        sensor.AddObservation(rBody.linearVelocity.z);

        // Agent position
        sensor.AddObservation(this.transform.localPosition);

        RayPerceptionOutput.RayOutput[] results = getRayResults();
        for (int i = 0; i < results.Length; i++)
        {
            sensor.AddObservation(results[i].EndPositionWorld);
            sensor.AddObservation(results[i].HitFraction); // distance from ray hit
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 moveDirection = new Vector3(actionBuffers.ContinuousActions[0], 0, actionBuffers.ContinuousActions[1]);

        // Directly set velocity for sliding
        rBody.linearVelocity = new Vector3(moveDirection.x * speed, rBody.linearVelocity.y, moveDirection.z * speed);

        // Rotate cube to face movement direction
        if (moveDirection.magnitude > 0.1f) // Ensure movement before rotating
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // CheckRays
        checkRays(); // manages food collection and rewards 

        // All food collected
        if (spawnedFood.Count == 0)
        {
            SetReward(5.0f); // Positive reward for collecting all food
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}