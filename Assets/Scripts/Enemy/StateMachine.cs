using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;


public class StateMachine
{
    TextMeshPro _display;

    List<BaseState> _allStates = new List<BaseState>();
    public BaseState currentState;
    BaseState _startState;
    public IdleState _idleState;
    PatrolState _patrolState;
    RoamState _roamState;
    public SearchState searchState;
    public ScanState scanState;
    public AttackState attackState;
    FollowState _followState;
    public ImmobileState immobileState;
    public FleeState _fleeState;
    public MoveToPointState moveToPointState;

    public StateMachine(TextMeshPro display, EnemyRef eref, Transform patrolparent, Transform[] patrolwaypoints, float roamradius, int indexOfStartingState, int grandes)
    {
        _display = display;

        _idleState = new IdleState(eref, _allStates, roamradius);
        _patrolState = new PatrolState(eref, _allStates, patrolparent, patrolwaypoints);
        _roamState = new RoamState(eref, _allStates, roamradius);
        searchState = new SearchState(eref, _allStates, roamradius);
        scanState = new ScanState(eref, _allStates, roamradius);
        attackState = new AttackState(eref, _allStates, grandes);
        _followState = new FollowState(eref, _allStates);
        immobileState = new ImmobileState(eref, _allStates);
        _fleeState = new FleeState(eref, _allStates);
        moveToPointState = new MoveToPointState(eref, _allStates);
        _startState = _allStates[indexOfStartingState];
        ChangeState(_startState);

    }

    public void ChangeToStartingState()
    {
        ChangeState(_startState);
    }
    public void ChangeState(BaseState nextState)
    {
        if (nextState == currentState || nextState == null) return;
        currentState?.OnExit();
        currentState = nextState;
        currentState.OnEnter();
        _display.text = currentState.ToString();
        _display.color = GameManager.Instance.gizmoColorsByState[currentState.counterForColors];

    }

}

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
public class SuperState_Alpha : BaseState //idle, roam, search, scann
{
    readonly float _roamRadius;
    internal Vector3 center;


    //when does the AI stops?
    internal bool isStoped;
    internal bool justIdeling; //idle state has this on true
    internal MoveType moveTypeMobile;
    float _timerStoped;
    float _maxTimerStoped;
    internal float stopInterval = 1f;

    //defines behaviour when stoped.
    float _maxTimerLookAt;
    float RdnRange() => Random.Range(1f, 3f);
    internal float lookAtInterval = 1f;
    internal float rotSpeed;
    float _prevAngle;

    //these two are needed for search state. Actor first goes to targets last know position and then does search behaviour (although he is in search state from the beggining)
    internal bool goTo2; 
    bool _goTo1;

    protected SuperState_Alpha(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates)
    {
        _roamRadius = roamRadius;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        goTo2 = _goTo1 = justIdeling = false;
        _timerStoped = 0f;
        isStoped = false;
        _prevAngle = 0f;
    }
    public override void UpdateLoop()
    {
        base.UpdateLoop();

        if (justIdeling)
        {
            LookAround();
            return;
        }

        if (eRef.ReadyToMove())
        {
            if (isStoped)
            {
                enBeh.SetSpeed_Animation(MoveType.Stationary);
                LookAround();
                _timerStoped += Time.deltaTime;
                if(_timerStoped > _maxTimerStoped)
                {
                    _timerStoped = 0f;
                    _maxTimerStoped = RdnRange() * stopInterval;
                    isStoped = false;
                }

                return;
            }


            _goTo1 = !_goTo1;
            if (!_goTo1) goTo2 = true;


            enBeh.movePoint.position = EnemyRef.GetRdnPos(center, _roamRadius);
            eRef.agent.SetDestination(enBeh.movePoint.position);

            isStoped = !isStoped;
        }
        enBeh.SetSpeed_Animation(moveTypeMobile);
       // enBeh.IdelLookAround_Animation(false, 0f);

    }

    void LookAround()
    {
        timer += Time.deltaTime;
        if (timer > _maxTimerLookAt)
        {
            timer = 0f;
            _maxTimerLookAt = RdnRange() * lookAtInterval;
            _prevAngle = Random.Range(-70f, 70f);
        }
        enBeh.IdelLookAround_Animation(true, _prevAngle, rotSpeed);

    }
    public override void OnExit()
    {
        base.OnExit();
      //  enBeh.IdelLookAround_Animation(false, 0f);
    }
}

public class IdleState : SuperState_Alpha
{

