using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    Transform _myTransform;
    [SerializeField] Transform parMeshes;
    GameObject[] parPickUpGroups;
    public PuType typeOfPickup;
    public PuType PUType
    {
        get => _puType;
        set
        {
            _puType = value;
            for (int i = 0; i < parPickUpGroups.Length; i++)
            {
                parPickUpGroups[i].SetActive(false);
                for (int j = 0; j < parPickUpGroups[i].transform.childCount; j++)
                {
                    parPickUpGroups[i].transform.GetChild(j).gameObject.SetActive(false);
                }
            }
            parPickUpGroups[(int)value].SetActive(true);
            
            switch (value)
            {
                case PuType.Weapon:
                    parPickUpGroups[(int)value].transform.GetChild(weaponPU.ordinalLookup).gameObject.SetActive(true);
                    break;
                case PuType.Ammo:
                    parPickUpGroups[(int)value].transform.GetChild((int)ammoType).gameObject.SetActive(true);
                    break;
                case PuType.Health:
                    parPickUpGroups[(int)value].transform.GetChild(0).gameObject.SetActive(true);
                    break;
                case PuType.Armor:
                    parPickUpGroups[(int)value].transform.GetChild(0).gameObject.SetActive(true);
                    break;
                case PuType.Key:
                    parPickUpGroups[(int)value].transform.GetChild((int)keyType).gameObject.SetActive(true);
                    break;
            }
        }
    }
    PuType _puType;
    public SoItem weaponPU;

    public AmmoType ammoType;
    public bool bigAmmoPack;
    public Dictionary<AmmoType, int> ammoQuantity = new Dictionary<AmmoType, int>();

    public int healAmount;
    public int armorAmount;
    public KeyType keyType;

    private void Awake()
    {
        _myTransform = transform;
        parPickUpGroups = new GameObject[parMeshes.childCount];
        for (int i = 0; i < parMeshes.childCount; i++)
        {
            parPickUpGroups[i] = parMeshes.GetChild(i).gameObject;
        }
    }
    private void OnEnable()
    {
        PUType = typeOfPickup;

        switch (PUType)
        {
            case PuType.Weapon:
                break;
            case PuType.Ammo:
                ammoQuantity.Add(AmmoType.None, 0);
                ammoQuantity.Add(AmmoType._9mm, bigAmmoPack ? 50 : 10);
                ammoQuantity.Add(AmmoType._44cal, bigAmmoPack ? 30 : 10);
                ammoQuantity.Add(AmmoType._762mm, bigAmmoPack ? 30 : 10);
                ammoQuantity.Add(AmmoType._303REM, bigAmmoPack ? 20 : 8);
                ammoQuantity.Add(AmmoType._12gauge, bigAmmoPack ? 20 : 8);
                ammoQuantity.Add(AmmoType.Rocket, bigAmmoPack ? 5 : 1);
                ammoQuantity.Add(AmmoType.HandGrenade, bigAmmoPack ? 5 : 1);
                ammoQuantity.Add(AmmoType.Bolt, bigAmmoPack ? 50 : 10);
                ammoQuantity.Add(AmmoType.Fuel, bigAmmoPack ? 500 : 100);
                break;
            case PuType.Health:
                break;
            case PuType.Armor:
                break;
            case PuType.Key:
                break;
        }


    }

    private void Update()
    {
        _myTransform.Rotate(10f * Time.deltaTime * Vector3.up, Space.Self);
    }

    public void OnPickup()
    {
        print($"{PUType} picked up");
        gameObject.SetActive(false);
    }
}
