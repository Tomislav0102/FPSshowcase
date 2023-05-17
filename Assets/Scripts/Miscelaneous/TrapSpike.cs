using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapSpike : MonoBehaviour
{
    readonly HashSet<ITakeDamage> _victims = new HashSet<ITakeDamage>();

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out ITakeDamage damagable) && !_victims.Contains(damagable))
        {
            _victims.Add(damagable);
          //  print("enter trigger");
            damagable.TakeDamage(ElementType.Normal, 1, transform, null);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ITakeDamage damagable) && _victims.Contains(damagable))
        {
           // print("exit trigger");
            _victims.Remove(damagable);
        }

    }
}
