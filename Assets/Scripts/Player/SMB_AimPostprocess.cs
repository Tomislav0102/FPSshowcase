using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_AimPostprocess : SMB_MainPlayer
{
    [SerializeField] bool activateAimPostprocess;
    bool _hasScope;

    protected override void Init()
    {
        base.Init();
        _hasScope = _currentWeapon.weaponDetail.scope;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      //  GlobalEventManager.PlayerAiming(activateAimPostprocess);
        if (!_hasScope) return;
        _gm.postProcess.ShowDepth(activateAimPostprocess);
    }
}
