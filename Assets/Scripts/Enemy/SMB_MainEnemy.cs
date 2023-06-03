using Knife.RealBlood.SimpleController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SMB_MainEnemy : StateMachineBehaviour
{
    internal GameManager _gm;
    internal EnemyRef _eRef;
    //internal EnemyBehaviour _enemyBehaviour;
    //internal EnemyAnim _enemyAnim;
    private void Awake()
    {
        Init();
    }
    protected virtual void Init()
    {
        _gm = GameManager.Instance;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_eRef == null)
        {
            _eRef = animator.transform.parent.GetComponent<EnemyRef>();
        }

    }
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (_eRef == null)
        {
            _eRef = animator.transform.parent.GetComponent<EnemyRef>();
        }

    }
}
