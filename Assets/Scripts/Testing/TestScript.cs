using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestScript : MonoBehaviour
{
    Camera _cam;
    RaycastHit _hit;
    public Transform sphere;
    NavMeshAgent _agent;
    Vector3[] _pos= new Vector3[0];
    public Vector3 _nextPos;
    float _ver, _hor;

    public bool isPlayer;
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _cam = Camera.main;
    }
    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }



    private void Update()
    {
        if (!isPlayer) return;


        if (Input.GetMouseButton(0) && isPlayer)
        {
            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out _hit, Mathf.Infinity))
            {
                sphere.position = _hit.point;
                _agent.SetDestination(_hit.point);
            }
        }

        //_agent.SetDestination(sphere.position);
        //_pos = _agent.path.corners;
        //_nextPos = _agent.nextPosition;
        //if (_agent.path.corners.Length > 1)
        //{
        //    Vector3 dir = (_pos[1] - _pos[0]).normalized;
        //    transform.rotation = Quaternion.LookRotation(dir);
        //}
        //_ver = Input.GetAxis("Vertical");
        //transform.Translate(_ver * Vector3.forward, Space.Self);
        //_agent.nextPosition = transform.position;
    }


}
