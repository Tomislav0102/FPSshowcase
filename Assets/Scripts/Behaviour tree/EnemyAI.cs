using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    NavMeshAgent _agent;
    [SerializeField] float startingHealth, lowHealthTreshold, regenerationRate;
    [Space]
    [SerializeField] float chaseRange;
    [SerializeField] float attackRange;
    [SerializeField] MeshRenderer meshRenderer;

    [SerializeField] Transform playerTransform;
    [SerializeField] Cover[] covers;
    public Transform bestSpot;

    Node _topNode;
    public float CurrentHealth
    {
        get { return _currentHealth; }
        set { _currentHealth = Mathf.Clamp(value, 0, startingHealth); }
    }
    float _currentHealth;
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        CurrentHealth = startingHealth;
        ConstructBehaviourTree();
    }
    private void Update()
    {
        _topNode.Evaluate();
        if(_topNode.nodeState == NodeState.Failure)
        {
            SetColor(Color.red);
            _agent.isStopped = true;
        }
        // CurrentHealth += Time.deltaTime * regenerationRate;

        if (Input.GetMouseButtonDown(1)) CurrentHealth = 10f;
    }

    void ConstructBehaviourTree()
    {
        var isCoverAvailableNode = new IsCoverAvailableNode(covers, playerTransform, this);
        var goToCoverSpotNode = new GoToCoverSpotNode(_agent, this);
        var healthNode = new HealthNode(this, lowHealthTreshold);
        var isCoveredNode = new IsCoveredNode(playerTransform, transform);
        var chaseNode = new ChaseNode(playerTransform, _agent, this);
        var chaseRangeNode = new RangeNode(chaseRange, playerTransform, transform);
        var shootRangeNode = new RangeNode(attackRange, playerTransform, transform);
        var shootNode = new ShootNode(_agent, this);


        Sequence chaseSeq = new Sequence(new List<Node> { chaseRangeNode, chaseNode });
        Sequence shootSeq = new Sequence(new List<Node> { shootRangeNode, shootNode });

        Sequence goToCoverSeq= new Sequence(new List<Node> { isCoverAvailableNode, goToCoverSpotNode });
        Selector findCoverSelector= new Selector(new List<Node> { goToCoverSeq, chaseNode });
        Selector tryToTakeCoverSelector = new Selector(new List<Node> { isCoveredNode, findCoverSelector });
        Sequence mainCoverSeq = new Sequence(new List<Node> { healthNode, tryToTakeCoverSelector });

        _topNode = new Selector(new List<Node> { mainCoverSeq, shootSeq, chaseSeq });

    }
    public void SetColor(Color boja)
    {
        meshRenderer.material.color = boja;
    }
}
 