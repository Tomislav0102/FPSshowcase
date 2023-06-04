using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_UpperLayer : SMB_MainEnemy
{
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        base.OnStateMachineEnter(animator, stateMachinePathHash);
        _eRef.enemyBehaviour.isHit = false;

    }
}
