using System;
using FirstCollection;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyBehaviour : MonoBehaviour, IFactionTarget, IMaterial, IActivation
{
    GameManager _gm;
    [SerializeField] TextMeshPro displayState;
    [SerializeField] NavMeshAgent agent;
    public EnemyAnim enemyAnim;
    public Transform movePoint;
    [SerializeField] Collider[] colliders;
    Animator _anim;
    Transform _animTr;
    Camera _cam;
    Vector3 _startPos;
    Quaternion _startRot;
    bool _canUpdateFOV, _canUpdateDestination;
    MoveType _moveType;

    #region//INTERFACES
    [field: SerializeField] public Transform MyTransform { get; set; }
    public Transform MyHead { get ; set ; }
    [field: SerializeField] public Faction Fact { get; set; }
    public IFactionTarget Owner { get; set; }
    [field: SerializeField] public MatType MaterialType { get; set; }
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            if (value)
            {
                fov.Init(this, agent.transform, consoleDisplay);
                enemyAnim.Init(this, out _anim, out _animTr, out weaponUsed, out muzzle);

                _attackClass = new AttackClass(colliders, this);
                _attackClass.bulletSpawnPosition = muzzle.transform;
                InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0f, 1f), 0.3f);

            }
            else
            {
                AttackTarget = null;
                fov = null;
                _attackClass = null;
                enemyAnim.Attack(false);
                CancelInvoke();
            }

            agent.enabled = value;
            _anim.gameObject.SetActive(value);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = value;
            }
            displayState.enabled = value;
        }
    }
    bool _isActive;

    #endregion

    [BoxGroup("Field of view")]
    [HideLabel]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] FieldOvView fov;

    [SerializeField] EnemyState startingState;
    //EnemyState _nextState;
    public EnemyState EnState
    {
        get => _enState;
        set
        {
            _timerIdleRotate = _timerSearch = _timerAttack = 0f;
            agent.ResetPath();
            enemyAnim.Attack(false);
            _enState = value;
            _moveType = MoveType.Stationary;
            if (value != EnemyState.Attack) AttackTarget = null;
            switch (value)
            {
                case EnemyState.Idle:
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Patrol:
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Roam:
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Search:
                    hasSearched = true;
                    _moveType = MoveType.Run;
                    break;
                case EnemyState.Attack:
                    hasSearched = false;
                    _timerAttack = Mathf.Infinity;
                    _moveType = MoveType.Run;
                    break;
                case EnemyState.Follow:
                    break;
            }
            enemyAnim.SetSpeed(_moveType);
            displayState.text = value.ToString();
            displayState.color = GameManager.gm.gizmoColorsByState[(int)value];
        }
    }
    EnemyState _enState;


    #region//BEHAVIOUR SPECIFIC
    //idle
    [BoxGroup("Idle")]
    [SerializeField]
    [ShowIf("startingState", EnemyState.Idle)]
    [Range(0f, 360f)]
    float idleLookAngle = 180f;
    Quaternion _targetRot;
    float _timerIdleRotate;
    float _startRotY;

    //patrol
    [BoxGroup("Patrol")]
    [ShowIf("startingState", EnemyState.Patrol)]
    [GUIColor(0f, 1f, 0f, 1f)]
    [SerializeField] Transform wpParent;
    Transform[] _wayPoints;
    int _counterWayPoints;

    //roam
    [BoxGroup("Roam")]
    [ShowIf("startingState", EnemyState.Roam)]
    [GUIColor(1f, 0f, 1f, 1f)]
    [SerializeField] float roamRadius = 10f;
    Vector3 GetRdnPos(Vector3 center)
    {
        Vector3 pos = center + roamRadius * Random.insideUnitSphere;
        if (NavMesh.SamplePosition(pos, out _navHit, 2f* roamRadius, NavMesh.AllAreas))
        {
            return _navHit.position;
        }

        return pos;
    }
    NavMeshHit _navHit;

    //attack
    SoItem weaponUsed;
    public IFactionTarget AttackTarget;
    float _attackRangeSquared;
    float _timerAttack, _timerCheckTargetVisible;
    GameObject muzzle;
    AttackClass _attackClass;

    //search
    float _timerSearch;
    [HideInInspector] public bool hasSearched;
    Vector3 _searchCenter;
    #endregion

    bool ReadyToMove()
    {
        return !agent.pathPending && agent.remainingDistance <= 0.5f; 
    }

    [Title("Debug only")]
    public bool consoleDisplay;
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedAnimRoot;
    public float speedAgent;
    public float remainDistance;
    public string namOfAttacker;


    #region //MAIN
    private void Awake()
    {
        _gm = GameManager.gm;
        _cam = _gm.mainCam;
        IsActive = true;
    }
    private void Start()
    {
        _startPos = agent.transform.position;
        _startRot = agent.transform.rotation;
        _searchCenter = movePoint.position;
        EnState = startingState;
        float range = MathF.Min(weaponUsed.range, fov.sightRange);
        _attackRangeSquared = Mathf.Pow(range, 2f);

        //idle
        _startRotY = agent.transform.eulerAngles.y;

        //patrol
         _wayPoints = HelperScript.AllChildren((wpParent == null || wpParent.childCount == 0) ? GameManager.gm.wayPointParent : wpParent);


    }

    private void Update()
    {
        if (!IsActive) return;
        Debugs();

        switch (EnState)
        {
            case EnemyState.Idle:
                IdleBehaviour(false);
                break;
            case EnemyState.Patrol:
                PatrolBehaviour();
                break;
            case EnemyState.Roam:
                RoamBehaviour(_startPos);
                break;
            case EnemyState.Search:
                SearchBeahaviour();
                break;
            case EnemyState.Attack:
                AttackBehaviour();
                break;
            case EnemyState.Follow:
                break;
        }
        
        speedAnimRoot = _anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        agent.speed = speedAnimRoot;
        _animTr.SetPositionAndRotation(agent.transform.position - 0.06152725f * Vector3.up, agent.transform.rotation);
    }
    void FixedUpdate()
    {
        if (!IsActive || EnState == EnemyState.Attack) return;
        if (_canUpdateFOV)
        {
            AttackTarget = fov.FindFovTargets();
            if (AttackTarget != null) movePoint.position = AttackTarget.MyTransform.position;
            _canUpdateFOV = false;
        }
    }
    void Debugs()
    {
        displayState.transform.LookAt(_cam.transform.position);
        displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        haspath = agent.hasPath;
        pathStatus = agent.pathStatus;
        remainDistance = agent.remainingDistance;
        speedAgent =agent.velocity.magnitude;
        namOfAttacker = AttackTarget == null ? "no target" : AttackTarget.MyTransform.name.ToString();
    }
    void CanUpdateFOVMethod() //optimization method. Detection and navigation don't need updates on every frame.
    {
        _canUpdateFOV = _canUpdateDestination = true;
    }
    void FollowMovingTarget()
    {
        enemyAnim.Attack(false);
        if (_canUpdateDestination)
        {
            agent.SetDestination(movePoint.position);
          //   print("moving");
            _canUpdateDestination = false;
        }
    }
    public void PassFromHealth_Attacked(Transform attackerTr)
    {
        _anim.SetTrigger("hit");
        if (EnState == EnemyState.Attack || AttackTarget != null) return;
        if (attackerTr.TryGetComponent(out IFactionTarget target) && target.Fact != Fact)
        {
            AttackTarget = target;
            EnState = EnemyState.Search;
        }
    }
    #endregion


    #region //BEHAVIOURS
    void IdleBehaviour(bool lookAround)
    {
        if (Vector3.SqrMagnitude(movePoint.position - agent.transform.position) < 1f)
        {
            _moveType = MoveType.Stationary;
            enemyAnim.SetSpeed(_moveType);

            if (!lookAround) return;

            _timerIdleRotate -= Time.deltaTime;
            if (_timerIdleRotate <= 0f)
            {
                _timerIdleRotate = Random.Range(3f, 10f);
                _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-idleLookAngle * 0.5f, idleLookAngle * 0.5f), Vector3.up);
            }
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, _targetRot, 10 * Time.deltaTime);
            return;
        }
        //else agent needs to move to position
        movePoint.position = _startPos;
        if(!agent.hasPath)
        {
            FollowMovingTarget();
        }
    }
    void PatrolBehaviour()
    {
        if (ReadyToMove())
        {
            movePoint.position = _wayPoints[_counterWayPoints].position;
            _counterWayPoints = (1 + _counterWayPoints) % _wayPoints.Length;
            agent.SetDestination(movePoint.position);
        } 
    }
    void RoamBehaviour(Vector3 center)
    {
        if (ReadyToMove())
        {
            movePoint.position = GetRdnPos(center);
            agent.SetDestination(movePoint.position);
        }
    }
    void AttackBehaviour()
    {
        if (AttackTarget == null)
        {
            movePoint.SetPositionAndRotation(_startPos, _startRot);
            EnState = startingState;
            return;
        }


        if (!fov.TargetStillVisible(AttackTarget, _gm.layAllWithoutDetectables))
        {
            enemyAnim.Attack(false);
            FollowMovingTarget();

            if (agent.remainingDistance < 1f)
            {
                SearchStart();
            }
            return;
        }
        movePoint.position = AttackTarget.MyTransform.position;


        Vector3 dir = movePoint.position - agent.transform.position;
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (agent.hasPath) agent.ResetPath();
            _timerAttack = 0f;
            enemyAnim.SetAim(AttackTarget.MyHead.position); //for some reason it only works in AttackBehaviour()
            enemyAnim.Attack(true);
        }
        else
        {
            enemyAnim.Attack(false);
             FollowMovingTarget();

        }
    }

    public void PassFromAE_Attacking()
    {
        if (AttackTarget == null)
        {
            EnState = EnemyState.Search;
            return;
        }

        muzzle.SetActive(false);
        muzzle.SetActive(true);
        _attackClass.Attack(weaponUsed);

    }
    void SearchStart()
    {
        _searchCenter = movePoint.position;
        EnState = EnemyState.Search;
    }
    void SearchBeahaviour()
    {
        RoamBehaviour(_searchCenter);
        _timerSearch += Time.deltaTime;
        if (_timerSearch > 5f)
        {
            _timerSearch = 0f;
            movePoint.SetPositionAndRotation(_startPos, _startRot);
            _searchCenter = movePoint.position;
            EnState = startingState;
        }
    }
    #endregion

}


