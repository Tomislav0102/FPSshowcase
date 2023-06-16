using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialIdentifier : MonoBehaviour, IMaterial
{
    [field:SerializeField] public MatType MaterialType { get ; set ; }
}
