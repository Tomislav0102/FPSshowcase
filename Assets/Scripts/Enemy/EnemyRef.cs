using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Animations.Rigging;
using Sirenix.OdinInspector;
using TMPro;
using static UnityEngine.GraphicsBuffer;


public class EnemyRef : GlobalEventManager
{
    GameManager _gm;
    [SerializeField] TextMeshPro displayState;
    public EnemyBehaviour enemyBehaviour;
    [SerializeField] Transform movePoint;
    public NavMeshAgent agent;
    [HideInInspector] public Transform agentTr;
    public AttackClass attackClass;
    public IFaction myFactionInterface;
    public HashSet<Collider> _allColliders = new HashSet<Collider>();
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform animTr;
    //  [HideInInspector] public DetectableObject detectableObject;
    public HealthEnemy enemyHealth;

    [BoxGroup("Field of view")]
    [HideLabel]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public FieldOvView fov;

    [Title("Debug only")]
    public bool consoleDisplay;


    private void Awake()
    {
        _gm = GameManager.Instance;
        myFactionInterface = enemyBehaviour.GetComponent<IFaction>();
        agentTr = agent.transform;
        anim = enemyBehaviour.GetComponent<Animator>();
        animTr = enemyBehaviour.transform;
        enemyBehaviour.InitAwake(this, movePoint, displayState, out _allColliders/*, out detectableObject*/);
        //detectableObject.HookInterface(myFactionInterface);
        //_allColliders.Add(detectableObject.GetComponent<Collider>());
        fov.Init(this, consoleDisplay);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        enemyHealth.Dead += MyDeath;
        attackClass = new AttackClass(myFactionInterface);
        attackClass.bulletSpawnPosition = enemyBehaviour.muzzle.transform;

    }
    protected override void OnDisable()
    {
        base.OnDisable();
        enemyHealth.Dead -= MyDeath;
    }
    protected override void CallEv_PlayerDead()
    {
        if (/*enemyBehaviour.AttackTarget != null &&*/ enemyBehaviour.attackTarget == _gm.plFaction)
        {
            enemyBehaviour.attackTarget = null;
        }
        base.CallEv_PlayerDead();
    }
    void MyDeath()
    {
        //  detectableObject.gameObject.SetActive(false);
        myFactionInterface.MyCollider.enabled = false;
        enemyBehaviour.enabled = false;
        agent.enabled = false;
        anim.enabled = false;
    }

    public static bool HostileFaction(Faction myFaction, Faction attackerFaction)
    {
        if (myFaction == attackerFaction) return false;

        switch (attackerFaction)
        {
            case Faction.Player:
                if (myFaction == Faction.Enemy) return true;
                break;
            case Faction.Enemy:
                if (myFaction == Faction.Ally) return true;
                break;
            case Faction.Ally:
                if (myFaction == Faction.Enemy) return true;
                break;
        }

        return false;
    }

}

[System.Serializable]
public class FieldOvView
{
    GameManager _gm;
    EnemyRef _eRef;
    EnemyBehaviour _enemyBehaviour;
    IFaction _myIFactionTarget;
    Transform _myTransform;
    [SerializeField] Transform sightSphere, hearSphere;
    float _sightRange, _hearingRange;
    [SerializeField] float sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    Collider[] _colls = new Collider[30];

    List<IFaction> _allChars = new List<IFaction>();
    List<DetectableObject> _allDetects = new List<DetectableObject>();
    IFaction _currTarget;
    DetectableObject _currDetect;


    bool _conseoleDisplay;
    public void Init(EnemyRef eRef, bool consoleDis)
    {
        _gm = GameManager.Instance;
        _eRef = eRef;
        _enemyBehaviour = _eRef.enemyBehaviour;
        _myIFactionTarget = _eRef.myFactionInterface;
        _myTransform = _myIFactionTarget.MyTransform;
        _sightRange = sightSphere.localScale.x * 0.5f;
        _hearingRange = hearSphere.localScale.x * 0.5f;
        sightSphere.gameObject.SetActive(false);
        hearSphere.gameObject.SetActive(false);
        _sightAngleTrigonometry = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
        _conseoleDisplay = consoleDis;

        _enemyBehaviour.InitAttackRange(_sightRange);
    }