    public IdleState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Stationary);
        enBeh.attackTarget = null;
        justIdeling = true;
        lookAtInterval = 5f;
        rotSpeed = 2f;
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
    }
    public override void OnExit()
    {
        base.OnExit();
    }
}
public class PatrolState : BaseState
{
    readonly Transform[] _wayPoints;
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
public class RoamState : SuperState_Alpha
{
    public RoamState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
        center = startPos;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        moveTypeMobile = MoveType.Walk;
        enBeh.attackTarget = null;
        justIdeling = false;
        lookAtInterval = 3f;
        stopInterval = 4f;
    }
}
public class SearchState : SuperState_Alpha
{
    public SearchState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
        rotSpeed = 4f;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        center = enBeh.movePoint.position;
        moveTypeMobile = MoveType.Run;
        enBeh.hasSearched = true;
        justIdeling = false;
        lookAtInterval = 1f;
        stopInterval = 2f;
    }
    public override void UpdateLoop()
    {
        base.UpdateLoop();

        if (!goTo2) return;

        timer += Time.deltaTime;
        if (timer > 50f)
        {
            timer = 0f;
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            center = enBeh.movePoint.position;
            enBeh.sm.ChangeState(enBeh.sm.moveToPointState);
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
public class ScanState : SuperState_Alpha
{
    SpriteRenderer _visibleSprite;
    Transform _visibleTransform;

    float _awareness;
    const float CONST_TIMETORECONGINZETARGET = 1f;
    const float CONST_AUTOFINISHDISTANCE = 5f;
    float _autoFinishDistanceSquared;
    readonly Color _startCol = new Color(0f, 1f, 0f, 0f);
    readonly Color _endCol = Color.red;

    public ScanState(EnemyRef eref, List<BaseState> allStates, float roamRadius) : base(eref, allStates, roamRadius)
    {
        _visibleSprite = eref.visibilityMark;
        _visibleTransform = _visibleSprite.transform;
        _autoFinishDistanceSquared = CONST_AUTOFINISHDISTANCE * CONST_AUTOFINISHDISTANCE;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        _awareness = 0;
        _visibleSprite.color = _startCol;
        enBeh.SetSpeed_Animation(MoveType.Stationary);
        rotSpeed = 2f;
    }
    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (enBeh.attackTarget == null)
        {
            enBeh.sm.ChangeToStartingState();
            return;
        }

        enBeh.movePoint.position = enBeh.attackTarget.MyTransform.position;
        Vector3 lookVector = enBeh.movePoint.position - enBeh.MyTransform.position;
        float angle = Vector3.SignedAngle(enBeh.MyHead.forward, lookVector.normalized, Vector3.up);
        enBeh.IdelLookAround_Animation(true, angle, rotSpeed);
      //  Debug.Log(angle);


        _awareness += Time.deltaTime / CONST_TIMETORECONGINZETARGET;
        if (Vector3.SqrMagnitude(lookVector) < _autoFinishDistanceSquared) _awareness = Mathf.Infinity;
        _visibleSprite.color = Color.Lerp(_startCol, _endCol, _awareness);
        _visibleTransform.LookAt(gm.camTr.position);
        if (_awareness >= 1f)
        {
            enBeh.sm.ChangeState(enBeh.sm.attackState);
        }
    }
    public override void OnExit()
    {
        base.OnExit();
        _visibleSprite.color = Color.clear;
       // enBeh.IdelLookAround_Animation(false, 0);
    }
}
public class AttackState : BaseState
{
    float _attackRangeSquared;
    public bool isThrowing; //start and finish of throw animation
    int _countGreandes = 0;
    LayerMask _layGrenadeThrow; //detecting your own to avoid friendly fire

    public AttackState(EnemyRef eref, List<BaseState> allStates, int countGreandes) : base(eref, allStates)
    {
        switch (eref.myFactionInterface.Fact)
        {
            case Faction.Enemy:
                _layGrenadeThrow = 1 << 24;
                break;
            case Faction.Ally:
                _layGrenadeThrow = 1 << 3;
                break;
        }
        _countGreandes = countGreandes;
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
        enBeh.IdelLookAround_Animation(false, 0f, 0f);
    }

    public override void UpdateLoop()
    {
        base.UpdateLoop();
        if (isThrowing)
        {
            return;
        }
        if (enBeh.attackTarget == null)
        {
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            enBeh.sm.ChangeToStartingState();
            return;
        }


        if (!eRef.fov.TargetVisible(enBeh.attackTarget.MyTransform, enBeh.attackTarget.MyCollider, gm.layFOV_Ray))
        {
            enBeh.Attack_Animation(false);
            enBeh.TrackMovingTarget();
           // if (CanThrowGreande()) eRef.anim.SetTrigger("throw");
            if (eRef.agent.remainingDistance < 1f)
            {
                enBeh.sm.ChangeState(enBeh.sm.searchState);
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
    public override void OnExit()
    {
        base.OnExit();
        isThrowing = false;
    }
    bool CanThrowGreande()
    {
        if(_countGreandes == 0) return false;
        else _countGreandes--;

        Collider[] coll = Physics.OverlapSphere(enBeh.movePoint.position, 2f, _layGrenadeThrow);
        if (coll.Length > 0) return false;
        return true;
    }
}
public class FollowState : BaseState
{
    float _stoppingDistanceSquared;
    Vector2 _deadZone;
    public FollowState(EnemyRef eref, List<BaseState> allStates) : base(eref, allStates)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enBeh.SetSpeed_Animation(MoveType.Walk);
        eRef.agent.stoppingDistance = 3f;
        _stoppingDistanceSquared = Mathf.Pow(eRef.agent.stoppingDistance, 2);
        _deadZone = new Vector2(_stoppingDistanceSquared - 1f, _stoppingDistanceSquared + 1f);
    }

    public override void UpdateLoop()
    {
        enBeh.attackTarget = null;
        Vector3 pos = gm.plFaction.MyTransform.position;

        MoveType mt = MoveType.Stationary;
        float dist = Vector3.SqrMagnitude(enBeh.MyTransform.position - pos);
        enBeh.movePoint.position = pos;
        if (dist > 50f)
        {
            mt = MoveType.Run;
        }
        else if (dist >= _deadZone.y)
        {
            mt = MoveType.Walk;
        }
        else if (dist > _deadZone.x && dist < _deadZone.y)
        {
            mt = MoveType.Stationary;
        }
        else
        {
            mt = MoveType.Run;
            Vector3 myPos = enBeh.MyTransform.position;
            enBeh.movePoint.position = myPos + 10f * (myPos - pos).normalized;
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
        enBeh.ResetHandsWeights();
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
        if (enBeh.attackTarget != null) FindPlaceToRunTo(false);
        else enBeh.sm.ChangeToStartingState();

    }

    void FindPlaceToRunTo(bool rdnPoint)
    {
        Vector3 dir = (enBeh.MyTransform.position - enBeh.attackTarget.MyTransform.position).normalized;
        dir.y = enBeh.MyTransform.position.y;
        if (rdnPoint)
        {
            dir.x = Random.value;
            dir.z = Random.value;
        }
        enBeh.movePoint.position = EnemyRef.GetRdnPos(enBeh.MyTransform.position + 50f * dir, 0f);
    }

    public override void UpdateLoop()
    {
        enBeh.TrackMovingTarget();
        if (!eRef.agent.hasPath) FindPlaceToRunTo(true);
        timer += Time.deltaTime;
        if (timer > 10f)
        {
            timer = 0f;
            enBeh.movePoint.SetPositionAndRotation(startPos, startRot);
            enBeh.sm.ChangeState(enBeh.sm.moveToPointState);
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
        enBeh.TrackMovingTarget();
        if (eRef.agent.remainingDistance < 0.1f) enBeh.sm.ChangeToStartingState();
    }

}











//if (Vector3.SqrMagnitude(enBeh.movePoint.position - eRef.agentTr.position) < 0.3f)
//{
//    enBeh.SetSpeed_Animation(MoveType.Stationary);

//    if (_idleOnMove)
//    {
//        _idleOnMove = false;
//        if (eRef.agent.hasPath) eRef.agent.ResetPath();
//        eRef.agentTr.rotation = startRot;
//    }

//    if (!_lookAround) return;

//    timer += Time.deltaTime;
//    if (timer > _maxTimer)
//    {
//        timer = 0f;
//        _maxTimer = Random.Range(3f, 10f);
//        _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-_idleLookAngle * 0.5f, _idleLookAngle * 0.5f), Vector3.up);
//    }
//    eRef.agentTr.rotation = Quaternion.Slerp(eRef.agentTr.rotation, _targetRot, 10 * Time.deltaTime);
//    return;
//}
//enBeh.movePoint.position = startPos;

//if (!eRef.agent.hasPath)
//{
//    _idleOnMove = true;
//    eRef.enemyBehaviour.TrackMovingTarget();
//}
