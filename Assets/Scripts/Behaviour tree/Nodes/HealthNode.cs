using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthNode : Node
{
    EnemyAI _enemyAI;
    float _treshold;

    public HealthNode(EnemyAI enemyAI, float treshold)
    {
        _enemyAI = enemyAI;
        _treshold = treshold;
    }
    public override NodeState Evaluate()
    {
        nodeState = _enemyAI.CurrentHealth <= _treshold ? NodeState.Success : NodeState.Failure;
        return nodeState;
    }

}
