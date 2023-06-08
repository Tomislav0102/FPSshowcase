using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Animations.Rigging;
using Sirenix.OdinInspector;
using TMPro;


public class EnemyRef : GlobalEventManager
{
    GameManager _gm;
    [SerializeField] TextMeshPro displayState;
    public EnemyBehaviour enemyBehaviour;
    [SerializeField] Transform movePoint;
    public NavMeshAgent agent;
    [HideInInspector] public Transform agentTr;
    public AttackClass attackClass;
    public IFactionTarget myFactionInterface;
    public HashSet<Collider> _allColliders = new HashSet<Collider>();
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform animTr;
    [HideInInspector] public DetectableObject detectableObject;
    [HideInInspector] public IFactionTarget detectableFaction;
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
        myFactionInterface = enemyBehaviour.GetComponent<IFactionTarget>();
        agentTr = agent.transform;
        anim = enemyBehaviour.GetComponent<Animator>();
        animTr = enemyBehaviour.transform;
        enemyBehaviour.InitAwake(this, movePoint, displayState, out _allColliders, out detectableObject);
        detectableObject.HookInterface(myFactionInterface);
        detectableFaction = detectableObject.GetComponent<IFactionTarget>();
        _allColliders.Add(detectableObject.GetComponent<Collider>());
        fov.Init(this, consoleDisplay);

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        enemyHealth.Dead += MyDeath;
        attackClass = new AttackClass(_allColliders, myFactionInterface);
        attackClass.bulletSpawnPosition = enemyBehaviour.muzzle.transform;

    }
    protected override void OnDisable()
    {
        base.OnDisable();
        enemyHealth.Dead -= MyDeath;
    }
    protected override void CallEv_PlayerDead()
    {
        if (enemyBehaviour.AttackTarget != null && enemyBehaviour.AttackTarget == _gm.player.GetComponent<IFactionTarget>())
        {
            enemyBehaviour.AttackTarget = null;
        }
        base.CallEv_PlayerDead();
    }
    void MyDeath()
    {
        detectableObject.gameObject.SetActive(false);
        enemyBehaviour.gameObject.SetActive(false);
        anim.enabled = false;
    }
}

[System.Serializable]
public class FieldOvView
{
    GameManager _gm;
    EnemyRef _eRef;
    EnemyBehaviour _enemyBehaviour;
    IFactionTarget _myIFactionTarget;
    Transform _myTransform;
    [SerializeField] Transform sightSphere, hearSphere;
    float _sightRange, _hearingRange;
    [SerializeField] float sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    Collider[] _colls = new Collider[30];
    RaycastHit[] _multipleHits = new RaycastHit[1];

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
    public bool TargetStillVisible(IFactionTarget target, LayerMask layerMask)
    {
        _ray.direction = (target.MyTransform.position - _myTransform.position).normalized;
        for (int i = 0; i < 2; i++)
        {
            _ray.origin = _myTransform.position + (i + 0.6f) * Vector3.up;
            if (Physics.Raycast(_ray, out _hit, EffectiveRange(target.MyTransform.position), layerMask, QueryTriggerInteraction.Ignore))
            {
                if (_eRef._allColliders.Contains(_hit.collider)) continue;
                if (_hit.collider == target.MyTransform.GetComponent<Collider>()) return true;
            }
        }
        Debug.Log($"I am {_eRef.name} and {target.MyTransform.name} is not visible, but in range");
        return false;
    }
    public IFactionTarget FindFovTargets()
    {
        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, _gm.layFOV_Overlap, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < num; i++)
        {
            IFactionTarget target = _colls[i].GetComponent<IFactionTarget>();

            if (target == null || 
              //  target == _myIFactionTarget || 
                (target.Owner != null && target.Owner == _myIFactionTarget) ||
                !TargetStillVisible(target, _gm.layFOV_Ray)) continue;

           // Debug.Log(_eRef.name + " ---- " + target.MyTransform.name);
            switch (_myIFactionTarget.Fact)
            {
                case Faction.Enemy:
                    switch (target.Fact)
                    {
                        case Faction.Enemy:
                            Debug.Log("ksdfjklsdjf");

                            return BuddysFoe(target);

                        default:
                         //   Debug.Log(target.MyTransform.name);
                            return Foe(target);
                    }

                case Faction.Ally:
                    switch (target.Fact)
                    {
                        case Faction.Enemy:
                            return Foe(target);

                        case Faction.Ally:
                            return BuddysFoe(target);
                    }
                    break;
            }



        }
        return null;

        IFactionTarget BuddysFoe(IFactionTarget tar)
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
        IFactionTarget Foe(IFactionTarget tar)
        {
            _enemyBehaviour.EnState = EnemyState.Attack;
            return tar.Owner ?? tar;
        }

    }


}



//public bool TargetStillVisible(IFactionTarget target, LayerMask layerMask)
//{
//    _ray.direction = (target.MyTransform.position - _myTransform.position).normalized;
//    for (int i = 0; i < 2; i++)
//    {
//        _ray.origin = _myTransform.position + (i + 0.6f) * Vector3.up;
//        if (Physics.Raycast(_ray, out _hit, EffectiveRange(target.MyTransform.position), layerMask, QueryTriggerInteraction.Ignore))
//        {
//            // Debug.Log($"{target.MyTransform.GetComponent<Collider>()} is target");
//            if (!_eRef._allColliders.Contains(_hit.collider) && _hit.collider == target.MyTransform.GetComponent<Collider>()) return true;
//            //  else Debug.Log($"{_hit.collider.name} is blocking");
//        }
//    }
//    //  Debug.Log($"{target.MyTransform.name} is not visible, but in range");
//    return false;
//}
