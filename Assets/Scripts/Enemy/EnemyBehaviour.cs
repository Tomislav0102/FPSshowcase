using FirstCollection;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyBehaviour : MonoBehaviour, IFactionTarget
{
    GameManager _gm;
    EnemyRef _eRef;
    [SerializeField] TextMeshPro displayState;
    public Transform movePoint;
    NavMeshAgent _agent;
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

    #endregion


    [SerializeField] EnemyState startingState;
    public EnemyState EnState
    {
        get => _enState;
        set
        {
            _timerIdleRotate = _timerSearch = _timerAttack = 0f;
            _agent.ResetPath();
            _agent.stoppingDistance = 0f;
            _eRef.enemyAnim.Attack(false);
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
                    _agent.stoppingDistance = 3f;
                    break;
            }
            _eRef.enemyAnim.SetSpeed(_moveType);
            displayState.text = value.ToString();
            displayState.color = GameManager.Instance.gizmoColorsByState[(int)value];
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
    bool _idleOnMove;

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
    public IFactionTarget AttackTarget;
    float _attackRangeSquared;
    float _timerAttack, _timerCheckTargetVisible;

    //search
    float _timerSearch;
    [HideInInspector] public bool hasSearched;
    Vector3 _searchCenter;
    #endregion

    bool ReadyToMove()
    {
        return !_agent.pathPending && _agent.remainingDistance <= 0.5f; 
    }

    [Title("Debug only")]
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedAnimRoot;
    public float speedAgent;
    public float remainDistance;
    public string namOfAttacker;


    #region //MAIN
    public void InitAwake(EnemyRef eRef)
    {
        _gm = GameManager.Instance;
        _cam = _gm.mainCam;
        _eRef = eRef;
        _agent = GetComponent<NavMeshAgent>();

        _startPos = _agent.transform.position;
        _startRot = _agent.transform.rotation;
        _searchCenter = movePoint.position;
        EnState = startingState;

        //idle
        _startRotY = _agent.transform.eulerAngles.y;

        //patrol
        _wayPoints = HelperScript.AllChildren((wpParent == null || wpParent.childCount == 0) ? GameManager.Instance.wayPointParent : wpParent);

        //attack
        float range = Mathf.Min(_eRef.enemyAnim.weaponUsed.range, _eRef.fov.sightRange);
        _attackRangeSquared = Mathf.Pow(range, 2f);

    }
    void OnEnable()
    {
        InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0f, 1f), 0.3f);
    }
    void OnDisable()
    {
        AttackTarget = null;
        CancelInvoke();
    }

    private void Update()
    {
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
                FollowBehaviour();
                break;
        }
        
        speedAnimRoot = _eRef.anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        _agent.speed = speedAnimRoot;
        _eRef.animTr.SetPositionAndRotation(_agent.transform.position - 0.06152725f * Vector3.up, _agent.transform.rotation);
    }
    void FixedUpdate()
    {
        if (EnState == EnemyState.Attack) return;
        if (_canUpdateFOV)
        {
            AttackTarget = _eRef.fov.FindFovTargets();
            if (AttackTarget != null) movePoint.position = AttackTarget.MyTransform.position;
            _canUpdateFOV = false;
        }
    }
    void Debugs()
    {
        displayState.transform.LookAt(_cam.transform.position);
        displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        haspath = _agent.hasPath;
        pathStatus = _agent.pathStatus;
        remainDistance = _agent.remainingDistance;
        speedAgent =_agent.velocity.magnitude;
        namOfAttacker = AttackTarget == null ? "no target" : AttackTarget.MyTransform.name.ToString();
    }
    void CanUpdateFOVMethod() //optimization method. Detection and navigation don't need updates on every frame.
    {
        _canUpdateFOV = _canUpdateDestination = true;
    }
    void TrackMovingTarget()
    {
        _eRef.enemyAnim.Attack(false);
        if (_canUpdateDestination)
        {
            _agent.SetDestination(movePoint.position);
          //   print("moving");
            _canUpdateDestination = false;
        }
    }
    public void PassFromHealth_Attacked(Transform attackerTr, bool switchAgro)
    {
        if (EnState == EnemyState.Attack || AttackTarget != null)
        {
            if (switchAgro) NewTarget();
            return;
        }
        NewTarget();

        void NewTarget()
        {
            if (attackerTr.TryGetComponent(out IFactionTarget target) && target.Fact != Fact)
            {
                AttackTarget = target;
                EnState = EnemyState.Search;
            }
        }
    }

    #endregion


    #region //BEHAVIOURS
    void IdleBehaviour(bool lookAround)
    {
        if (Vector3.SqrMagnitude(movePoint.position - _agent.transform.position) < 0.3f)
        {
            _moveType = MoveType.Stationary;
            _eRef.enemyAnim.SetSpeed(_moveType);

            if (_idleOnMove)
            {
                _idleOnMove = false;
                if(_agent.hasPath) _agent.ResetPath();
                _agent.transform.rotation = _startRot;
            }

            if (!lookAround) return;

            _timerIdleRotate -= Time.deltaTime;
            if (_timerIdleRotate <= 0f)
            {
                _timerIdleRotate = Random.Range(3f, 10f);
                _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-idleLookAngle * 0.5f, idleLookAngle * 0.5f), Vector3.up);
            }
            _agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, _targetRot, 10 * Time.deltaTime);
            return;
        }
        movePoint.position = _startPos;

        if(!_agent.hasPath)
        {
            _idleOnMove = true;
            TrackMovingTarget();
        }
    }
    void PatrolBehaviour()
    {
        if (ReadyToMove())
        {
            movePoint.position = _wayPoints[_counterWayPoints].position;
            _counterWayPoints = (1 + _counterWayPoints) % _wayPoints.Length;
            _agent.SetDestination(movePoint.position);
        } 
    }
    void RoamBehaviour(Vector3 center)
    {
        if (ReadyToMove())
        {
            movePoint.position = GetRdnPos(center);
            _agent.SetDestination(movePoint.position);
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


        if (!_eRef.fov.TargetStillVisible(AttackTarget, _gm.layShooting))
        {
            _eRef.enemyAnim.Attack(false);
            TrackMovingTarget();

            if (_agent.remainingDistance < 1f)
            {
                SearchStart();
            }
            return;
        }
        movePoint.position = AttackTarget.MyTransform.position;


        Vector3 dir = movePoint.position - _agent.transform.position;
        _agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (_agent.hasPath) _agent.ResetPath();
            _timerAttack = 0f;
            _eRef.enemyAnim.SetAim(AttackTarget.MyHead.position); //for some reason it only works in AttackBehaviour()
            _eRef.enemyAnim.Attack(true);
        }
        else
        {
            _eRef.enemyAnim.Attack(false);
             TrackMovingTarget();

        }
    }

    public void PassFromAE_Attacking()
    {
        if (AttackTarget == null)
        {
            EnState = EnemyState.Search;
            return;
        }

        _eRef.enemyAnim.muzzle.SetActive(false);
        _eRef.enemyAnim.muzzle.SetActive(true);
        _eRef.attackClass.Attack(_eRef.enemyAnim.weaponUsed);

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
    void FollowBehaviour()
    {
        AttackTarget = null;
        movePoint.position = _gm.player.GetComponent<IFactionTarget>().MyTransform.position;

        MoveType mt = MoveType.Stationary;
        if (_agent.remainingDistance > _agent.stoppingDistance)
        {
            mt = Vector3.SqrMagnitude(_agent.transform.position - movePoint.position) > 50f ? MoveType.Run : MoveType.Walk;
        }
        _eRef.enemyAnim.SetSpeed(mt);

        TrackMovingTarget();
    }
    #endregion

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
