using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBall : MonoBehaviour
{
    [SerializeField] Transform ball, target;
    float _height, _gravity;
    Rigidbody body;

    private void Awake()
    {
        body = ball.GetComponent<Rigidbody>();
    }
    private void Start()
    {
        _height = Mathf.Max(ball.position.y, target.position.y) + 2f;
        _gravity = Physics.gravity.magnitude;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            body.useGravity = true;
          //  print(CalculateLaunchVel());
            body.velocity = CalculateLaunchVel();
        }
    }

    Vector3 CalculateLaunchVel()
    {
        float displacementY = target.position.y - ball.position.y;
        Vector3 displacementXZ = new Vector3(target.position.x -  ball.position.x, 0f, target.position.z - ball.position.z);

        Vector3 velY = Mathf.Sqrt(2 * _gravity * _height) * Vector3.up;
        Vector3 velXZ = displacementXZ / (Mathf.Sqrt(2 * _height / _gravity) + Mathf.Sqrt(2 * Mathf.Abs(displacementY - _height) / _gravity));
        return velXZ + velY;
    }
}
