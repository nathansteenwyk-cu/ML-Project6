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

    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Freeze rotation on X and Z to prevent tilting
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void checkRays() {
        RayPerceptionSensorComponent3D sensorComponent = GetComponent<RayPerceptionSensorComponent3D>();

        // sensorResults is an array of type RayPerceptionOutput.RayOutput
        var sensorResults = RayPerceptionSensor.Perceive(sensorComponent.GetRayPerceptionInput(),false).RayOutputs;
    
        for (int i = 0; i < sensorResults.Length; i++) {
            RayPerceptionOutput.RayOutput result = sensorResults[i];
            if (result.HitTagIndex != -1) {
                if (result.HitFraction <= 0.02f) {
                    Destroy(result.HitGameObject);
                    SetReward(1.0f);
                }
            }
        }
    }

    private RayPerceptionOutput.RayOutput[] getRayResults() {
        RayPerceptionSensorComponent3D sensorComponent = GetComponent<RayPerceptionSensorComponent3D>();

        // sensorResults is an array of type RayPerceptionOutput.RayOutput
        var sensorResults = RayPerceptionSensor.Perceive(sensorComponent.GetRayPerceptionInput(),false).RayOutputs;

        return sensorResults;
    }   

    public override void OnEpisodeBegin()
    {
        // If the Agent fell, reset position and velocity
        if (this.transform.localPosition.y < 0)
        {
            rBody.linearVelocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        // Instantiate food clones
        for (int i = 0; i < 10; i++)
        {
            Instantiate(food, new Vector3(Random.Range(-24f, 24f), 0.5f, Random.Range(-24f, 24f)), transform.rotation);
        }
    }

    public float speed = 5f;
    public float rotationSpeed = 10f; // Adjust for smooth rotation

    public override void CollectObservations(VectorSensor sensor) {
    
        // Agent velocity
        sensor.AddObservation(rBody.linearVelocity.x);
        sensor.AddObservation(rBody.linearVelocity.z);

        // Agent position
        sensor.AddObservation(this.transform.localPosition);

        RayPerceptionOutput.RayOutput[] results = getRayResults();
        for(int i = 0; i < results.Length; i++) {
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

        // Fell off platform
        if (this.transform.localPosition.y < 0) {
            SetReward(-0.5f);

            GameObject[] objects = GameObject.FindGameObjectsWithTag("Food");
            foreach(GameObject obj in objects) {
                Destroy(obj);
            }
            
            EndEpisode();
        }

        // All food collected
        if (GameObject.FindWithTag("Food") == null) {
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
