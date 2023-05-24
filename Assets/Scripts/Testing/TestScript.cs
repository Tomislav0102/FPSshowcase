using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestScript : MonoBehaviour
{
    Camera _cam;
    RaycastHit _hit;
    RaycastHit[] _multipleHits = new RaycastHit[1];
    public Transform sphere;
    Vector3[] _pos= new Vector3[0];
    public Vector3 _nextPos;
    float _ver, _hor;
    public LayerMask layerMask;
    public bool isPlayer;
    private void Awake()
    {
        _cam = Camera.main;
    }
    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
        gameObject.layer = layerMask;
    }



    private void Update()
    {
       int num =  Physics.SphereCastNonAlloc(transform.position, 0.5f, transform.forward, _multipleHits, 10f);

        for (int i = 0; i < num; i++)
        {
            print(_multipleHits[i].collider.name);
        }
    }


}
