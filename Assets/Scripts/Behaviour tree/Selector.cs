using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : Node
{
    protected List<Node> _nodes = new List<Node>();

    public Selector(List<Node> nodes)
    {
        _nodes = nodes;
    }
    public override NodeState Evaluate()
    {
        foreach (Node item in _nodes)
        {
            switch (item.Evaluate())
            {
                case NodeState.Success:
                    nodeState = NodeState.Success;
                    return nodeState;
                case NodeState.Failure:
                    break;
                case NodeState.Running:
                    nodeState = NodeState.Running;
                    return nodeState;

            }
        }
        nodeState = NodeState.Failure;
        return nodeState;
    }
}
