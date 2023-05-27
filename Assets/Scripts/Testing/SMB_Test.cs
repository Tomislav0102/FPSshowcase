using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_Test : StateMachineBehaviour
{


    //public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    Debug.Log("enter state");
    //}
    //public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    Debug.Log("exit state");
    //}

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        Debug.Log("Enter statemachine");
    }
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        Debug.Log("Exit statemachine");
    }
}
