using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleMovePlayerNavMesh : MonoBehaviour/*, IFactionTarget*/
{
    Camera _cam;
    NavMeshAgent _agent;
    RaycastHit _hit;
    //public Transform MyTransform { get => _agent.transform; set { } }

    //[field:SerializeField] public Faction Fact { get; set; }
    //public Transform MyHead { get ; set ; }

    public float AgentSpeed()
    {
        return _agent.velocity.magnitude;
    }

    private void Awake()
    {
        _cam = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
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

    }

}
