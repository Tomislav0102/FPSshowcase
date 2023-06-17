using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeNode : Node
{
    float _range;
    Transform _target;
    Transform _origin;

    public RangeNode(float range, Transform target, Transform origin)
    {
        _range = range;
        _target = target;
        _origin = origin;
    }

    public override NodeState Evaluate()
    {
        nodeState = Vector3.Distance(_target.position, _origin.position) <= _range ? NodeState.Success : NodeState.Failure;
        return nodeState;
    }
}
