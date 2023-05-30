using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_RandomPar_Machine : StateMachineBehaviour
{
    [SerializeField] string parameterName;
    [SerializeField] int numOfVariations = 4;
    [Header("Testing")]
    [SerializeField] int testVariation;
    
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        animator.SetInteger(parameterName, Random.Range(0, numOfVariations));
       // animator.SetInteger(parameterName, testVariation);
    }
}
