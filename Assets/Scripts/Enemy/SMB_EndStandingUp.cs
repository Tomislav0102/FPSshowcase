using UnityEngine;

public class SMB_EndStandingUp : SMB_MainEnemy
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        eRef.enemyBehaviour.ChangeState(eRef.enemyBehaviour.attackState);
    }
}
