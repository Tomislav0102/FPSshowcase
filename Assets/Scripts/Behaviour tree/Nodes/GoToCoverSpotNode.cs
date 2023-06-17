using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GoToCoverSpotNode : Node
{
    NavMeshAgent _agent;
    EnemyAI _enemyAI;

    public GoToCoverSpotNode(NavMeshAgent agent, EnemyAI enemyAI)
    {
        _agent = agent;
        _enemyAI = enemyAI;
    }

    public override NodeState Evaluate()
    {
        Transform coverSpot = _enemyAI.bestSpot;
        if(coverSpot == null)
        {
            nodeState = NodeState.Failure;
            return nodeState;
        }

        _enemyAI.SetColor(Color.blue);

        float dist = Vector3.Distance(coverSpot.position, _agent.transform.position);
        if (dist > 1f)
        {
            _agent.isStopped = false;
            _agent.SetDestination(coverSpot.position);
            nodeState = NodeState.Running;
            return nodeState;
        }
        else
        {
            _agent.isStopped = true;
            nodeState = NodeState.Success;
            return nodeState;
        }


    }
}
