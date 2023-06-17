using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Node 
{
    public NodeState nodeState;

    public abstract NodeState Evaluate();
}

public enum NodeState
{
    Success,
    Failure,
    Running
}
