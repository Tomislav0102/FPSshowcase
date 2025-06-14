using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_Hit : SMB_MainEnemy
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        eRef.enemyBehaviour.isHit = true;
    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        eRef.enemyBehaviour.isHit = false;
    }

}
