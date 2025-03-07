using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

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


    public override void OnEpisodeBegin()
    {
        // If the Agent fell, reset position and velocity
        if (this.transform.localPosition.y < 0)
        {
            rBody.linearVelocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        // Move the target to a new spot
        for (int i = 0; i < 10; i++)
        {
            Instantiate(food, new Vector3(Random.Range(-24f, 24f), 0.5f, Random.Range(-24f, 24f)), transform.rotation);
        }
    }

    public float speed = 5f;
    public float rotationSpeed = 10f; // Adjust for smooth rotation

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

        // Rewards
        //float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Reached target
        /*if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            EndEpisode();
        }*/

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
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
