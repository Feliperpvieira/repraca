using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Dados do Objeto")]
public class ObjetosData : ScriptableObject
{
    public string nome; //nome do objeto
    public string categoria; //categoria do objeto
    public string tamanho; //medidas em 1 linha unica
    public string materiais; //materiais do objeto
    [TextArea]
    public string descricao; //descricao do objeto
    public Sprite imagemObjeto; //render transparente do objeto
    public GameObject prefab; //prefab com o modelo que vai ser construído
}
