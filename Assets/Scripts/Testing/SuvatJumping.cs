using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuvatJumping : MonoBehaviour
{
    public float jumpHeight;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<Rigidbody>().velocity = Mathf.Sqrt(2 * jumpHeight * Physics.gravity.magnitude) * Vector3.up;
        }
    }
}
