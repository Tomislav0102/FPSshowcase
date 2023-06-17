using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence : Node
{
    protected List<Node> _nodes = new List<Node>();

    public Sequence(List<Node> nodes)
    {
        _nodes = nodes;
    }
    public override NodeState Evaluate()
    {
        bool isAnyNodeRunning = false;

        foreach (Node item in _nodes)
        {
            switch (item.Evaluate())
            {
                case NodeState.Success:
                    break;
                case NodeState.Failure:
                    nodeState = NodeState.Failure;
                    return nodeState;
                case NodeState.Running:
                    isAnyNodeRunning = true;
                    break;
                default:
                    break;
            }
        }
        nodeState = isAnyNodeRunning ? NodeState.Running : NodeState.Success;
        return nodeState;
    }
}
