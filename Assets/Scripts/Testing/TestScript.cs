using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    Ray _ray;
        RaycastHit[] _hits = new RaycastHit[10];
    public int num;
    public Transform sphere;
   // public Collider[] colls = new Collider[5];

    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }


    private void FixedUpdate()
    {
        _ray.origin = transform.position;
        _ray.direction = transform.forward;

      //  num = Physics.OverlapSphereNonAlloc(transform.position, sphere.localScale.x * 0.5f, colls);


        num = Physics.RaycastNonAlloc(_ray, _hits, sphere.localScale.x * 0.5f);
        for (int i = 0; i < num; i++)
        {
            print(_hits[i].collider.name);
        }
    }

}
