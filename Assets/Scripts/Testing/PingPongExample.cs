using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingPongExample : MonoBehaviour
{
    public Light myLight;
    public float num;

    void Start()
    {
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        num = Mathf.PingPong(Time.time, 8);
    }
}