using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_BaseLayer : SMB_MainEnemy
{

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        _eRef.enemyAnim.isHit = false;
    }
}
