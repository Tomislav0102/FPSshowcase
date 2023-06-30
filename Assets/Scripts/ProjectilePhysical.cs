using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ProjectilePhysical : MonoBehaviour, IMaterial, ITakeDamage
{
    GameManager _gm;
    Rigidbody _rigid;
    Transform _myTransform;
    [SerializeField] SoItem weaponUsingThisProjectile;
    ElementType _elType;
    [field:SerializeField] public MatType MaterialType { get; set; }
    public bool IsDead { get; set; }
    public EnemyRef EnRef { get; set; }

    [SerializeField] float forceSpeed = 50f;
    Collider _collToIgnore;
    RaycastHit[] _hitsForContactPoint = new RaycastHit[1];
    bool _calculateLaunchVelocity;
    Vector3 _launchVel;
    EnemyRef _targetEnemyRef;


    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
        _gm = GameManager.Instance;
        _myTransform = transform;
        ReturnToPool();
    }
    public void IniThrowable(Transform spawPoint, Collider ownerCollider)
    {
        _myTransform.SetPositionAndRotation(spawPoint.position, spawPoint.rotation);
        _collToIgnore = ownerCollider;
        switch (weaponUsingThisProjectile.ammoType)
        {
            case AmmoType.Rocket:
                _elType = ElementType.Explosion;
                break;
            case AmmoType.HandGrenade:
                _elType = ElementType.Explosion;
                break;
            case AmmoType.Fuel:
                _elType = ElementType.Fire;
                break;
            default:
                _elType = ElementType.Normal;
                break;
        }
        _calculateLaunchVelocity = false;
    }
    public void IniThrowable(Transform spawPoint, Collider ownerCollider, Vector3 launchVelocity)
    {
        IniThrowable(spawPoint, ownerCollider);
        _calculateLaunchVelocity = true;
        _launchVel = launchVelocity;
        float spread = 1f;
        _launchVel = new Vector3(_launchVel.x + Random.Range(-spread, spread), _launchVel.y, _launchVel.z + Random.Range(-spread, spread));
    }
    void OnEnable()
    {
        if (_calculateLaunchVelocity) _rigid.velocity = _launchVel;
        else _rigid.AddRelativeForce(forceSpeed * Vector3.forward, ForceMode.VelocityChange);

        Invoke(nameof(ReturnToPool), 10f);
        
        if (weaponUsingThisProjectile.ammoType != AmmoType.HandGrenade) return;
        _rigid.AddTorque(1000f * new Vector3(Random.value, Random.value, Random.value));
    }

    public void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        if (damage > 0)
        {
            switch (weaponUsingThisProjectile.ammoType)
            {
                case AmmoType.Rocket:
                    _gm.poolManager.GetExplosion(ExplosionType.Big, _myTransform.position);
                    break;
                case AmmoType.HandGrenade:
                    _gm.poolManager.GetExplosion(ExplosionType.Small, _myTransform.position);
                    break;
            }

            ReturnToPool();
        }

    }

    void ReturnToPool()
    {
        CancelInvoke();
        _rigid.velocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    RaycastHit GetHit()
    {
        Physics.RaycastNonAlloc(_myTransform.position - 2 * _myTransform.forward, _myTransform.forward, _hitsForContactPoint, 2.1f, _gm.layShooting);
        return _hitsForContactPoint[0];
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_collToIgnore == other || other.isTrigger) return;

        switch (weaponUsingThisProjectile.ammoType)
        {
            case AmmoType.Bolt:
                _gm.player.offense.attack.ApplyDamage(weaponUsingThisProjectile, GetHit(), false);
                break;
            default:
               // print($"Collision with {other.name}. I AM EXPLODING NOW!!!");
                _targetEnemyRef = null;
                Collider[] allAffectedCollider = Physics.OverlapSphere(_myTransform.position, weaponUsingThisProjectile.areaOfEffect);
                _gm.poolManager.GetExplosion(weaponUsingThisProjectile.ammoType == AmmoType.Rocket ? ExplosionType.Big : ExplosionType.Small, _myTransform.position);
                foreach (Collider item in allAffectedCollider)
                {
                    if (item.TryGetComponent(out ITakeDamage damagable))
                    {
                        if (_targetEnemyRef == null || _targetEnemyRef != damagable.EnRef)
                        {
                            _targetEnemyRef = damagable.EnRef;
                            damagable.TakeDamage(_elType, HelperScript.Damage(weaponUsingThisProjectile.damage), _myTransform, null);
                        }
                    }
                    else if (item.TryGetComponent(out Rigidbody rigids) && !rigids.isKinematic)
                    {
                        rigids.AddExplosionForce(200f, _myTransform.position, weaponUsingThisProjectile.areaOfEffect, 0.2f);
                    }
                }
                break;
        }
        ReturnToPool();
    }


}
