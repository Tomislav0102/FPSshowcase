using DG.Tweening;
using FirstCollection;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleMovePlayerNavMesh : MonoBehaviour/*, IFactionTarget*/
{
    public Transform MyTransform { get => _agent.transform; set { } }
    [field: SerializeField] public Faction Fact { get; set; }
    public Transform MyHead { get => _agent.transform; set { } }
    Camera _cam;
    NavMeshAgent _agent;
    RaycastHit _hit;
    Animator _anim;

    public Transform tar;
    [SerializeField] float _roamRadius = 10f;
    Vector3 _startPos;
    public Vector3[] _pos = new Vector3[0];
    Vector3 GetRdnPos(Vector3 center)
    {
        Vector3 pos = center + _roamRadius * Random.insideUnitSphere;
        if (NavMesh.SamplePosition(pos, out _navHit, 2f * _roamRadius, NavMesh.AllAreas))
        {
            return _navHit.position;
        }

        return pos;
    }
    NavMeshHit _navHit;
    public bool isPlayer;
    [Title("Debug only")]
    public bool haspath;
    public float worldMagnitude;
    public Vector2 _vel, _smothDeltaPos;
    public float velMagnitude;
    public float speedCurrent;
    public float remainDistance;
    public Vector3 avatarVel;
    public float avatarSpeed;
    public Vector3 steering;

    public float AgentSpeed()
    {
        return _agent.velocity.magnitude;
    }

    private void Awake()
    {
        _cam = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        //_agent.updatePosition = false;
        //_agent.updateRotation = false;
        _startPos=transform.position;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && isPlayer)
        {
            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out _hit, Mathf.Infinity))
            {
                tar.position = _hit.point;
                _agent.SetDestination(_hit.point);
            }
        }

        _agent.speed = _anim.velocity.magnitude;
       // RoamBehaviour(_startPos);

        bool shouldMove = _agent.remainingDistance > _agent.stoppingDistance;
        _anim.SetBool("move", shouldMove);
       // if (!shouldMove && _agent.hasPath) _agent.ResetPath();
        //if (!shouldMove) _agent.ResetPath();
        //_pos = _agent.path.corners;
        //if (_agent.path.corners.Length > 1)
        //{
        //    Vector3 dir = _pos[1] - _pos[0];
        //    dir.y = 0f;
        //    dir.Normalize();
        //    if (_agent.hasPath) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        //}
        //  if(!_agent.hasPath) _agent.SetDestination(tar.position);


        //Vector3 worldDelatPos = _agent.nextPosition - transform.position;
        //worldDelatPos.y = 0f;
        //worldDelatPos.Normalize();

        //float dx = Vector3.Dot(transform.right, worldDelatPos);
        //float dz = Vector3.Dot(transform.forward, worldDelatPos);
        //Vector2 deltaPos = new Vector2(dx, dz);

        //float smooth = Mathf.Min(1f, Time.deltaTime / 0.1f);
        //_smothDeltaPos = Vector2.Lerp(_smothDeltaPos, deltaPos, smooth);
        //_vel = _smothDeltaPos / Time.deltaTime;
        //if (_agent.remainingDistance <= _agent.stoppingDistance)
        //{
        //    _vel = Vector2.Lerp(Vector2.zero, _vel, _agent.remainingDistance / _agent.stoppingDistance);
        //}

        //bool shouldMove = _vel.magnitude > 0.1f && _agent.remainingDistance > _agent.stoppingDistance;
        //_anim.SetBool("move", shouldMove);

        //  worldMagnitude = worldDelatPos.magnitude;
        //if (worldMagnitude > _agent.radius / 2f)
        //{
        //    transform.position = Vector3.Lerp(_anim.rootPosition, _agent.nextPosition, smooth);
        //}


        haspath = _agent.hasPath;
        //speedCurrent = _agent.velocity.magnitude;
        remainDistance = _agent.remainingDistance;
        //velMagnitude = _vel.magnitude;
        //avatarVel = _anim.velocity;
        //avatarSpeed = _anim.velocity.magnitude;
        //steering = _agent.steeringTarget;
    }
    void RoamBehaviour(Vector3 center)
    {
        if (_agent.remainingDistance < 1f) tar.position = GetRdnPos(center);
        //  if (Vector3.SqrMagnitude(_animTr.position - _movePoint.position) < 2f) _movePoint.position = GetRdnPos(center);
       // GoToTarget();
    }

    //void OnAnimatorMove()
    //{
    //    Vector3 pos = _anim.rootPosition;
    //    pos.y = _agent.nextPosition.y;
    //    transform.position = pos;
    //    _agent.nextPosition = pos;
    //}

}
