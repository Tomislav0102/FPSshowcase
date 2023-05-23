using System;
using FirstCollection;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;

public class EnemyBehaviour : MonoBehaviour, IFactionTarget, IMaterial
{
    [SerializeField] TextMeshPro displayState;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] EnemyAnim enemyAnim;
    [SerializeField] Transform movePoint;
    [SerializeField] Collider[] colliders;
    [SerializeField] Transform lastKnowLocationTransform;
    
    //INTERFACES
    #region
    public Transform MyTransform { get => agent.transform; set { } }
    public Transform MyHead { get ; set ; }
    [field: SerializeField] public Faction Fact { get; set; }
    [field: SerializeField] public MatType MaterialType { get; set; }
    #endregion

    [BoxGroup("Field of view")]
    [HideLabel]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] FieldOvView fov;

    [SerializeField] EnemyState startingState;
    EnemyState _nextState;
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
                case EnemyState.MoveToPoint:
                    if (_nextState == EnemyState.Search || _nextState == EnemyState.Attack)
                    {
                        _moveType = MoveType.Run;
                    }
                    else
                    {
                        _moveType = MoveType.Walk;
                    }
                    break;
            }
            enemyAnim.SetSpeed(_moveType);
            displayState.text = value.ToString();
            displayState.color = GameManager.gm.gizmoColorsByState[(int)value];
        }
    }
    EnemyState _enState;
    Animator _anim;
    Transform _animTr;
    Camera _cam;
    Vector3 _startPos;
    Quaternion _startRot;
    bool _canUpdateFOV;
    MoveType _moveType;

    //BEHAVIOUR SPECIFIC
    #region
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
    float _timerAttack;
    GameObject muzzle;
    AttackClass _attackClass;

    //search
    float _timerSearch;
    [HideInInspector] public bool hasSearched;
    [HideInInspector] public Vector3 lastKnowLocation;
    #endregion

    bool ReadyToMove()
    {
        return !agent.pathPending && agent.remainingDistance <= 0.5f; 
    }

    [Title("Debug only")]
    public bool consoleDisplay;
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedCurrent;
    public float remainDistance;

    //AWAKE -> UPDATE
    #region
    private void Awake()
    {
        _cam = GameManager.gm.mainCam;
        fov.Init(this, agent.transform, consoleDisplay);
        enemyAnim.Init(this,out _anim, out _animTr, out weaponUsed, out muzzle);

        _attackClass = new AttackClass(colliders, this);
        _attackClass.bulletSpawnPosition = muzzle.transform;


    }
    private void Start()
    {
        _startPos = agent.transform.position;
        _startRot = agent.transform.rotation;
        EnState = startingState;
        _attackRangeSquared = Mathf.Pow(weaponUsed.range, 2f);

        //idle
        _startRotY = agent.transform.eulerAngles.y;

        //patrol
         _wayPoints = HelperScript.AllChildren((wpParent == null || wpParent.childCount == 0) ? GameManager.gm.wayPointParent : wpParent);


        InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0f, 1f), 0.2f);
    }

    private void Update()
    {
       // if (Input.GetMouseButtonDown(1)) _anim.SetTrigger("hit");
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
            case EnemyState.MoveToPoint:
                MoveTo();
                break;
        }
        
        speedCurrent = _anim.velocity.magnitude;
        if (speedCurrent < 0.05f) speedCurrent = 0f;
        agent.speed = speedCurrent;
        _animTr.SetPositionAndRotation(agent.transform.position - 0.06152725f * Vector3.up, agent.transform.rotation);
    }
    void FixedUpdate()
    {
        if (EnState == EnemyState.Attack) return;
        if (_canUpdateFOV)
        {
            AttackTarget = fov.FindFovTargets();
            _canUpdateFOV = false;
        }
    }
    void Debugs()
    {
        displayState.transform.LookAt(_cam.transform.position);
        displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        //   lastKnowLocationTransform.position = lastKnowLocation;
        lastKnowLocationTransform.position = agent.nextPosition;
        haspath = agent.hasPath;
        pathStatus = agent.pathStatus;
        remainDistance = agent.remainingDistance;
    }
    void CanUpdateFOVMethod()
    {
        _canUpdateFOV = true;
    }
    void GoToTarget()
    {
        if (ReadyToMove())
        {
            enemyAnim.Attack(false);
            agent.SetDestination(movePoint.position);
           // print("moving");
        }
    }
    #endregion

    //BEHAVIOURS
    #region
    void IdleBehaviour(bool lookAround)
    {
        if (!lookAround) return;

        _timerIdleRotate -= Time.deltaTime;
        if (_timerIdleRotate <= 0f)
        {
            _timerIdleRotate = Random.Range(3f, 10f);
            _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-idleLookAngle * 0.5f, idleLookAngle * 0.5f), Vector3.up);
        }
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, _targetRot, 10 * Time.deltaTime);
    }
    void PatrolBehaviour()
    {
        if (ReadyToMove())
        {
            movePoint.position = _wayPoints[_counterWayPoints].position;
            _counterWayPoints = (1 + _counterWayPoints) % _wayPoints.Length;
            GoToTarget();
        } 
    }
    void RoamBehaviour(Vector3 center)
    {
        if (ReadyToMove())
        {
            movePoint.position = GetRdnPos(center);
            GoToTarget();
        }
    }
    void AttackBehaviour()
    {
        if (AttackTarget == null)
        {
            movePoint.SetPositionAndRotation(_startPos, _startRot);
            _nextState = startingState;
            EnState = EnemyState.MoveToPoint;
            GoToTarget();
            return;
        }

        movePoint.position = lastKnowLocation = AttackTarget.MyTransform.position;

        if (!fov.TargetStillVisible(AttackTarget))
        {
            _nextState = EnemyState.Search;
            EnState = EnemyState.MoveToPoint;
            return;
        }

        Vector3 dir = lastKnowLocation - agent.transform.position;
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (agent.hasPath) agent.ResetPath();
            _timerAttack = 0f;
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation,
                Quaternion.LookRotation(dir), 2f * Time.deltaTime);
            enemyAnim.SetAim(AttackTarget.MyHead.position); //for some reason it doesn't work here, but works in AttackBehaviour()
            enemyAnim.Attack(true);

        }
        else
        {
            _timerAttack += Time.deltaTime;
            if (_timerAttack > 1f)
            {
                _timerAttack = 0f;
                GoToTarget();
            }
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

    void SearchBeahaviour()
    {
        RoamBehaviour(lastKnowLocation);
        _timerSearch += Time.deltaTime;
        if (_timerSearch > 5f)
        {
            _timerSearch = 0f;
            movePoint.SetPositionAndRotation(_startPos, _startRot);
            _nextState = startingState;
            EnState = EnemyState.MoveToPoint;
        }
    }
    void MoveTo()
    {
        if (agent.hasPath && agent.remainingDistance < 0.1f)
        {
            agent.transform.rotation = _startRot;
            EnState = _nextState;
            return;
        }
        GoToTarget();
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
    float _sightRange, _hearingRange;
    [SerializeField] float sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    [SerializeField] LayerMask layerFOV;
    Collider[] _colls = new Collider[30];
    RaycastHit[] _multipleHits = new RaycastHit[1];

    bool _conseoleDisplay;
    public void Init(EnemyBehaviour enemyBehaviour, Transform agentTransform, bool consoleDis)
    {
        _enemyBehaviour = enemyBehaviour;
        _parentFovTraget = enemyBehaviour.GetComponent<IFactionTarget>();
        _myTransform = agentTransform;
        _sightRange = sightSphere.localScale.x * 0.5f;
        _hearingRange = hearSphere.localScale.x * 0.5f;
        sightSphere.gameObject.SetActive(false);
        hearSphere.gameObject.SetActive(false);

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
    public bool TargetStillVisible(IFactionTarget target)
    {
        _ray.direction = (target.MyTransform.position - _myTransform.position).normalized;
        for (int i = 0; i < 2; i++)
        {
            _ray.origin = _myTransform.position + (i + 0.7f) * Vector3.up;

            if (Physics.Raycast(_ray, out _hit, EffectiveRange(target.MyTransform.position), ~0, QueryTriggerInteraction.Ignore))
            {
                if (_hit.collider == target.MyTransform.GetComponent<Collider>()) return true;
            }
        }
        return false;
    }
    public IFactionTarget FindFovTargets()
    {
        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, layerFOV, QueryTriggerInteraction.Ignore);

        if (num == 0) return null;

        for (int i = 0; i < num; i++)
        {
            IFactionTarget ifact = _colls[i].GetComponent<IFactionTarget>();

            if (ifact == null || ifact == _parentFovTraget) continue;

            if (!TargetStillVisible(ifact)) continue;

            switch (ifact.Fact)
            {
                case Faction.Player:
                    _enemyBehaviour.EnState = EnemyState.Attack;
                    return ifact;

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
                                _enemyBehaviour.lastKnowLocation = en.lastKnowLocation;
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
