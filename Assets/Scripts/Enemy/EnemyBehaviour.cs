using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using Sirenix.OdinInspector;

public class EnemyBehaviour : MonoBehaviour, IFaction
{
    #region INTERFACE
    [field: SerializeField] public Transform MyTransform { get; set; }
    [field: SerializeField] public Collider MyCollider { get; set; }
    [field: SerializeField] public Transform MyHead { get; set; }
    [field: SerializeField] public Faction Fact { get; set; }
    #endregion

    GameManager _gm;
    EnemyRef _eRef;
    TextMeshPro _displayState;
    Camera _cam;
    bool _canUpdateFOV, _canUpdateDestination;
    public RagToAnimTranstions ragToAnimTransition;
    [HideInInspector] public Transform movePoint;
    [HideInInspector] public bool hasSearched;
    public IFaction attackTarget;
    [HideInInspector] public DetectableObject detectObject;
    [PropertySpace(SpaceAfter = 10, SpaceBefore = 0)]
    public ParticleSystem psOnFire;
    public bool animFollowsAgent = true;

    #region BEHAVIOUR SPECIFIC
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    public EnemyState beginState;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    public StateMachine sm;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    [SerializeField] Transform wpParent;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    [SerializeField] float roamRadius = 10f;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    [SerializeField] Transform handGreandeSpawnPoint;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    [SerializeField] SoItem handGrenadeScriptable;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    [SerializeField] int handGrenadeCount = 3;
    [BoxGroup("Behaviour")]
    [GUIColor("green")]
    /*[HideInInspector]*/ public Cover coverObject;
    #endregion

    #region ANIMATIONS
    float _weightHit;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    [SerializeField] Transform[] ragdollTransform;
    RagdollBodyPart[] _bodyParts;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    public SoItem weaponUsed;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    [SerializeField] GameObject weaponMesh;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    public GameObject muzzle;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    [SerializeField] Rig rigAiming;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    [SerializeField] Rig rigLeftHand;
    [BoxGroup("Animations")]
    [GUIColor("blue")]
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand; 
    Transform _aimIK;
    float _spreadWeapon = 50f;
    Vector3 _offsetTar;
    [HideInInspector] public bool isHit;
    float _weightRightHandAim, _weightLeftHand;
    #endregion

    #region DEBUGS
    [FoldoutGroup("Debugs")]
    public bool haspath;
    [FoldoutGroup("Debugs")]
    public NavMeshPathStatus pathStatus;
    [FoldoutGroup("Debugs")]
    public float speedAnimRoot;
    [FoldoutGroup("Debugs")]
    public float speedAgent;
    [FoldoutGroup("Debugs")]
    public float remainDistance;
    [FoldoutGroup("Debugs")]
    public string nameOfTarget;
    [FoldoutGroup("Debugs")]
    public string animName;
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region INITIALIZATION AND UNITY CALLBACKS
    public void InitAwake(EnemyRef eRef, Transform moveP, TextMeshPro displayT, out HashSet<Collider> hs)
    {
        _gm = GameManager.Instance;
        _cam = _gm.mainCam;
        _eRef = eRef;
        movePoint = moveP;
        _displayState = displayT;

        sm = new StateMachine(_displayState, _eRef, _gm.wayPointParent, null, roamRadius, (int)beginState, handGrenadeCount);

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

        switch (weaponUsed.enemyWeaponUsed)
        {
            case EnemyWeaponUsed.Pistol:
                SetIK_HoldWeapon(false);
                break;
            case EnemyWeaponUsed.Rifle:
                SetIK_HoldWeapon(true);
                break;
        }
        SetIK_AimWeapon(false);
    }
    void OnEnable()
    {
        InvokeRepeating(nameof(CanUpdateFOVMethod), Random.Range(0.3f, 1f), 0.3f);
    }
    void OnDisable()
    {
        attackTarget = null;
        detectObject = null;
        CancelInvoke();
        _displayState.text = "Dead";
    }

    private void Update()
    {
        if (sm.currentState == null) return;
        Debugs();

        sm.currentState.UpdateLoop();

        if (sm.currentState == sm.immobileState) return;

        rigAiming.weight = Mathf.MoveTowards(rigAiming.weight, _weightRightHandAim, 4f * Time.deltaTime);
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, 4f * Time.deltaTime);
        _weightHit = Mathf.MoveTowards(_weightHit, isHit ? 1f : 0f, 5f * Time.deltaTime);
        _eRef.anim.SetLayerWeight(1, _weightHit);

        speedAnimRoot = _eRef.anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        _eRef.agent.speed = speedAnimRoot;
        if(animFollowsAgent) _eRef.animTr.SetPositionAndRotation(_eRef.agentTr.position - 0.06152725f * Vector3.up, _eRef.agentTr.rotation);


