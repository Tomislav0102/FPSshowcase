using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChaseNode : Node
{
    Transform _target;
    NavMeshAgent _agent;
    EnemyAI _enemyAI;

    public ChaseNode(Transform target, NavMeshAgent agent, EnemyAI enemyAI)
    {
        _target = target;
        _agent = agent;
        _enemyAI = enemyAI;
    }

    public override NodeState Evaluate()
    {
        _enemyAI.SetColor(Color.yellow);
        float dist = Vector3.Distance(_target.position, _agent.transform.position);

        if(dist > 1f )
        {
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
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
