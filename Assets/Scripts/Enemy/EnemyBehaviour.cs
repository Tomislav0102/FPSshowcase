using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class EnemyBehaviour : MonoBehaviour, IFaction
{
    #region//INTERFACE
    [field: SerializeField] public Transform MyTransform { get; set; }
    [field: SerializeField] public Collider MyCollider { get; set; }
    [field: SerializeField] public Transform MyHead { get; set; }
    [field: SerializeField] public Faction Fact { get; set; }
    #endregion

    GameManager _gm;
    EnemyRef _eRef;
    TextMeshPro _displayState;
    Camera _cam;
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Quaternion startRot;
    bool _canUpdateFOV, _canUpdateDestination;
    MoveType _moveType;
    public RagToAnimTranstions ragToAnimTransition;
    public ParticleSystem psOnFire;
    [HideInInspector] public Transform movePoint;
    [HideInInspector] public bool hasSearched;
    public IFaction attackTarget;
    [HideInInspector] public DetectableObject detectObject;
    float _weightHit;


    #region//BEHAVIOUR SPECIFIC
    [Title("Behaviours", null, TitleAlignments.Centered)]
    [GUIColor(0f, 0.64f, 1f, 1f)]
    public EnemyState startingState;
    public EnemyState EnState
    {
        get => _enState;
        set
        {
            _idle.ResetMe();
            _patrol.ResetMe();
            roam.ResetMe();
            search.ResetMe();
            attack.ResetMe();
            _follow.ResetMe();
            _immoblie.ResetMe();
            _flee.ResetMe();

            _eRef.agent.ResetPath();
            _eRef.agent.stoppingDistance = 0f;
            Attack_Animation(false);
            _enState = value;
          //  print(value);
            _moveType = MoveType.Stationary;
            switch (value)
            {
                case EnemyState.Idle:
                    attackTarget = null;
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Patrol:
                    attackTarget = null;
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Roam:
                    attackTarget = null;
                    _moveType = MoveType.Walk;
                    break;
                case EnemyState.Search:
                    hasSearched = true;
                    _moveType = MoveType.Run;
                    break;
                case EnemyState.Attack:
                    hasSearched = false;
                    detectObject = null;
                    _moveType = MoveType.Run;
                    break;
                case EnemyState.Follow:
                    attackTarget = null;
                    _eRef.agent.stoppingDistance = 3f;
                    break;
                case EnemyState.Immobile:
                    rigRightHandAiming.weight = rigLeftHand.weight = 0f;
                    break;
                case EnemyState.Flee:
                    if (attackTarget != null)
                    {
                        _moveType = MoveType.Run;
                        Vector3 dir = (MyTransform.position - attackTarget.MyTransform.position).normalized;
                        movePoint.position = EnemyRef.GetRdnPos(MyTransform.position + 50f * dir, 0f);
                    }
                    else EnState = startingState;
                    break;
            }
            SetSpeed_Animation(_moveType);
            _displayState.text = value.ToString();
            _displayState.color = GameManager.Instance.gizmoColorsByState[(int)value];
        }
    }
    EnemyState _enState;

    BehIdle _idle;
    BehPatrol _patrol;
    public BehRoam roam;
    public BehSearch search;
    public BehAttack attack;
    BehFollow _follow;
    BehImmoblie _immoblie;
    BehFlee _flee;

    [SerializeField]
    [GUIColor(0f, 0.64f, 1f, 1f)]
    [Range(0f, 360f)]
    float idleLookAngle = 180f;
    [SerializeField]
    [GUIColor(0f, 0.64f, 1f, 1f)]
    Transform wpParent;
    [SerializeField]
    [GUIColor(0f, 0.64f, 1f, 1f)]
    float roamRadius = 10f;
    #endregion


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
    float _spreadWeapon = 50f;
    Vector3 _offsetTar;
    [HideInInspector] public bool isHit;
    float _weightRightHandAim, _weightLeftHand;
    BehIdle behidle;

    [Title("Debug only")]
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedAnimRoot;
    public float speedAgent;
    public float remainDistance;
    public string nameOfTarget;


    #region //MAIN
    public void InitAwake(EnemyRef eRef, Transform moveP, TextMeshPro displayT, out HashSet<Collider> hs/*, out DetectableObject detectObject*/)
    {
        _gm = GameManager.Instance;
        _cam = _gm.mainCam;
        _eRef = eRef;
        movePoint = moveP;
        _displayState = displayT;
        startPos = _eRef.agentTr.position;
        startRot = _eRef.agentTr.rotation;
        _idle = new BehIdle(_eRef, idleLookAngle);
        _patrol = new BehPatrol(_eRef, _gm.wayPointParent, null);
        roam = new BehRoam(_eRef, roamRadius);
        search = new BehSearch(_eRef);
        _follow = new BehFollow(_eRef);
        attack = new BehAttack(_eRef);
        _immoblie = new BehImmoblie(_eRef);
        _flee = new BehFlee(_eRef);
        EnState = startingState;


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
        ragToAnimTransition = new RagToAnimTranstions(_eRef, ragdollTransform);
    }
    void OnEnable()
    {
        InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0.3f, 1f), 0.3f);

    }
    void OnDisable()
    {
        attackTarget = null;
        CancelInvoke();
        _displayState.text = "Dead";
    }

    private void Update()
    {
        Debugs();

        switch (EnState)
        {
            case EnemyState.Idle:
                _idle.UpdateLoop(true);
                break;
            case EnemyState.Patrol:
                _patrol.UpdateLoop();
                break;
            case EnemyState.Roam:
                roam.UpdateLoop(startPos);
                break;
            case EnemyState.Search:
               search.UpdateLoop();
                break;
            case EnemyState.Attack:
               attack.UpdateLoop();
                break;
            case EnemyState.Follow:
               _follow.UpdateLoop();
                break;
            case EnemyState.Immobile:
                _immoblie.UpdateLoop();
                return;
            case EnemyState.Flee: 
               _flee.UpdateLoop();
                break;
        }

        speedAnimRoot = _eRef.anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        _eRef.agent.speed = speedAnimRoot;
        _eRef.animTr.SetPositionAndRotation(_eRef.agentTr.position - 0.06152725f * Vector3.up, _eRef.agentTr.rotation);

       // rigRightHandAiming.weight = _weightRightHandAim;
        rigRightHandAiming.weight = Mathf.MoveTowards(rigRightHandAiming.weight, _weightRightHandAim, 4f * Time.deltaTime);
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, 4f * Time.deltaTime);
        _weightHit = Mathf.MoveTowards(_weightHit, isHit ? 1f : 0f, 2f * Time.deltaTime);
        _eRef.anim.SetLayerWeight(1, _weightHit);

        if (EnState == EnemyState.Attack || EnState == EnemyState.Flee) return;
        if (_canUpdateFOV)
        {
            _eRef.fov.GetAllTargets(out attackTarget, out detectObject);
            if (attackTarget != null)
            {
                EnState = EnemyState.Attack;
                movePoint.position = attackTarget.MyTransform.position;
            }
            else if (detectObject != null)
            {
                if (!hasSearched)
                {
                    movePoint.position = detectObject.owner.MyTransform.position;
                    search.SearchStart();
                }
            }

            _canUpdateFOV = false;
        }
    }
    #endregion


    void Debugs()
    {
        _displayState.transform.LookAt(_cam.transform.position);
        _displayState.transform.Rotate(180 * Vector3.up, Space.Self);

        haspath = _eRef.agent.hasPath;
        pathStatus = _eRef.agent.pathStatus;
        remainDistance = _eRef.agent.remainingDistance;
        speedAgent = _eRef.agent.velocity.magnitude;
        nameOfTarget = attackTarget == null ? "no target" : attackTarget.MyTransform.name.ToString();
    }
    void CanUpdateFOVMethod() => _canUpdateFOV = _canUpdateDestination = true;
    public void TrackMovingTarget()
    {
        Attack_Animation(false);
        if (_canUpdateDestination)
        {
            _eRef.agent.SetDestination(movePoint.position);
            //   print("moving");
            _canUpdateDestination = false;
        }
    }

    public void PassFromHealth_Attacked(Transform attackerTr, bool switchAgro, bool lowHP)
    {
        if (EnState == EnemyState.Immobile || attackerTr == null) return;
        if (lowHP)
        {
            if (attackerTr.TryGetComponent(out IFaction target) && EnemyRef.HostileFaction(Fact, target.Fact))
            {
                attackTarget = target;
                EnState = EnemyState.Flee;
            }
            return;
        }

        if (EnState == EnemyState.Attack)
        {
            if (switchAgro) NewTarget();
            return;
        }
        NewTarget();

        void NewTarget()
        {
            if (attackerTr.TryGetComponent(out IFaction target) && EnemyRef.HostileFaction(Fact, target.Fact))
            {
                attackTarget = target;
                EnState = EnemyState.Search;
            }
        }
    }
    public void AE_Attacking()
    {
        if (isHit) return;

        if (attackTarget == null)
        {
            EnState = EnemyState.Search;
            return;
        }

        muzzle.SetActive(false);
        muzzle.SetActive(true);
        _eRef.attackClass.Attack(weaponUsed);

    }


    #region//ANIMATIONS
    void GetIK_Animation(bool attack)
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
    public void Attack_Animation(bool isAttacking)
    {
        GetIK_Animation(isAttacking);
        _eRef.anim.SetBool("attack", isAttacking);
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee || !isAttacking) return;
        _offsetTar = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));
    }
    public void SetAim_Animation()
    {
        _aimIK.position = attackTarget.MyHead.position;
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee) return;

        multiAimConstraintRightHand.data.offset =
            Vector3.Lerp(multiAimConstraintRightHand.data.offset, _offsetTar, 0.3f * Time.deltaTime);

    }
    public void SetSpeed_Animation(MoveType movetype)
    {
        _eRef.anim.SetInteger("movePhase", (int)movetype);
    }
    #endregion



}


