using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_Shooting : StateMachineBehaviour //not being used
{
    float _normTime;
    bool _usesAnimationEvent;
    EnemyBehaviour _enemyBehaviour;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_enemyBehaviour == null)
        {
            _enemyBehaviour = animator.GetComponentInParent<EnemyBehaviour>();
        }

        _normTime = 0f;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_usesAnimationEvent) return;

        if (_normTime < stateInfo.normalizedTime)
        {
            _enemyBehaviour.PassFromAE_Attacking();
            _normTime++;
        }
    }

}
