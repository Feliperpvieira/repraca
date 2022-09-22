using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dados do Objeto")]
public class ObjetosData : ScriptableObject
{
    public string nome;
    public GameObject prefab;
}
