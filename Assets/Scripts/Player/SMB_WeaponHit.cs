using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;

public class SMB_WeaponHit : SMB_MainParent
{
    float _normTime;
    bool _usesAnimationEvent;

    protected override void Init()
    {
        base.Init();
        _usesAnimationEvent = _currentWeapon.animEventForShooting;
    }

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _normTime = 0f;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_usesAnimationEvent) return;

        if(_normTime < stateInfo.normalizedTime)
        {
            //Debug.Log($"normTime is {_normTime}");
            //Debug.Log($"stateInfo.normalizedTime is {stateInfo.normalizedTime}");
            _offense.WeaponDischarge(0);
            _normTime++;
        }
    }

}
