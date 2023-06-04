using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_BaseLayer : SMB_MainEnemy
{

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        base.OnStateMachineEnter(animator, stateMachinePathHash);
        Debug.Log("enter machine");
        _eRef.enemyBehaviour.isHit = false;
    }
}
