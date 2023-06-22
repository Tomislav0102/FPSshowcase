using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class BaseState
{
    internal readonly GameManager gm;
    internal readonly EnemyRef eRef;
    internal readonly EnemyBehaviour enBeh;
    internal float timer;
    internal Vector3 startPos;
    internal Quaternion startRot;
    public int counterForColors;
    protected BaseState(EnemyRef eref, List<BaseState> allStates)
    {
        gm = GameManager.Instance;
        eRef = eref;
        enBeh = eRef.enemyBehaviour;
        startPos = enBeh.transform.position;
        startRot = enBeh.transform.rotation;
        allStates.Add(this);
        counterForColors = allStates.Count - 1;
    }
    public virtual void OnEnter()
    {
        eRef.agent.ResetPath();
        enBeh.Attack_Animation(false);
    }
    public virtual void OnExit()
    {
        timer = 0;
    }
    public virtual void UpdateLoop()
    {

    }
    public virtual void PhysicsUpdateLoop()
    {

    }
}
public class IdleState : BaseState
{
    float _idleLookAngle;
    Quaternion _targetRot;
    float _startRotY;
    bool _idleOnMove;
    float _maxTimer;
    bool _lookAround;

    public IdleState(EnemyRef eref, List<BaseState> allStates, float lookAngle, bool lookAround) : base(eref, allStates)
    {
        _startRotY = eRef.agentTr.eulerAngles.y;
        _maxTimer = Random.Range(3f, 10f);
        _idleLookAngle = lookAngle;
        _lookAround = lookAround;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Stationary);
        enBeh.attackTarget = null;
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (Vector3.SqrMagnitude(enBeh.movePoint.position - eRef.agentTr.position) < 0.3f)
        {
            enBeh.SetSpeed_Animation(MoveType.Stationary);

            if (_idleOnMove)
            {
                _idleOnMove = false;
                if (eRef.agent.hasPath) eRef.agent.ResetPath();
                eRef.agentTr.rotation = startRot;
            }

            if (!_lookAround) return;

            timer += Time.deltaTime;
            if (timer > _maxTimer)
            {
                timer = 0f;
                _maxTimer = Random.Range(3f, 10f);
                _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-_idleLookAngle * 0.5f, _idleLookAngle * 0.5f), Vector3.up);
            }
            eRef.agentTr.rotation = Quaternion.Slerp(eRef.agentTr.rotation, _targetRot, 10 * Time.deltaTime);
            return;
        }
        enBeh.movePoint.position = startPos;

        if (!eRef.agent.hasPath)
        {
            _idleOnMove = true;
            eRef.enemyBehaviour.TrackMovingTarget();
        }
    }

}
public class PatrolState : BaseState
{
    Transform[] _wayPoints;
    int _counterWayPoints;

    public PatrolState(EnemyRef eref, List<BaseState> allStates, Transform wPar, Transform[] waypoints) : base(eref, allStates)
    {
        if (wPar == null) _wayPoints = waypoints;
        else
        {
            _wayPoints = new Transform[wPar.childCount];
            for (int i = 0; i < _wayPoints.Length; i++)
            {
                _wayPoints[i] = wPar.GetChild(i);
            }
        }
    }
    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Walk);
        enBeh.attackTarget = null;
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
        enBeh.movePoint.position = _wayPoints[_counterWayPoints].position;
        _counterWayPoints = (1 + _counterWayPoints) % _wayPoints.Length;
        eRef.agent.SetDestination(enBeh.movePoint.position);
    }
}
public class SuperWanderState : BaseState
{
    readonly float _roamRadius;
    internal Vector3 center;

    protected SuperWanderState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates)
    {
        _roamRadius = roamRadius;
    }
    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (eRef.ReadyToMove())
        {
            enBeh.movePoint.position = EnemyRef.GetRdnPos(center, _roamRadius);
            eRef.agent.SetDestination(enBeh.movePoint.position);
        }

    }
}
public class RoamState : SuperWanderState
{
    public RoamState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
        center = startPos;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Walk);
        enBeh.attackTarget = null;
    }
}
public class SearchState : SuperWanderState
{
    public SearchState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        center = enBeh.movePoint.position;
        enBeh.SetSpeed_Animation(MoveType.Run);
        enBeh.hasSearched = true;
    }
    public override void UpdateLoop()
    {
        base.UpdateLoop();

        timer += Time.deltaTime;
        if (timer > 3f)
        {
            timer = 0f;
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            center = enBeh.movePoint.position;
            enBeh.ChangeState(enBeh.moveToPointState);
        }
    }
    public override void OnExit()
    {
        base.OnExit();
        CoolDownHasSearched();
    }

    async void CoolDownHasSearched()
    {
        await Task.Delay(5000);
        enBeh.hasSearched = false;
    }
}
public class AttackState : BaseState
{
    float _attackRangeSquared;

