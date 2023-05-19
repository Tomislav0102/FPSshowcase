using FirstCollection;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.Animations.Rigging;

public class EnemyBehaviour : MonoBehaviour, IFactionTarget, IMaterial
{
    [SerializeField] TextMeshPro _displayState;
    [SerializeField]
    [BoxGroup("Colors for gizmo")]
    [HideLabel]
    Color[] gizmoColorsByState;

    [SerializeField] NavMeshAgent _agent;
    Animator _anim;
    [SerializeField] Transform _movePoint;
    [SerializeField] Collider[] _colliders;
    [SerializeField] Transform lastKnowLocationTransform;
    [SerializeField] Vector2 _moveSpeed = new Vector2(2f, 5f);
    public Transform MyTransform { get => _agent.transform; set { } }
    [field: SerializeField] public Transform MyHead { get ; set ; }

    [field: SerializeField] public Faction Fact { get; set; }
    [field: SerializeField] public MatType MaterialType { get; set; }

    [BoxGroup("Field of view")]
    [HideLabel]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] FieldOvView _fov;

    [SerializeField] EnemyState _startingState;
    EnemyState _nextState;
    public EnemyState EnState
    {
        get => _enState;
        set
        {
            _timerIdleRotate = _timerSearch = _timerAttack = 0f;
            _agent.ResetPath();
            _animClass.Attack(false);
            _agent.speed = _moveSpeed.x;
            _enState = value;
            _animClass.SetSpeed(0);
            if (value != EnemyState.Attack) attackTarget = null;
            switch (value)
            {
                case EnemyState.Idle:
                    break;
                case EnemyState.Patrol:
                    _animClass.SetSpeed(1);
                    break;
                case EnemyState.Roam:
                    _animClass.SetSpeed(1);
                    break;
                case EnemyState.Search:
                    _agent.speed = _moveSpeed.y;
                    hasSearched = true;
                    _animClass.SetSpeed(2);
                    break;
                case EnemyState.Attack:
                    _agent.speed = _moveSpeed.y;
                    hasSearched = false;
                    _timerAttack = Mathf.Infinity;
                    _animClass.SetSpeed(2);
                    break;
                case EnemyState.Follow:
                    break;
                case EnemyState.MoveToPoint:
                    if (_nextState == EnemyState.Search || _nextState == EnemyState.Attack)
                    {
                        _agent.speed = _moveSpeed.y;
                        _animClass.SetSpeed(2);
                    }
                    else
                    {
                        _agent.speed = _moveSpeed.x;
                        _animClass.SetSpeed(1);
                    }
                    break;
            }
            _displayState.text = value.ToString();
            _displayState.color = gizmoColorsByState[(int)value];
        }
    }


    EnemyState _enState;
    Camera _cam;
    Vector3 _startPos;
    Quaternion _startRot;
    bool _canUpdate;

    #region//BEHAVIOURS SPECIFIC
    //idle
    [BoxGroup("Idle")]
    [SerializeField]
    [ShowIf("_startingState", EnemyState.Idle)]
    [Range(0f, 360f)]
    float _idleLookAngle = 180f;
    Quaternion _targetRot;
    float _timerIdleRotate;
    float _startRotY;

    //patrol
    //[BoxGroup("Patrol")]
    //[ShowIf("_startingState", EnemyState.Patrol)]
    //[GUIColor(0f, 1f, 0f, 1f)]
    //[SerializeField] Transform wpParent;
    Transform[] _wayPoints;
    int _counterWayPoints;

    //roam
    [BoxGroup("Roam")]
    [ShowIf("_startingState", EnemyState.Roam)]
    [GUIColor(1f, 0f, 1f, 1f)]
    [SerializeField] float _roamRadius = 10f;
    Vector3 GetRdnPos(Vector3 center)
    {
        return center + _roamRadius * Random.insideUnitSphere;
    }

    //attack
    [GUIColor(1f, 0f, 0f, 1f)]
    [BoxGroup("Attack")]
    [SerializeField] SoItem _weaponUsed;
    [GUIColor(1f, 0f, 0f, 1f)]
    [BoxGroup("Attack")]
    [HideInInspector] public IFactionTarget attackTarget;
    [GUIColor(1f, 0f, 0f, 1f)]
    [BoxGroup("Attack")]
    [SerializeField] float _attackRange = 5f;
    float _attackRangeSquared;
    float _timerAttack;
    [GUIColor(1f, 0f, 0f, 1f)]
    [BoxGroup("Attack")]
    [SerializeField] float _rof = 0.5f;
    [GUIColor(1f, 0f, 0f, 1f)]
    [BoxGroup("Attack")]
    [SerializeField] GameObject muzzle;
    AttackClass _attackClass;

    //search
    float _timerSearch;
    [HideInInspector] public bool hasSearched;
    [HideInInspector] public Vector3 lastKnowLocation;
    #endregion

    [BoxGroup("Animator")]
    [HideLabel]
    [GUIColor(0.27f, 0.53f, 0.77f, 1f)]
    [SerializeField]
    Transform _animTr;
    [BoxGroup("Animator")]
    [HideLabel]
    [GUIColor(0.27f, 0.53f, 0.77f, 1f)]
    [SerializeField]
    AnimClass _animClass;

    [Title("Debug only")]
    public bool consoleDisplay;
    public bool haspath;
    public bool agentGoingToDestination;
    public NavMeshPathStatus pathStatus;
    public float speedCurrent;

    #region//AWAKE -> UPDATE
    private void Awake()
    {
        _cam = GameManager.gm.mainCam;
        _fov.Init(this, this, _agent.transform, consoleDisplay);
        _anim = GetComponent<Animator>();
        _animClass.Init(_moveSpeed);
        _attackClass = new AttackClass(_colliders, this);
        _attackClass.bulletSpawnPosition = muzzle.transform;


    }
    private void Start()
    {
        _agent.updatePosition = false;
        _startPos = _agent.transform.position;
        _startRot = _agent.transform.rotation;
        EnState = _startingState;
        _attackRangeSquared = _attackRange * _attackRange;

        //idle
        _startRotY = _agent.transform.eulerAngles.y;

        //patrol
         _wayPoints = HelperScript.AllChildren(GameManager.gm.wayPointParent);

        switch (EnState)
        {
            case EnemyState.Idle:
                break;
            case EnemyState.Patrol:
                if (_wayPoints.Length > 0) _movePoint.position = _wayPoints[_counterWayPoints].position;
                break;
            case EnemyState.Roam:
                break;
            case EnemyState.Search:
                break;
            case EnemyState.Attack:
                break;
            case EnemyState.Follow:
                break;
            case EnemyState.MoveToPoint:
                break;
        }

        InvokeRepeating(nameof(CanUpdateMethod), Random.Range(0f, 1f), 0.2f);
    }

    private void Update()
    {
        _displayState.transform.LookAt(_cam.transform.position);
        _displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        Vector3 worldDelatPos = _agent.nextPosition - transform.position;
        if (worldDelatPos.magnitude > _agent.radius)
        {
            _agent.nextPosition = transform.position + 0.5f * worldDelatPos;
        }

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

      //  if (Input.GetMouseButtonDown(1)) _animClass.SetAim(Vector3.zero);
    }
    void FixedUpdate()
    {
        if (EnState == EnemyState.Attack) return;
        if (_canUpdate)
        {
            attackTarget = _fov.FindFovTargets();
            _canUpdate = false;
        }
    }
    void Debugs()
    {
        lastKnowLocationTransform.position = lastKnowLocation;
        haspath = _agent.hasPath;
        pathStatus = _agent.pathStatus;
        speedCurrent = _agent.velocity.magnitude;
    }
    void CanUpdateMethod()
    {
        _canUpdate = true;
    }
    void GoToTarget()
    {
        agentGoingToDestination = false;
        if(!_agent.hasPath && !_agent.pathPending)
        {
            _animClass.Attack(false);
            _agent.SetDestination(_movePoint.position);
            agentGoingToDestination = true;
        }
    }
    void OnAnimatorMove()
    {
        Vector3 pos = _anim.rootPosition;
        pos.y = _agent.nextPosition.y;
        transform.position = pos;
    }

    #endregion


    #region//BEHAVIOURS
    void IdleBehaviour(bool lookAround)
    {
        if (!lookAround) return;

        _timerIdleRotate -= Time.deltaTime;
        if (_timerIdleRotate <= 0f)
        {
            _timerIdleRotate = Random.Range(3f, 10f);
            _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-_idleLookAngle * 0.5f, _idleLookAngle * 0.5f), Vector3.up);
        }
        _agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, _targetRot, 10 * Time.deltaTime);
    }
    void PatrolBehaviour()
    {
        if (_agent.hasPath && _agent.remainingDistance <= _agent.radius)
        {
            _counterWayPoints = (1 + _counterWayPoints) % _wayPoints.Length;
            _agent.ResetPath();
        }
        _movePoint.position = _wayPoints[_counterWayPoints].position;

        GoToTarget();
    }
    void RoamBehaviour(Vector3 center)
    {
        if (!_agent.hasPath && !_agent.pathPending) _movePoint.position = GetRdnPos(center);
        GoToTarget();
    }
    void AttackBehaviour()
    {
        if (attackTarget == null)
        {
            _movePoint.SetPositionAndRotation(_startPos, _startRot);
            _nextState = _startingState;
            EnState = EnemyState.MoveToPoint;
            GoToTarget();
            return;
        }

        _movePoint.position = lastKnowLocation = attackTarget.MyTransform.position;

        if (!_fov.TargetStillVisible(attackTarget))
        {
            _nextState = EnemyState.Search;
            EnState = EnemyState.MoveToPoint;
            return;
        }

       // _animClass.SetSpeed(Mathf.Infinity);
        if (Vector3.SqrMagnitude(lastKnowLocation - _agent.transform.position) < _attackRangeSquared)
        {
            if (_agent.hasPath) _agent.ResetPath();
            _animClass.Attack(true);
            _animTr.LookAt(attackTarget.MyTransform);
            _timerAttack = 0f;
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

    public void AE_Attacking()
    {
        if (attackTarget == null)
        {
            EnState = EnemyState.Search;
            return;
        }

        muzzle.SetActive(false);
        muzzle.SetActive(true);
        _animClass.SetAim(attackTarget.MyHead.position); //for some reason it doesn't work here, but works in AttackBehaviour()
        _attackClass.Attack(_weaponUsed);
    }

    void SearchBeahaviour()
    {
        RoamBehaviour(lastKnowLocation);
        _timerSearch += Time.deltaTime;
        if (_timerSearch > 5f)
        {
            _timerSearch = 0f;
            _movePoint.SetPositionAndRotation(_startPos, _startRot);
            _nextState = _startingState;
            EnState = EnemyState.MoveToPoint;
        }
    }
    void MoveTo()
    {
        if (_agent.hasPath && _agent.remainingDistance < 0.1f)
        {
            _agent.transform.rotation = _startRot;
            EnState = _nextState;
            return;
        }
        GoToTarget();
    }
    #endregion

}

[System.Serializable]
public class AnimClass
{
    [SerializeField] Animator _anim;
    [SerializeField] EnemyBehaviour _enemyBehaviour;
    [SerializeField] Rig rigAiming;
    [SerializeField] MultiAimConstraint _multiAimConstraintRightHand;
    Transform _aimIK;
    Vector2 _moveSpeed;
    float _targtSpeed;
    float _spreadWeapon = 50f;


    public void Init(Vector2 moveSpeed)
    {
        _moveSpeed = moveSpeed;
        _aimIK = _multiAimConstraintRightHand.data.sourceObjects[0].transform;

    }
    public void Attack(bool isAttacking)
    {
        _anim.SetBool("attack", isAttacking);
        rigAiming.weight = isAttacking ? 1 : 0;
    }
    public void SetAim(Vector3 pos)
    {
      //  _multiAimConstraintRightHand.data.offset = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));

        _aimIK.position = pos;
    }
    public void SetSpeed(int speed)
    {
        //int sp = 0;
        //if (speed  < _moveSpeed.x * 0.1f) sp = 0;
        //else if (speed <= _moveSpeed.x) sp = 1;
        //else sp = 2;

        _anim.SetInteger("movePhase", speed);
        //_targtSpeed = Mathf.MoveTowards(_targtSpeed, sp, 2f * Time.deltaTime);
        //_anim.SetFloat("moveSpeed", _targtSpeed);
    }
}

[System.Serializable]
public class FieldOvView
{
    EnemyBehaviour _enemyBehaviour;
    IFactionTarget _parentFovTraget;
    Transform _myTransform;
    //[SerializeField] Transform head;
    [SerializeField] Transform _sightSphere, _hearSphere;
    float _sightRange, _hearingRange;
    [SerializeField] float _sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    [SerializeField] LayerMask layerFOV;
    Collider[] _colls = new Collider[30];
    RaycastHit[] _multipleHits = new RaycastHit[1];

    bool _conseoleDisplay;
    public void Init(IFactionTarget fovTarget, EnemyBehaviour enemyBehaviour, Transform agentTransform, bool consoleDis)
    {
        _parentFovTraget = fovTarget;
        _enemyBehaviour = enemyBehaviour;
        _myTransform = agentTransform;
        _sightRange = _sightSphere.localScale.x * 0.5f;
        _hearingRange = _hearSphere.localScale.x * 0.5f;
        _sightSphere.gameObject.SetActive(false);
        _hearSphere.gameObject.SetActive(false);

        _sightAngleTrigonometry = Mathf.Cos(_sightAngle * 0.5f * Mathf.Deg2Rad);
        _conseoleDisplay = consoleDis;
    }

    float EffectiveRange(Vector3 targetPos)
    {
        float r = _sightAngle;
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
                                return en.attackTarget;
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

