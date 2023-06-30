using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_ToggleIK : SMB_MainEnemy
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        eRef.enemyBehaviour.ThrowMethod(true);
    }
    //public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    base.OnStateUpdate(animator, stateInfo, layerIndex);
    //    eRef.enemyBehaviour.ThrowMethod(true);
    //}
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        eRef.enemyBehaviour.ThrowMethod(false);
    }
}
