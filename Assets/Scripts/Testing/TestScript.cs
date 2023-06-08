using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Threading;
using System.Threading.Tasks;

public class TestScript : MonoBehaviour
{
    Camera _cam;
    //RaycastHit _hit;
    //RaycastHit[] _multipleHits = new RaycastHit[1];
    //public Transform sphere;
    //Vector3[] _pos= new Vector3[0];
    //public Vector3 _nextPos;
    //float _ver, _hor;
    //public LayerMask layerMask;
    //public bool isPlayer;
    // public Animator anim;

    private void Awake()
    {
        _cam = Camera.main;
    }
    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }



}
