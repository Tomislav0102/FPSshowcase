using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_GetWeapon : SMB_MainPlayer
{


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _gm.uiManager.crosshairObject.Weapon = _offense.weapons[_offense.Windex];
    }
}
