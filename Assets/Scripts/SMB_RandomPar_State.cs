using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_RandomPar_State : StateMachineBehaviour
{
    [SerializeField] string parameterName;
    [SerializeField] int numOfVariations = 4;
    [Header("Testing")]
    [SerializeField] int testVariation;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(parameterName, Random.Range(0, numOfVariations));
        if (testVariation >= 0 && testVariation < numOfVariations) animator.SetInteger(parameterName, testVariation);
    }
}