    public AttackState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }

    public void InitAttackRange(float sightRange)
    {
        float range = Mathf.Min(enBeh.weaponUsed.range, sightRange);
        _attackRangeSquared = Mathf.Pow(range, 2f);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Run);
        enBeh.hasSearched = false;
        enBeh.detectObject = null;
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (enBeh.attackTarget == null)
        {
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            enBeh.ChangeToStartingState();
            return;
        }


        if (!eRef.fov.TargetVisible(enBeh.attackTarget.MyTransform, enBeh.attackTarget.MyCollider, gm.layFOV_Ray))
        {
            enBeh.Attack_Animation(false);
            enBeh.TrackMovingTarget();
            if (eRef.agent.remainingDistance < 1f)
            {
                enBeh.ChangeState(enBeh.searchState);
            }
            return;
        }
        enBeh.movePoint.position = enBeh.attackTarget.MyTransform.position;


        Vector3 dir = enBeh.movePoint.position - eRef.agentTr.position;
        eRef.agentTr.rotation = Quaternion.Slerp(eRef.agentTr.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (eRef.agent.hasPath) eRef.agent.ResetPath();
            enBeh.SetAim_Animation();
            enBeh.Attack_Animation(true);
        }
        else
        {
            enBeh.Attack_Animation(false);
            enBeh.TrackMovingTarget();

        }

    }
}
public class FollowState : BaseState
{
    public FollowState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Walk);
        eRef.agent.stoppingDistance = 3f;
    }

    public override void UpdateLoop()
    {
        enBeh.attackTarget = null;
        Vector3 pos = gm.plFaction.MyTransform.position;

        MoveType mt = MoveType.Stationary;
        float dist = Vector3.SqrMagnitude(enBeh.MyTransform.position - pos);
        if (dist > 50f)
        {
            mt = MoveType.Run;
            enBeh.movePoint.position = pos;
        }
        else
        {
            mt = MoveType.Walk;
            if (eRef.agent.remainingDistance < eRef.agent.stoppingDistance)
            {
                Vector3 myPos = enBeh.MyTransform.position;
                enBeh.movePoint.position = myPos + 10f * (myPos - pos).normalized;
            }
            else
            {
                enBeh.movePoint.position = pos;
            }
        }

        enBeh.SetSpeed_Animation(mt);
        enBeh.TrackMovingTarget();
    }

    public override void OnExit()
    {
        base.OnExit();
        eRef.agent.stoppingDistance = 0f;
    }
}
public class ImmobileState : BaseState
{
    public ImmobileState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.ResetAllWeights();
    }

    public override void UpdateLoop()
    {
        enBeh.ragToAnimTransition.RagdollStandingUp();
        eRef.agentTr.SetPositionAndRotation(new Vector3(eRef.animTr.position.x, eRef.agentTr.position.y, eRef.animTr.position.z), eRef.animTr.rotation);
    }

}
public class FleeState : BaseState
{
    public FleeState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Run);
        if (enBeh.attackTarget != null)
        {
            Vector3 dir = (enBeh.MyTransform.position - enBeh.attackTarget.MyTransform.position).normalized;
            enBeh.movePoint.position = EnemyRef.GetRdnPos(enBeh.MyTransform.position + 50f * dir, 0f);
        }
        else enBeh.ChangeToStartingState();

    }

    public override void UpdateLoop()
    {
        enBeh.TrackMovingTarget();
        timer += Time.deltaTime;
        if (timer > 10f)
        {
            timer = 0f;
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            enBeh.ChangeToStartingState();
        }

    }
}

public class MoveToPointState : BaseState
{
    public MoveToPointState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Walk);
        eRef.agent.SetDestination(enBeh.movePoint.position);
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (eRef.agent.remainingDistance < 0.1f) enBeh.ChangeToStartingState();
    }

    public override void OnExit()
    {
        base.OnExit();
        eRef.agentTr.rotation = startRot;
    }
}
