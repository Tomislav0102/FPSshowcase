using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;

public class BreathCollider : MonoBehaviour
{
    PoolManager _poolManager;
    HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();
    SoItem _weapon;
    Transform wildFire;
    Transform _myTransform;

    public void Init(Collider[] colIgnore, SoItem wea)
    {
        for (int i = 0; i < colIgnore.Length; i++)
        {
            _collidersToIgnore.Add(colIgnore[i]);
        }

        _weapon = wea;
        _poolManager = GameManager.Instance.poolManager;
        _myTransform = transform;
    }

    public void SpawnFire(Vector3 point)
    {
        if (wildFire == null)
        {
            wildFire = _poolManager.GetWildFire();
            wildFire.GetComponent<ParticleSystem>().Play();
        }

        wildFire.position = point;
    }
    public void StopFire()
    {
        wildFire = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ITakeDamage damagable) && !_collidersToIgnore.Contains(other))
        {
            damagable.TakeDamage(ElementType.Fire, HelperScript.Damage(_weapon.damage), _myTransform, _weapon.damageOverTime);
          //  print(other.name);
        }
    }

}