[Serializable]
public class FieldOvView
{
    EnemyBehaviour _enemyBehaviour;
    IFactionTarget _parentFovTraget;
    Transform _myTransform;
    [SerializeField] Transform sightSphere, hearSphere;
    [HideInInspector] public float sightRange;
    float _hearingRange;
    [SerializeField] float sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    LayerMask _layerFOV;
    Collider[] _colls = new Collider[30];
    RaycastHit[] _multipleHits = new RaycastHit[1];

    bool _conseoleDisplay;
    public void Init(EnemyBehaviour enemyBehaviour, Transform agentTransform, bool consoleDis)
    {
        _enemyBehaviour = enemyBehaviour;
        _parentFovTraget = enemyBehaviour.GetComponent<IFactionTarget>();
        _myTransform = agentTransform;
        sightRange = sightSphere.localScale.x * 0.5f;
        _hearingRange = hearSphere.localScale.x * 0.5f;
        sightSphere.gameObject.SetActive(false);
        hearSphere.gameObject.SetActive(false);
        _layerFOV = GameManager.gm.layFOV;
        _sightAngleTrigonometry = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
        _conseoleDisplay = consoleDis;
    }

    float EffectiveRange(Vector3 targetPos)
    {
        float r = sightAngle;
        if (Vector3.Dot(_myTransform.forward, (targetPos - _myTransform.position).normalized) < _sightAngleTrigonometry)
        {
            r = _hearingRange;
        }
        return r;
    }
    public bool TargetStillVisible(IFactionTarget target, LayerMask layerMask)
    {
        _ray.direction = (target.MyTransform.position - _myTransform.position).normalized;
        for (int i = 0; i < 2; i++)
        {
            _ray.origin = _myTransform.position + (i + 0.7f) * Vector3.up;

            if (Physics.Raycast(_ray, out _hit, EffectiveRange(target.MyTransform.position), layerMask, QueryTriggerInteraction.Ignore))
            {
                if (_hit.collider == target.MyTransform.GetComponent<Collider>()) return true;
            }
        }
      //  Debug.Log($"{target.MyTransform.name} is not visible, but in range");
        return false;
    }
    public IFactionTarget FindFovTargets()
    {
        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, sightRange, _colls, _layerFOV, QueryTriggerInteraction.Ignore);

