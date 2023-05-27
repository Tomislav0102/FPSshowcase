using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_IdleToIdleOther : SMB_MainPlayer
{
    float _timer;
    float _maxTimer;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _timer = 0f;
        _maxTimer = Random.Range(10f, 300f);

      //  _maxTimer = 3f; //testing
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _timer += Time.deltaTime;
        if(_timer > _maxTimer)
        {
            _timer = 0f;
            animator.SetTrigger("toIdleOther");
        }
    }
}
