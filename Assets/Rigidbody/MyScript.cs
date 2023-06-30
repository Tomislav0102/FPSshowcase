using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyScript : MonoBehaviour
{
    float _hor, _ver, _rotY;
    Vector3 _smjer;
    public float brzinaKretanja;
    public float brzinaRotacije;
    Rigidbody _rigid;
    public Transform kockaKugla;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }


    private void Update()
    {
        _hor = Input.GetAxis("Horizontal");
        _ver = Input.GetAxis("Vertical");
      //  Kretanje1();
      //  Kretanje2();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            Collider[] colls = Physics.OverlapSphere(kockaKugla.position, 2f);
            foreach(Collider item in colls)
            {
                if(item.GetComponent<Rigidbody>() != null)
                {
                    print(item.name);
                    item.GetComponent<Rigidbody>().AddExplosionForce(1000f, kockaKugla.position, 10f, 0f);
                }
            }
        }
    }


    private void Kretanje1()
    {
        _smjer = new Vector3(_hor, 0f, _ver).normalized;
        _smjer *= brzinaKretanja;
        transform.position += brzinaKretanja * _smjer;
        // transform.Translate(_dir);
        //_rotY = Input.GetAxis("Mouse X");
        //transform.Rotate(brzinaRotacije * _rotY * Vector3.up);
    }
    private void Kretanje2()
    {
        _smjer = brzinaKretanja * _ver * transform.forward;
        transform.Rotate(brzinaRotacije * _hor * Vector3.up);
    }


    private void FixedUpdate()
    {
       // _rigid.velocity = new Vector3(_smjer.x, _rigid.velocity.y, _smjer.z);
       // _rigid.AddForce(brzinaKretanja * _smjer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(Vector3.zero, _smjer);
    }
}
