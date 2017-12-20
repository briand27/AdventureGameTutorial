using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI; // for Unity 5.5 or later

public class PlayerMovement : MonoBehaviour {

    public Animator animator;
    public NavMeshAgent agent;
    public float inputHoldDelay = 0.5f;
    public float turnSpeedThreshold = 0.5f;
    public float speedDampTime = 0.1f;
    public float slowingSpeed = 0.175f;
    public float turnSmoothing = 15f;

    private WaitForSeconds inputHoldWait;
    private Vector3 destinationPosition;

    private const float stopDistanceProportion = 0.1f;
    private const float navMeshSampleDistance = 4f;

    private readonly int hashSpeedParam = Animator.StringToHash("Speed");

    private void Start()
    {
        agent.updateRotation = false;

        inputHoldWait = new WaitForSeconds(inputHoldDelay);

        destinationPosition = transform.position;
    }

    private void OnAnimatorMove()
    {
        agent.velocity = animator.deltaPosition / Time.deltaTime;
    }

    private void Update()
    {
        if (agent.pathPending) return;

        // isn't calculating a path so probably already have one
        float speed = agent.desiredVelocity.magnitude;

        if (agent.remainingDistance <= 
            agent.stoppingDistance * stopDistanceProportion)
        {
            Stopping(out speed);
        }
        else if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Slowing(out speed, agent.remainingDistance);
        }
        else if (speed > turnSpeedThreshold)
        {
            Moving();
        }

        animator.SetFloat(hashSpeedParam, speed, speedDampTime, Time.deltaTime);
    }

    private void Stopping(out float speed)
    {
        // agent.Stop(); // old version
        agent.isStopped = true; // new version
        transform.position = destinationPosition;
        speed = 0f;
    }

    private void Slowing(out float speed, float distanceToDestination)
    {
        agent.isStopped = true;

        transform.position = Vector3.MoveTowards(
            transform.position, destinationPosition, 
            slowingSpeed * Time.deltaTime);

        float proportionalDistance = 1f - 
            distanceToDestination / agent.stoppingDistance;

        speed = Mathf.Lerp(slowingSpeed, 0f, proportionalDistance);
    }

    private void Moving()
    {
        Quaternion targetRotation = 
            Quaternion.LookRotation(agent.desiredVelocity);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation,
                                             turnSmoothing * Time.deltaTime);
        
    }

    public void OnGroundClick(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData) data;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            pData.pointerCurrentRaycast.worldPosition,
            out hit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            destinationPosition = hit.position;
        }
        else
        {
            destinationPosition = pData.pointerCurrentRaycast.worldPosition;
        }
        agent.SetDestination(destinationPosition);
        // agent.Resume(); // old way
        agent.isStopped = false; // new way
    }

}
