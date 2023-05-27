using Knife.RealBlood.SimpleController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SMB_MainEnemy : StateMachineBehaviour
{
    internal GameManager _gm;
    internal EnemyBehaviour _enemyBehaviour;
    internal EnemyAnim _enemyAnim;
    private void Awake()
    {
        Init();
    }
    protected virtual void Init()
    {
        _gm = GameManager.gm;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_enemyBehaviour == null)
        {
            _enemyBehaviour = animator.transform.parent.GetChild(0).GetComponent<EnemyBehaviour>();
        }
        if (_enemyAnim == null)
        {
            _enemyAnim = _enemyBehaviour.enemyAnim;
        }

    }
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (_enemyBehaviour == null)
        {
            _enemyBehaviour = animator.transform.parent.GetChild(0).GetComponent<EnemyBehaviour>();
        }
        if (_enemyAnim == null)
        {
            _enemyAnim = _enemyBehaviour.enemyAnim;
        }

    }
}
