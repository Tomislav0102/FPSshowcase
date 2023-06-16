using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
    public IFaction myFactionInterface;
    public HashSet<Collider> _allColliders = new HashSet<Collider>();
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform animTr;
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
        enemyBehaviour.InitAwake(this, movePoint, displayState, out _allColliders);
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
        if (enemyBehaviour.attackTarget != null && enemyBehaviour.attackTarget == _gm.plFaction)
        {
            enemyBehaviour.attackTarget = null;
        }
        base.CallEv_PlayerDead();
    }
    void MyDeath()
    {
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
    public static Vector3 GetRdnPos(Vector3 center, float radius)
    {
        Vector3 pos = center + radius * Random.insideUnitSphere;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f * radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return pos;
    }

    public bool ReadyToMove() => !agent.pathPending && agent.remainingDistance <= 0.5f;

}


