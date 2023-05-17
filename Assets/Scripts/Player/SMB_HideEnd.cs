using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_HideEnd : SMB_MainParent
{
    bool _oneHit;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _oneHit = false;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      //  Debug.Log(stateInfo.normalizedTime);
        if (!_oneHit && stateInfo.normalizedTime > 0.8f)
        {
            _oneHit = true;
            _offense.ReadyWeapon();
        }
    }
}