        if (!_canUpdateFOV ||
            sm.currentState == sm.attackState ||
          //  sm.currentState == sm.coverState ||
            sm.currentState == sm.fleeState) return;

        bool frienDetectsEnemy = false;
        _eRef.fov.GetAllTargets(out attackTarget, out detectObject, ref frienDetectsEnemy);
        if (sm.currentState == sm.coverState) return;
        if (attackTarget != null)
        {
            movePoint.position = attackTarget.MyTransform.position;
            if (sm.currentState == sm.searchState) sm.ChangeState(sm.attackState);
            else if (frienDetectsEnemy) sm.ChangeState(sm.searchState);
            else sm.ChangeState(sm.detectingEnemyState);
                
        }
        else if (detectObject != null)
        {
            if (!hasSearched)
            {
                movePoint.position = detectObject.owner.MyTransform.position;
                sm.ChangeState(sm.searchState);
            }
        }

        _canUpdateFOV = false;
    }
    #endregion

    #region MISCELLANEOUS
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

    public void PassFromHealth_Attacked(Transform attackerTr, bool switchAgro, bool lowHp)
    {
        if (sm.currentState == sm.immobileState || sm.currentState == sm.fleeState || attackerTr == null) return;
        if (lowHp)
        {
            if (attackerTr.TryGetComponent(out IFaction target) && EnemyRef.HostileFaction(Fact, target.Fact))
            {
                attackTarget = target;
                sm.ChangeState(sm.fleeState);
            }
            return;
        }

        if (sm.currentState == sm.attackState )
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
                sm.ChangeState(sm.searchState);
            }
        }
    }
    public void AE_Attacking()
    {
        if (isHit) return;

        if (attackTarget == null)
        {
            sm.ChangeState(sm.searchState);
            return;
        }

        muzzle.SetActive(false);
        muzzle.SetActive(true);
        _eRef.attackClass.Attack(weaponUsed);

    }
    public void AE_ThrowGrenade()
    {
        GameObject greande = _gm.poolManager.GetProjectile(handGrenadeScriptable.ammoType);
        greande.GetComponent<ProjectilePhysical>().IniThrowable(handGreandeSpawnPoint, ragdollTransform[10].GetComponent<Collider>(),
            _eRef.attackClass.GetLauchVelocity(movePoint.position, handGreandeSpawnPoint.position));
        greande.SetActive(true);
    }
    public void ThrowMethod(bool startThrowing)
    {
        weaponMesh.SetActive(!startThrowing);
        sm.attackState.isThrowing = startThrowing;
        if(sm.currentState == sm.attackState) Attack_Animation(!startThrowing);
        if (!startThrowing)
        {
            _offsetTar = multiAimConstraintRightHand.data.offset = Vector3.zero;
        }
    }

    #endregion

    #region ANIMATIONS
    bool CantChangeIK() => isHit || weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee;
    void SetIK_HoldWeapon(bool hold)
    {
        if (CantChangeIK()) return;
        _weightLeftHand = hold ? 1f : 0f;
    }
    public void SetIK_LookAt(bool look)
    {
        if (CantChangeIK()) return;
        _weightRightHandAim = look ? 1f : 0f;
        multiAimConstraintRightHand.weight = 0f;
        if (look && attackTarget != null) _aimIK.position = attackTarget.MyHead.position;
    }
    void SetIK_AimWeapon(bool aim)
    {
        if (CantChangeIK()) return;
        _weightRightHandAim = multiAimConstraintRightHand.weight = aim ? 1f : 0f;
    }
    public void SetIK_ResetAll()
    {
        _weightRightHandAim = _weightLeftHand = rigAiming.weight = rigLeftHand.weight = multiAimConstraintRightHand.weight = 0f;
        _eRef.anim.SetLayerWeight(1, 0);
    }
    public void Attack_Animation(bool isAttacking)
    {
        SetIK_AimWeapon(isAttacking);
        _eRef.anim.SetBool("attack", isAttacking);
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee || !isAttacking) return;
        _offsetTar = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));
    }
    public void SetAim_Animation()
    {
        _aimIK.position = attackTarget.MyHead.position - 0.1f * Vector3.up;
        if (weaponUsed.enemyWeaponUsed == EnemyWeaponUsed.Melee) return;

        //multiAimConstraintRightHand.data.offset =
        //    Vector3.Lerp(multiAimConstraintRightHand.data.offset, _offsetTar, 0.3f * Time.deltaTime);

    }
    public void SetSpeed_Animation(MoveType movetype) => _eRef.anim.SetInteger("movePhase", (int)movetype);

    #endregion



}


