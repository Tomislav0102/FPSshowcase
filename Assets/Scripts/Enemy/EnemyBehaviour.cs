using FirstCollection;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;

public class EnemyBehaviour : MonoBehaviour, IFaction
{
    #region//INTERFACE
    [field: SerializeField] public Transform MyTransform { get; set; }
    public Collider MyCollider { get; set; }
    [field: SerializeField] public Transform MyHead { get; set; }
    [field: SerializeField] public Faction Fact { get; set; }
    public IFaction Owner { get; set; }
    #endregion

    GameManager _gm;
    EnemyRef _eRef;
    TextMeshPro _displayState;
    Camera _cam;
    Vector3 _startPos;
    Quaternion _startRot;
    bool _canUpdateFOV, _canUpdateDestination;
    MoveType _moveType;
    public RagToAnimTranstions ragToAnimTransition;
    [Title("General", null, TitleAlignments.Centered)]
    [SerializeField] DetectableObject detectable;
    public ParticleSystem psOnFire;
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
        if (NavMesh.SamplePosition(pos, out _navHit, 2f * roamRadius, NavMesh.AllAreas))
        {
            return _navHit.position;
        }

        return pos;
    }
    NavMeshHit _navHit;

    //attack
    public IFaction AttackTarget;
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
    bool HostileFaction(Faction attackerFaction)
    {
        if (Fact == attackerFaction) return false;

        switch (attackerFaction)
        {
            case Faction.Player:
                if (Fact == Faction.Enemy) return true;
                break;
            case Faction.Enemy:
                if (Fact == Faction.Ally) return true;
                break;
            case Faction.Ally:
                if (Fact == Faction.Enemy) return true;
                break;
        }

        return false;
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
    /*[HideInInspector] */public bool isHit;
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
        _startPos = _eRef.agentTr.position;
        _startRot = _eRef.agentTr.rotation;
        _searchCenter = movePoint.position;
        EnState = startingState;
        Owner = this;

        //idle
        _startRotY = _eRef.agentTr.eulerAngles.y;

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
        ragToAnimTransition = new RagToAnimTranstions(_eRef, ragdollTransform);
       
        detectObject = detectable;
    }
    public void InitAttackRange(float sightRange)
    {
        float range = Mathf.Min(weaponUsed.range, sightRange);
        _attackRangeSquared = Mathf.Pow(range, 2f);
    }
    void OnEnable()
    {
        InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0.3f, 1f), 0.3f);
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
            case EnemyState.Immobile:
                ImmobileBehaviour();
                return;
        }

        speedAnimRoot = _eRef.anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        _eRef.agent.speed = speedAnimRoot;
        _eRef.animTr.SetPositionAndRotation(_eRef.agentTr.position - 0.06152725f * Vector3.up, _eRef.agentTr.rotation);

        rigRightHandAiming.weight = _weightRightHandAim;
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, 2f * Time.deltaTime);

        _weightHit = Mathf.MoveTowards(_weightHit, isHit ? 1f : 0f, 2f * Time.deltaTime);
        _eRef.anim.SetLayerWeight(1, _weightHit);


        if (EnState == EnemyState.Attack || EnState == EnemyState.Immobile) return;
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
        speedAgent = _eRef.agent.velocity.magnitude;
        namOfAttacker = AttackTarget == null ? "no target" : AttackTarget.MyTransform.name.ToString();
    }
    void CanUpdateFOVMethod() => _canUpdateFOV = _canUpdateDestination = true;
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
        if (EnState == EnemyState.Immobile) return;

        if (EnState == EnemyState.Attack || AttackTarget != null)
        {
            if (switchAgro) NewTarget();
            return;
        }
        NewTarget();

        void NewTarget()
        {
            if (attackerTr.TryGetComponent(out IFaction target) && HostileFaction(target.Fact))
            {
                AttackTarget = target;
                EnState = EnemyState.Search;
            }
        }
    }

    #endregion

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
    void Attack_Animation(bool isAttacking)
    {
        GetIK_Animation(isAttacking);
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
        if (Vector3.SqrMagnitude(movePoint.position - _eRef.agentTr.position) < 0.3f)
        {
            _moveType = MoveType.Stationary;
            SetSpeed_Animation(_moveType);

            if (_idleOnMove)
            {
                _idleOnMove = false;
                if (_eRef.agent.hasPath) _eRef.agent.ResetPath();
                _eRef.agentTr.rotation = _startRot;
            }

            if (!lookAround) return;

            _timerIdleRotate -= Time.deltaTime;
            if (_timerIdleRotate <= 0f)
            {
                _timerIdleRotate = Random.Range(3f, 10f);
                _targetRot = Quaternion.AngleAxis(_startRotY + Random.Range(-idleLookAngle * 0.5f, idleLookAngle * 0.5f), Vector3.up);
            }
            _eRef.agentTr.rotation = Quaternion.Slerp(_eRef.agentTr.rotation, _targetRot, 10 * Time.deltaTime);
            return;
        }
        movePoint.position = _startPos;

        if (!_eRef.agent.hasPath)
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


        if (!_eRef.fov.TargetStillVisible(AttackTarget))
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


        Vector3 dir = movePoint.position - _eRef.agentTr.position;
        _eRef.agentTr.rotation = Quaternion.Slerp(_eRef.agentTr.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        if (Vector3.SqrMagnitude(dir) <= _attackRangeSquared)
        {
            if (_eRef.agent.hasPath) _eRef.agent.ResetPath();
            _timerAttack = 0f;
            SetAim_Animation(AttackTarget.Owner.MyHead.position); //for some reason it only works in AttackBehaviour()
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
        movePoint.position = _gm.player.GetComponent<IFaction>().MyTransform.position;

        MoveType mt = MoveType.Stationary;
        if (_eRef.agent.remainingDistance > _eRef.agent.stoppingDistance)
        {
            mt = Vector3.SqrMagnitude(_eRef.agentTr.position - movePoint.position) > 50f ? MoveType.Run : MoveType.Walk;
        }
        SetSpeed_Animation(mt);

        TrackMovingTarget();
    }
    private void ImmobileBehaviour()
    {
        ragToAnimTransition.RagdollStandingUp();
        _eRef.agentTr.SetPositionAndRotation(new Vector3(_eRef.animTr.position.x, _eRef.agentTr.position.y, _eRef.animTr.position.z), _eRef.animTr.rotation);
        rigRightHandAiming.weight = rigLeftHand.weight = 0f;
    }

    #endregion


    [System.Serializable]
    public class RagToAnimTranstions
    {
        struct BoneTransforms
        {
            public Vector3 pos;
            public Quaternion rot;
        }

        EnemyRef _eRef;
        Transform _myTransform;
        Animator _anim;
        readonly string[] _clipNames = { "Stand Up", "Zombie Stand Up" };
        string _currentClip;
        Transform[] _ragdollTransforms;
        Rigidbody[] _ragdollRigids;
        Transform _hipsBone;

        List<Transform> _bones = new List<Transform>();
        BoneTransforms[] _standingFaceUpBones;
        BoneTransforms[] _standingFaceDownBones;
        BoneTransforms[] _standingCurrent;
        BoneTransforms[] _ragdollBones;

        bool FacingUp() => _hipsBone.forward.y > 0f;
        bool _readyToStandUp;
        const float CONST_TIMETOSTANDUP = 2f;
        float _timer;
        float _elapsedPercentage;

        public RagToAnimTranstions(EnemyRef eRef, Transform[] ragParts)
        {
            _eRef = eRef;
            _ragdollTransforms = ragParts;
            _myTransform = _eRef.animTr;
            _anim = _eRef.anim;

            _hipsBone = _anim.GetBoneTransform(HumanBodyBones.Hips);

            _ragdollRigids = new Rigidbody[_ragdollTransforms.Length];
            for (int i = 0; i < _ragdollTransforms.Length; i++)
            {
                _ragdollRigids[i] = _ragdollTransforms[i].GetComponent<Rigidbody>();
            }

            Transform[] bon = _hipsBone.GetComponentsInChildren<Transform>();
            for (int i = 0; i < bon.Length; i++)
            {
                if (bon[i].name.StartsWith("mixamorig")) _bones.Add(bon[i]);
            }
            _standingFaceUpBones = new BoneTransforms[_bones.Count];
            _standingFaceDownBones = new BoneTransforms[_bones.Count];
            _ragdollBones = new BoneTransforms[_bones.Count];

            PopulateAnimationBones(true);
            PopulateAnimationBones(false);

        }

        public void RagdollStandingUp()
        {
            if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Rifle Idle"))
            {
                _eRef.enemyBehaviour.EnState = EnemyState.Attack;

            }
            if (!_readyToStandUp) return;

            _timer += Time.deltaTime;
            _elapsedPercentage = _timer / CONST_TIMETOSTANDUP;
            for (int i = 0; i < _bones.Count; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(_ragdollBones[i].pos, _standingCurrent[i].pos, _elapsedPercentage);
                _bones[i].localRotation = Quaternion.Lerp(_ragdollBones[i].rot, _standingCurrent[i].rot, _elapsedPercentage);
            }


          //  if (Mathf.Approximately(_elapsedPercentage, 1f))
            if (_elapsedPercentage >= 1f)
            {
                _readyToStandUp = false;
                _anim.Play(_currentClip, 0, 0);
                _timer = 0f;
                _anim.enabled = true;
                for (int i = 0; i < _ragdollRigids.Length; i++)
                {
                    _ragdollRigids[i].isKinematic = true;
                }
            }

        }



        public void RagdollMe(Rigidbody ragRigid, Transform attackerTr)
        {
            if (!_readyToStandUp)
            {
                for (int i = 0; i < _ragdollRigids.Length; i++)
                {
                    _ragdollRigids[i].isKinematic = false;
                }

                Vector3 dir = (attackerTr.position - _myTransform.position).normalized;
                ragRigid.AddForce(-40f * dir, ForceMode.VelocityChange);
                _anim.enabled = false;
                _eRef.enemyBehaviour.EnState = EnemyState.Immobile;
                BeginStandUp();
            }

        }
        async void BeginStandUp()
        {
            await Task.Delay(2000);
            _readyToStandUp = true;
            _standingCurrent = FacingUp() ? _standingFaceUpBones : _standingFaceDownBones;
            AlignRotationToHips();
            AlignPositionToHips();
            PopulateBones(_ragdollBones);
            _currentClip = FacingUp() ? _clipNames[0] : _clipNames[1];

        }

        void ActivateRagdoll(bool activ)
        {
            for (int i = 0; i < _ragdollRigids.Length; i++)
            {
                _ragdollRigids[i].isKinematic = !activ;
            }
            if (activ)
            {
                if (!_readyToStandUp)
                {
                    _ragdollRigids[8].AddForce(-40f * Vector3.forward, ForceMode.VelocityChange);
                    _anim.enabled = false;
                }
            }
            else
            {
                _standingCurrent = FacingUp() ? _standingFaceUpBones : _standingFaceDownBones;
                AlignRotationToHips();
                AlignPositionToHips();
                PopulateBones(_ragdollBones);
                _readyToStandUp = true;
                _currentClip = FacingUp() ? _clipNames[0] : _clipNames[1];
            }
        }
        void PopulateBones(BoneTransforms[] bon)
        {
            for (int i = 0; i < _bones.Count; i++)
            {
                bon[i].pos = _bones[i].localPosition;
                bon[i].rot = _bones[i].localRotation;
            }
        }
        void PopulateAnimationBones(bool isFacingUp)
        {
            Vector3 posBeforeSampling = _myTransform.position;
            Quaternion rotBeforeSampling = _myTransform.rotation;

            _currentClip = isFacingUp ? _clipNames[0] : _clipNames[1];

            foreach (AnimationClip item in _anim.runtimeAnimatorController.animationClips)
            {
                if (item.name == _currentClip)
                {
                    item.SampleAnimation(_myTransform.gameObject, 0f);
                    PopulateBones(isFacingUp ? _standingFaceUpBones : _standingFaceDownBones);
                    break;
                }
            }

            _myTransform.position = posBeforeSampling;
            _myTransform.rotation = rotBeforeSampling;
        }

        private void AlignRotationToHips()
        {
            Vector3 originalHipsPosition = _hipsBone.position;
            Quaternion originalHipsRotation = _hipsBone.rotation;

            Vector3 desiredDirection = _hipsBone.up * -1;
            if (!FacingUp()) desiredDirection *= -1f;
            desiredDirection.y = 0;
            desiredDirection.Normalize();

            Quaternion fromToRotation = Quaternion.FromToRotation(_myTransform.forward, desiredDirection);
            _myTransform.rotation *= fromToRotation;

            _hipsBone.position = originalHipsPosition;
            _hipsBone.rotation = originalHipsRotation;
        }

        private void AlignPositionToHips()
        {
            Vector3 originalHipsPosition = _hipsBone.position;
            _myTransform.position = _hipsBone.position;

            Vector3 positionOffset = _standingCurrent[0].pos;
            positionOffset.y = 0;
            positionOffset = _myTransform.rotation * positionOffset;
            _myTransform.position -= positionOffset;

            if (Physics.Raycast(_myTransform.position, Vector3.down, out RaycastHit hitInfo))
            {
                _myTransform.position = new Vector3(_myTransform.position.x, hitInfo.point.y, _myTransform.position.z);
            }

            _hipsBone.position = originalHipsPosition;
        }

    }
}


