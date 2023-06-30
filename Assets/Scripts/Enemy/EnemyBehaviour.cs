using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

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
    public ParticleSystem psOnFire;
    [HideInInspector] public Transform movePoint;
    [HideInInspector] public bool hasSearched;
    public IFaction attackTarget;
    [HideInInspector] public DetectableObject detectObject;
    float _weightHit;

    #region BEHAVIOUR SPECIFIC
    [Header("Behaviour")]
    public EnemyState beginState;
    public StateMachine sm;
    [SerializeField] Transform wpParent;
    [SerializeField] float roamRadius = 10f;
    [SerializeField] Transform handGreandeSpawnPoint;
    [SerializeField] SoItem handGrenadeScriptable;
    #endregion

    #region ANIMATIONS
    [Header("Animations")]
    [SerializeField] Transform[] ragdollTransform;
    RagdollBodyPart[] _bodyParts;
    public SoItem weaponUsed;
    public GameObject muzzle;
    [SerializeField] Rig rigLookAt;
    [SerializeField] Transform ikLookAtTransform;
    [SerializeField] Rig rigRightHandAiming;
    [SerializeField] Rig rigLeftHand;
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand; //needed for accuracy (together with '_spreadWeapon')
    Transform _aimIK;
    float _spreadWeapon = 50f;
    Vector3 _offsetTar;
    [HideInInspector] public bool isHit;
    float _weightRightHandAim, _weightLeftHand;
    #endregion

    #region DEBUGS
    [Header("Debug only")]
    public bool haspath;
    public NavMeshPathStatus pathStatus;
    public float speedAnimRoot;
    public float speedAgent;
    public float remainDistance;
    public string nameOfTarget;
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region INITIALIZATION AND UNITY CALLBACKS
    public void InitAwake(EnemyRef eRef, Transform moveP, TextMeshPro displayT, out HashSet<Collider> hs/*, out DetectableObject detectObject*/)
    {
        _gm = GameManager.Instance;
        _cam = _gm.mainCam;
        _eRef = eRef;
        movePoint = moveP;
        _displayState = displayT;

        sm = new StateMachine(_displayState, _eRef, _gm.wayPointParent, null, roamRadius, (int)beginState);

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
        
        speedAnimRoot = _eRef.anim.velocity.magnitude;
        if (speedAnimRoot < 0.05f) speedAnimRoot = 0f;
        _eRef.agent.speed = speedAnimRoot;
        _eRef.animTr.SetPositionAndRotation(_eRef.agentTr.position - 0.06152725f * Vector3.up, _eRef.agentTr.rotation);

        rigRightHandAiming.weight = Mathf.MoveTowards(rigRightHandAiming.weight, _weightRightHandAim, 4f * Time.deltaTime);
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, 4f * Time.deltaTime);
        _weightHit = Mathf.MoveTowards(_weightHit, isHit ? 1f : 0f, 2f * Time.deltaTime);
        _eRef.anim.SetLayerWeight(1, _weightHit);
       // print(_weightRightHandAim);
        if (sm.currentState == sm.attackState || sm.currentState == sm._fleeState) return;
        if (_canUpdateFOV)
        {
            _eRef.fov.GetAllTargets(out attackTarget, out detectObject);
            if (attackTarget != null)
            {
                movePoint.position = attackTarget.MyTransform.position;
                sm.ChangeState(sm.attackState);
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
        if (sm.currentState == sm.immobileState || sm.currentState == sm._fleeState || attackerTr == null) return;
        if (lowHp)
        {
            if (attackerTr.TryGetComponent(out IFaction target) && EnemyRef.HostileFaction(Fact, target.Fact))
            {
                attackTarget = target;
                sm.ChangeState(sm._fleeState);
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
            _eRef.attackClass.GetLauchVelocity(attackTarget.MyTransform.position, handGreandeSpawnPoint.position));
        greande.SetActive(true);
    }
    public void ThrowMethod(bool startThrowing)
    {
        sm.attackState.isThrowing = startThrowing;
        Attack_Animation(!startThrowing);
        if (!startThrowing)
        {
            _offsetTar = multiAimConstraintRightHand.data.offset = Vector3.zero;
        }
    }

    #endregion

    #region ANIMATIONS
    public void IdelLookAround_Animation(bool active, float angle)
    {
        rigLookAt.weight = Mathf.Lerp(rigLookAt.weight, active ? 1f : 0f, 3f * Time.deltaTime);
        ikLookAtTransform.localEulerAngles = angle * Vector3.up;
    }
    void GetIK_Animation(bool attak)
    {
        _weightRightHandAim = _weightLeftHand = 0f;
        if (isHit) return;

        switch (weaponUsed.enemyWeaponUsed)
        {
            case EnemyWeaponUsed.Melee:
                _weightRightHandAim = attak ? 1f : 0f;
                break;
            case EnemyWeaponUsed.Pistol:
                _weightRightHandAim = _weightLeftHand = attak ? 1f : 0f;
                break;
            case EnemyWeaponUsed.Rifle:
                _weightRightHandAim = attak ? 1f : 0f;
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

    public void ResetAllWeights()
    {
        rigRightHandAiming.weight = rigLeftHand.weight = 0f;
    }
    #endregion



}