    float EffectiveRange(Vector3 targetPos)
    {
        float r = _sightRange;
        if (Vector3.Dot(_myTransform.forward, (targetPos - _myTransform.position).normalized) < _sightAngleTrigonometry)
        {
            r = _hearingRange;
        }
        return r;
    }
    public bool TargetVisible(Transform targetTr, Collider targetColl, LayerMask layerMask)
    {
        _ray.direction = (targetTr.position - _myTransform.position).normalized;
        for (int i = 0; i < 2; i++)
        {
            _ray.origin = _myTransform.position + (i + 0.6f) * Vector3.up;
            if (Physics.Raycast(_ray, out _hit, EffectiveRange(targetTr.position), layerMask, QueryTriggerInteraction.Ignore))
            {
                if (_eRef._allColliders.Contains(_hit.collider))
                {
                    Debug.Log(_hit.collider.name);
                    continue;
                }
                if (_hit.collider == targetColl) return true;
              //  else Debug.Log($"I am {_eRef.name} and {_hit.collider.name} is blocking");
                //   else Debug.Log($"Hit coll is {_hit.collider.GetHashCode()} and target should be {tr.GetComponent<Collider>().GetHashCode()}");
                //  else Debug.Log($"Hit coll is {_hit.collider.name} and target should be {tr.GetComponent<Collider>().name}");
                // else Debug.Log($"Hit coll is {_hit.collider.transform.position} and target should be {tr.GetComponent<Collider>().transform.position}");
                //else
                //{
                //    _hit.collider.transform.position = 10f * Vector3.forward;
                //  //  tr.GetComponent<Collider>().transform.position = 12f * Vector3.forward;
                //}
            }
        }
        //  Debug.Log($"I am {_eRef.name} and {ent.MyTransform.name} is not visible, but in range");
        return false;
    }
    public void GetAllTargets(out IFaction tarCharacter, out DetectableObject tarDetectable)
    {
        IFaction character = null;
        DetectableObject detect = null;

        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, _gm.layFOV_Overlap, QueryTriggerInteraction.Ignore);
        _allChars.Clear();
        _allDetects.Clear();
        for (int i = 0; i < num; i++)
        {
            if (_colls[i].TryGetComponent(out IFaction ifa)) _allChars.Add(ifa);
            else if (_colls[i].TryGetComponent(out DetectableObject det)) _allDetects.Add(det);
        }

        for (int i = 0; i < _allChars.Count; i++)
        {
            _currTarget = _allChars[i];
            if (_currTarget == null ||
            _currTarget == _myIFactionTarget ||
            !TargetVisible(_currTarget.MyTransform, _currTarget.MyCollider, _gm.layFOV_Ray)) continue;

            if (EnemyRef.HostileFaction(_myIFactionTarget.Fact, _currTarget.Fact))
            {
                character = _currTarget;
            }
            else if (_currTarget.MyTransform.TryGetComponent(out EnemyBehaviour en))
            {
                switch (en.EnState)
                {
                    case EnemyState.Attack:
                        character = en.attackTarget;
                        break;
                    case EnemyState.Search:
                        if (_enemyBehaviour.EnState == EnemyState.Search || _enemyBehaviour.hasSearched)
                        {
                            character = null;
                        }
                        else
                        {
                            detect = en.detectObject;
                        }
                        break;
                }
            }

            if (character != null) break;
        }

        for (int i = 0; i < _allDetects.Count; i++)
        {
            if (character != null) break;

            _currDetect = _allDetects[i];
            if (_currDetect == null ||
                _currDetect.owner == null ||
                _currDetect.owner == _myIFactionTarget ||
                !EnemyRef.HostileFaction(_myIFactionTarget.Fact, _currDetect.owner.Fact) ||
                !TargetVisible(_currDetect.myTransform, _currDetect.myCollider, _gm.layFOV_RayAll)) continue;

            detect = _currDetect;
            break;
        }

