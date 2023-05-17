using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class TestScript : MonoBehaviour
{
    public Collider collTarget;
    Ray _ray;
    RaycastHit _hit;
    [SerializeField]
    Transform sightSphere;

    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }


    private void FixedUpdate()
    {
        _ray.origin = transform.position;
        _ray.direction = (collTarget.transform.position - transform.position).normalized;
        if (Physics.Raycast(_ray, out _hit, sightSphere.localScale.x * 0.5f))
        {
            if (_hit.collider == collTarget)
            {
                Debug.DrawRay(_ray.origin, _ray.direction, Color.red);
            }
            else Debug.Log(_hit.collider.name);

        }
    }

}
