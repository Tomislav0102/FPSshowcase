using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_Test : StateMachineBehaviour
{

    MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1f);
    Transform tar;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(tar == null) tar = animator.GetComponent<TestScript>().targetMatchL;
        animator.MatchTarget(tar.position, tar.rotation, AvatarTarget.Root, weightMask, 0f, 1f);

    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Debug.Log("exit state");
    }

    //public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //   // Debug.Log("Enter statemachine");
    //}
    //public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //   // Debug.Log("Exit statemachine");
    //}
}
