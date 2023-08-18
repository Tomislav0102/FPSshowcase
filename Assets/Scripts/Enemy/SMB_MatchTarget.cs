using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_MatchTarget : SMB_MainEnemy
{
    public Transform _target;
    Transform Tar()
    {
        if(_target == null) return eRef.enemyBehaviour.coverObject.transform;
        return _target.transform;
    }
    MatchTargetWeightMask _weightMask = new MatchTargetWeightMask(Vector3.one, 1f);

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }
    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateMove(animator, stateInfo, layerIndex);
        if (!animator.IsInTransition(0))
        {
            //Vector3 pos = new Vector3(Tar().position.x, animator.transform.position.y, Tar().position.z);
            //Quaternion rot = Quaternion.LookRotation(-Tar().forward, Vector3.up);

            //animator.MatchTarget(pos, rot, AvatarTarget.Root, _weightMask, 0.25f, 0.5f);
            //animator.ApplyBuiltinRootMotion();
        }
    }
}
