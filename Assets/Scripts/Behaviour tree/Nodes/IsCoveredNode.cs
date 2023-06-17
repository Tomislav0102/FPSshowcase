using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsCoveredNode : Node
{
    Transform _target, _origin;
    RaycastHit _hit;
    LayerMask _mask = (1 << 0) + (1 << 16);

    public IsCoveredNode(Transform target, Transform origin)
    {
        _target = target;
        _origin = origin;
    }

    public override NodeState Evaluate()
    {
        if (Physics.Raycast(_origin.position + Vector3.up, _target.position - _origin.position, out _hit, _mask))
        {
            if(_hit.transform != _target.transform)
            {
                nodeState = NodeState.Success;
            }
            else nodeState = NodeState.Failure;
        }
        else nodeState = NodeState.Failure;

        return nodeState;
    }
}
