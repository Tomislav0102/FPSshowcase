using DG.Tweening;
using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleMovePlayerNavMesh : MonoBehaviour, IFactionTarget
{
    public Transform MyTransform { get => _agent.transform; set { } }
    [field: SerializeField] public Faction Fact { get; set; }
    public Transform MyHead { get => _agent.transform; set { } }
    Camera _cam;
    NavMeshAgent _agent;
    RaycastHit _hit;
    Animator _anim;
    public bool hasPath;

    public float AgentSpeed()
    {
        return _agent.velocity.magnitude;
    }

    private void Awake()
    {
        _cam = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _agent.updatePosition = false;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out _hit, Mathf.Infinity))
            {
              //  if(_agent.hasPath) _agent.ResetPath();
                _agent.SetDestination(_hit.point);
            }
        }

        Vector3 worldDelatPos = _agent.nextPosition - transform.position;

        if (worldDelatPos.magnitude > _agent.radius)
        {
            _agent.nextPosition = transform.position + 0.9f * worldDelatPos;
        }

        bool shouldMove = _agent.remainingDistance > _agent.radius;
        if (!shouldMove && _agent.hasPath) _agent.ResetPath(); 

        _anim.SetBool("move", shouldMove);
        hasPath = _agent.hasPath;
    }

    void OnAnimatorMove()
    {
        Vector3 pos = _anim.rootPosition;
        pos.y = _agent.nextPosition.y;
        transform.position = pos;
    }

}
