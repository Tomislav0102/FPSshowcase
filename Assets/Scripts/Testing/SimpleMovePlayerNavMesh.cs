using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleMovePlayerNavMesh : MonoBehaviour, IFaction
{
    [field: SerializeField] public Transform MyTransform { get; set; }
    [field: SerializeField] public Faction Fact { get; set; }
    [field: SerializeField] public Transform MyHead { get; set; }
    [field: SerializeField] public Collider MyCollider { get; set; }
    Camera _cam;
    Vector3 _camOffset;
    NavMeshAgent _agent;
    RaycastHit _hit;
    Animator _anim;

    Vector3 GetRdnPos(Vector3 center, float radius)
    {
        Vector3 pos = center + radius * Random.insideUnitSphere;
        if (NavMesh.SamplePosition(pos, out _navHit, 2f * radius, NavMesh.AllAreas))
        {
            return _navHit.position;
        }

        return pos;
    }
    NavMeshHit _navHit;

    public float AgentSpeed()
    {
        return _agent.velocity.magnitude;
    }

    private void Awake()
    {
        _cam = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _camOffset = MyTransform.position - _cam.transform.position;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out _hit, Mathf.Infinity))
            {
                _agent.SetDestination(_hit.point);
            }
        }
    }

    private void LateUpdate()
    {
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, MyTransform.position - _camOffset, 3f * Time.deltaTime);
    }

}
