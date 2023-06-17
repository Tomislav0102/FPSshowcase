using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inverter : Node
{
    protected Node _node;
    public Inverter(Node node)
    {
        _node = node;
    }
    public override NodeState Evaluate()
    {
        switch (_node.Evaluate())
        {
            case NodeState.Success:
                nodeState = NodeState.Failure;
                break;
            case NodeState.Failure:
                nodeState = NodeState.Success;
                break;
            case NodeState.Running:
                nodeState = NodeState.Running;
                break;
        }

        return nodeState;
    }
}