        tarCharacter = character;
        tarDetectable = detect;
    }
}


    /*
    public IFaction FindFovTargets()
    {
        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, _gm.layFOV_Overlap, QueryTriggerInteraction.Ignore);


        List<IFaction> allChars = new List<IFaction>();
        List<IFaction> allDetects = new List<IFaction>();
        for (int i = 0; i < num; i++)
        {
            _currTarget = _colls[i].GetComponent<IFaction>();
            if (_currTarget == null ||
            _currTarget == _myIFactionTarget) continue;

            if (EnemyRef.HostileFaction(_myIFactionTarget.Fact, _currTarget.Fact)) allChars.Add(_currTarget);
        }

        for (int i = 0; i < allChars.Count; i++)
        {
            _currTarget = allChars[i];
           // if (!TargetStillVisible(_currTarget, _gm.layFOV_Ray)) continue;

            return FinalTarget(_currTarget);
        }
        //for (int i = 0; i < allDetects.Count; i++)
        //{
        //    _currTarget = allDetects[i];
        //    if (!TargetStillVisible(_currTarget, _gm.layFOV_RayAll)) continue;

        //    return FinalTarget(_currTarget.Owner);
        //}

        return null;

        IFaction FinalTarget(IFaction tar)
        {
            switch (_myIFactionTarget.Fact)
            {
                case Faction.Enemy:
                    switch (tar.Fact)
                    {
                        case Faction.Enemy:
                            return BuddysFoe(tar);

                        default:
                            return Foe(tar);
                    }

                case Faction.Ally:
                    switch (tar.Fact)
                    {
                        case Faction.Enemy:
                            return Foe(tar);

                        case Faction.Ally:
                            return BuddysFoe(tar);
                    }
                    break;
            }

            return null;

            IFaction BuddysFoe(IFaction tar)
            {
                if (tar.MyTransform.TryGetComponent(out EnemyBehaviour en))
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

                return null;
            }
            IFaction Foe(IFaction tar)
            {
                _enemyBehaviour.EnState = EnemyState.Attack;
                return tar;
            }

        }


    }*/


//   Debug.Log($"I am {_eRef.name} and {target.MyTransform.name} is my traget");


/*
public bool TargetStillVisible(IFaction ent, LayerMask layerMask)
{
    _ray.direction = (ent.MyTransform.position - _myTransform.position).normalized;
    for (int i = 0; i < 2; i++)
    {
        _ray.origin = _myTransform.position + (i + 0.6f) * Vector3.up;
        if (Physics.Raycast(_ray, out _hit, EffectiveRange(ent.MyTransform.position), layerMask, QueryTriggerInteraction.Ignore))
        {
            if (_eRef._allColliders.Contains(_hit.collider))
            {
                Debug.Log(_hit.collider.name);
                continue;
            }
            if (_hit.collider == ent.MyCollider) return true;
            // else Debug.Log($"I am {_eRef.name} and {_hit.collider.name} is blocking");
            //   else Debug.Log($"Hit coll is {_hit.collider.GetHashCode()} and target should be {tr.GetComponent<Collider>().GetHashCode()}");
            //  else Debug.Log($"Hit coll is {_hit.collider.name} and target should be {tr.GetComponent<Collider>().name}");
            // else Debug.Log($"Hit coll is {_hit.collider.transform.position} and target should be {tr.GetComponent<Collider>().transform.position}");
            //else
            //{
            //    _hit.collider.transform.position = 10f * Vector3.forward;
            //  //  tr.GetComponent<Collider>().transform.position = 12f * Vector3.forward;
            //}
        }
    }
    // Debug.Log($"I am {_eRef.name} and {ent.MyTransform.name} is not visible, but in range");
    return false;
}
public IFaction FindFovTargets()
{
    int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, _gm.layFOV_Overlap, QueryTriggerInteraction.Ignore);

    for (int i = 0; i < num; i++)
    {
        IFaction target = _colls[i].GetComponent<IFaction>();

        if (target == null ||
            target == _myIFactionTarget ||
            (target.Owner != null && target.Owner == _myIFactionTarget)) continue;
        //   !TargetStillVisible(target, _gm.layFOV_Ray)) continue;

        if (target.Owner == null)
        {

        }
        else
        {
            return target.Owner;
        }


        //   Debug.Log($"I am {_eRef.name} and {target.MyTransform.name} is my traget");
        switch (_myIFactionTarget.Fact)
        {
            case Faction.Enemy:
                switch (target.Fact)
                {
                    case Faction.Enemy:
                        IFaction tar = BuddysFoe(target);
                        if (tar == null) continue;
                        else return BuddysFoe(target);

                    default:
                        //  Debug.Log(target.MyTransform.name);
                        return Foe(target);

                }
            case Faction.Ally:
                switch (target.Fact)
                {
                    case Faction.Enemy:
                        return Foe(target);

                    case Faction.Ally:
                        IFaction tar = BuddysFoe(target);
                        if (tar == null) continue;
                        else return BuddysFoe(target);

                }
                break;
        }



    }
    return null;

    IFaction BuddysFoe(IFaction tar)
    {
        if (tar.Owner.MyTransform.TryGetComponent(out EnemyBehaviour en))
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

        return null;
    }
    IFaction Foe(IFaction tar)
    {
        _enemyBehaviour.EnState = EnemyState.Attack;
        return tar;
    }
}
*/
