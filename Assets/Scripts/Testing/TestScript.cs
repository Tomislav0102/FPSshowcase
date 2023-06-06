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
    bool _down;
    float _height;
    private void Awake()
    {
        _cam = Camera.main;
        _height = transform.position.y;
    }
    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            _down = !_down;
        }

        // float height = _down ? 1f : 3.5f;
        _height = Mathf.MoveTowards(_height, _down ? 1f : 5f, 0.2f);
        transform.position = new Vector3(transform.position.x, _height, transform.position.z);
    }


}
