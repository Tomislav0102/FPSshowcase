using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ShootNode : Node
{
    NavMeshAgent _agent;
    EnemyAI _enemyAI;

    public ShootNode(NavMeshAgent agent, EnemyAI enemyAI)
    {
        _agent = agent;
        _enemyAI = enemyAI;
    }

    public override NodeState Evaluate()
    {
        _agent.isStopped = true;
        _enemyAI.SetColor(Color.red);
        nodeState = NodeState.Running;
        return nodeState;
    }

}
