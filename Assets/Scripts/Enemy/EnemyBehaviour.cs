using FirstCollection;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Timers;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;

public class EnemyBehaviour : MonoBehaviour, IFactionTarget
{
    #region//INTERFACE
    [field: SerializeField] public Transform MyTransform { get; set; }
    [field: SerializeField] public Transform MyHead { get ; set ; }
    [field: SerializeField] public Faction Fact { get; set; }
    public IFactionTarget Owner { get; set; }
    #endregion

    GameManager _gm;
    EnemyRef _eRef;
    TextMeshPro _displayState;
    Camera _cam;
    Vector3 _startPos;
    Quaternion _startRot;
    bool _canUpdateFOV, _canUpdateDestination;
    MoveType _moveType;
    [Title("General", null, TitleAlignments.Centered)]
    [SerializeField] DetectableObject detectable;
    [HideInInspector] public Transform movePoint;

    [Title("Behaviours", null, TitleAlignments.Centered)]
    [SerializeField] EnemyState startingState;
    public EnemyState EnState
    {
        get => _enState;
        set
        {
            _timerIdleRotate = _timerSearch = _timerAttack = 0f;
            _eRef.agent.ResetPath();
            _eRef.agent.stoppingDistance = 0f;
            Attack_Animation(false);
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
                    _eRef.agent.stoppingDistance = 3f;
                    break;
            }
            SetSpeed_Animation(_moveType);
            _displayState.text = value.ToString();
            _displayState.color = GameManager.Instance.gizmoColorsByState[(int)value];
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
    float _weightHit;

    //search
    float _timerSearch;
    [HideInInspector] public bool hasSearched;
    Vector3 _searchCenter;
    #endregion

    bool ReadyToMove()
    {
        return !_eRef.agent.pathPending && _eRef.agent.remainingDistance <= 0.5f; 
    }

    [Title("Animations", null, TitleAlignments.Centered)]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] Transform[] ragdollTransform;
    RagdollBodyPart[] _bodyParts;
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public SoItem weaponUsed;
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public GameObject muzzle;
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] Rig rigRightHandAiming;
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] Rig rigLeftHand;
    [GUIColor(0.5f, 1f, 0f, 1f)]
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand; //needed for accuracy (together with '_spreadWeapon')
    Transform _aimIK;
    float _spreadWeapon = 0f;
    Vector3 _offsetTar;
    [HideInInspector] public bool isHit;
    float _weightRightHandAim, _weightLeftHand;


    [Title("Debug only")]
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedAnimRoot;
    public float speedAgent;
    public float remainDistance;
    public string namOfAttacker;


    #region //MAIN
    public void InitAwake(EnemyRef eRef, Transform moveP, TextMeshPro displayT, out HashSet<Collider> hs, out DetectableObject detectObject)
    {
        _gm = GameManager.Instance;
        _cam = _gm.mainCam;
        _eRef = eRef;
        movePoint = moveP;
        _displayState = displayT;
        _startPos = _eRef.agent.transform.position;
        _startRot = _eRef.agent.transform.rotation;
        _searchCenter = movePoint.position;
        EnState = startingState;

        //idle
        _startRotY = _eRef.agent.transform.eulerAngles.y;

        //patrol
        _wayPoints = HelperScript.AllChildren((wpParent == null || wpParent.childCount == 0) ? GameManager.Instance.wayPointParent : wpParent);

        //anims
        _aimIK = multiAimConstraintRightHand.data.sourceObjects[0].transform;
        _bodyParts = new RagdollBodyPart[ragdollTransform.Length];
        HashSet<Collider> colls = new HashSet<Collider>();
        for (int i = 0; i < _bodyParts.Length; i++)
        {
            _bodyParts[i] = ragdollTransform[i].GetComponent<RagdollBodyPart>();
            _bodyParts[i].InitializeMe(_eRef);
            colls.Add(_bodyParts[i].GetComponent<Collider>());
        }
        hs = colls;
        _eRef.anim.SetFloat("rof", weaponUsed.rofModifier);
        detectObject = detectable;
    }
    public void InitAttackRange(float sightRange)
    {
        float range = Mathf.Min(weaponUsed.range, sightRange);
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
        _eRef.agent.speed = speedAnimRoot;
        _eRef.animTr.SetPositionAndRotation(_eRef.agent.transform.position - 0.06152725f * Vector3.up, _eRef.agent.transform.rotation);

        rigRightHandAiming.weight = _weightRightHandAim;
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, 2f * Time.deltaTime);

        _weightHit = Mathf.MoveTowards(_weightHit, isHit ? 1f : 0f, 2f * Time.deltaTime);
        _eRef.anim.SetLayerWeight(1, _weightHit);
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
        _displayState.transform.LookAt(_cam.transform.position);
        _displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        haspath = _eRef.agent.hasPath;
        pathStatus = _eRef.agent.pathStatus;
        remainDistance = _eRef.agent.remainingDistance;
        speedAgent =_eRef.agent.velocity.magnitude;
        namOfAttacker = AttackTarget == null ? "no target" : AttackTarget.MyTransform.name.ToString();
    }
    void CanUpdateFOVMethod() //optimization method. Detection and navigation don't need updates on every frame.
    {
        _canUpdateFOV = _canUpdateDestination = true;
    }
    void TrackMovingTarget()
    {
        Attack_Animation(false);
        if (_canUpdateDestination)
        {
            _eRef.agent.SetDestination(movePoint.position);
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
    void GetIKAnimation(bool attack)
    {
        _weightRightHandAim = _weightLeftHand = 0f;
        if (isHit) return;

        switch (weaponUsed.enemyWeaponUsed)
        {
            case EnemyWeaponUsed.Melee:
                _weightRightHandAim = attack ? 1f : 0f;
                break;
            case EnemyWeaponUsed.Pistol:
                _weightRightHandAim = _weightLeftHand = attack ? 1f : 0f;
                break;
            case EnemyWeaponUsed.Rifle:
                _weightRightHandAim = attack ? 1f : 0f;
                _weightLeftHand = 1f;
                break;
        }
    }
    void Attack_Animation(bool isAttacking)
    {
        GetIKAnimation(isAttacking);
        _eRef.anim.SetBool("attack", isAttacking);
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee) return;
        _offsetTar = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));
    }
    void SetAim_Animation(Vector3 pos)
    {
        _aimIK.position = pos;
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee) return;

        multiAimConstraintRightHand.data.offset =
            Vector3.Lerp(multiAimConstraintRightHand.data.offset, _offsetTar, 0.3f * Time.deltaTime);

    }
    void SetSpeed_Animation(MoveType movetype)
    {
        _eRef.anim.SetInteger("movePhase", (int)movetype);
    }

    #endregion


    #region //BEHAVIOURS
    void IdleBehaviour(bool lookAround)
    {
        if (Vector3.SqrMagnitude(movePoint.position - _eRef.agent.transform.position) < 0.3f)
        {
            _moveType = MoveType.Stationary;
            SetSpeed_Animation(_moveType);

            if (_idleOnMove)
            {
                _idleOnMove = false;
                if(_eRef.agent.hasPath) _eRef.agent.ResetPath();
                _eRef.agent.transform.rotation = _startRot;
            }

            if (!lookAround) return;

            _timerIdleRotate -= Time.deltaTime;
            if (_timerIdleRotate <= 0f)
            {
                _timerIdleRotate = Random.Range(3f, 10f);
                _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-idleLookAngle * 0.5f, idleLookAngle * 0.5f), Vector3.up);
            }
            _eRef.agent.transform.rotation = Quaternion.Slerp(_eRef.agent.transform.rotation, _targetRot, 10 * Time.deltaTime);
            return;
        }
        movePoint.position = _startPos;

        if(!_eRef.agent.hasPath)
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
            _eRef.agent.SetDestination(movePoint.position);
        } 
    }
    void RoamBehaviour(Vector3 center)
    {
        if (ReadyToMove())
        {
            movePoint.position = GetRdnPos(center);
            _eRef.agent.SetDestination(movePoint.position);
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


        if (!_eRef.fov.TargetStillVisible(AttackTarget, _gm.layFOV_Ray))
        {
            Attack_Animation(false);
            TrackMovingTarget();

            if (_eRef.agent.remainingDistance < 1f)
            {
                SearchStart();
            }
            return;
        }
        movePoint.position = AttackTarget.MyTransform.position;


        Vector3 dir = movePoint.position - _eRef.agent.transform.position;
        _eRef.agent.transform.rotation = Quaternion.Slerp(_eRef.agent.transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (_eRef.agent.hasPath) _eRef.agent.ResetPath();
            _timerAttack = 0f;
            SetAim_Animation(AttackTarget.MyHead.position); //for some reason it only works in AttackBehaviour()
            Attack_Animation(true);
        }
        else
        {
            Attack_Animation(false);
             TrackMovingTarget();

        }
    }

    public void AE_Attacking()
    {
        if (isHit) return;

        if (AttackTarget == null)
        {
            EnState = EnemyState.Search;
            return;
        }

        muzzle.SetActive(false);
        muzzle.SetActive(true);
        _eRef.attackClass.Attack(weaponUsed);

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
        if (_eRef.agent.remainingDistance > _eRef.agent.stoppingDistance)
        {
            mt = Vector3.SqrMagnitude(_eRef.agent.transform.position - movePoint.position) > 50f ? MoveType.Run : MoveType.Walk;
        }
        SetSpeed_Animation(mt);

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
