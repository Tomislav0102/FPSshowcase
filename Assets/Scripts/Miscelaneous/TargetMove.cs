using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMove : MonoBehaviour
{
    float posZ;
    float _addedPos;

    private void Start()
    {
        posZ = transform.position.z;
    }

    private void Update()
    {
        _addedPos = Mathf.PingPong(2f * Time.time, 10f);
        transform.position = new Vector3(transform.position.x, 1f, posZ + _addedPos);
    }
}
