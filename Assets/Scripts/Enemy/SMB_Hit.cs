using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_Hit : SMB_MainEnemy
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        _enemyAnim.ikActive = false;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        _enemyAnim.ikActive = true;
    }


    //public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    base.OnStateMachineEnter(animator, stateMachinePathHash);
    //    _enemyBehaviour.hitAnimation = true;
    //}

    //public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    base.OnStateMachineExit(animator, stateMachinePathHash);
    //    _enemyBehaviour.hitAnimation = false;
    //}
}
