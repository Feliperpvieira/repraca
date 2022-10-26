using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Dados do Objeto")]
public class ObjetosData : ScriptableObject
{
    public string nome; //nome do objeto
    public Sprite imagemObjeto; //render transparente do objeto
    public GameObject prefab; //prefab com o modelo que vai ser construído
}
