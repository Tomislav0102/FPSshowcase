using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_FirstAid : SMB_MainPlayer
{
    bool _oneHit;
    GameObject _myGameObject;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        _oneHit = false;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        _offense.ReadyWeapon();

        if(_myGameObject == null) _myGameObject = animator.gameObject;
        _myGameObject.SetActive(false);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if(stateInfo.normalizedTime > 0.58f && !_oneHit)
        {
            _oneHit = true;
            HealthPlayer.HealSyringe?.Invoke(25);
        }
    }
}