        if (num == 0) return null;

        for (int i = 0; i < num; i++)
        {
            IFactionTarget ifact = _colls[i].GetComponent<IFactionTarget>();

            if (ifact == null || ifact == _parentFovTraget) continue;
            if (!TargetStillVisible(ifact, ~0)) continue;

            switch (ifact.Fact)
            {
                case Faction.Player:
                    _enemyBehaviour.EnState = EnemyState.Attack;
                    //  return ifact.Owner == null ? ifact : ifact.Owner;
                    return ifact.Owner ?? ifact;

                case Faction.Enemy:
                    if (_hit.collider.TryGetComponent(out EnemyBehaviour en))
                    {
                        switch (en.EnState)
                        {
                            case EnemyState.Attack:
                                _enemyBehaviour.EnState = EnemyState.Attack;
                                return en.AttackTarget;
                            case EnemyState.Search:
                                if (_enemyBehaviour.EnState == EnemyState.Search || _enemyBehaviour.hasSearched)
                                {
                                    return null;
                                }
                                _enemyBehaviour.EnState = EnemyState.Search;
                                _enemyBehaviour.movePoint.position = en.movePoint.position;
                                break;
                        }
                    }
                    break;
            }

        }
        return null;
    }
}

// switch (EnState)
// {
//     case EnemyState.Idle:
//         break;
//     case EnemyState.Patrol:
//         break;
//     case EnemyState.Roam:
//         break;
//     case EnemyState.Search:
//         break;
//     case EnemyState.Attack:
//         break;
//     case EnemyState.Follow:
//         break;
//     case EnemyState.MoveToPoint:
//         break;
// }
