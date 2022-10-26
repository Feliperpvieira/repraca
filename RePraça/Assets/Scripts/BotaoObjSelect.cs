using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjSelect : MonoBehaviour
{
    public ObjetosData dadosObj;
    public GameObject imagemDestaque;
    public Image imagemObjeto;

    private BotaoObjManager objetosManager;

    public void Start()
    {
        objetosManager = GameObject.Find("GridAddObjetos").GetComponent<BotaoObjManager>();
        imagemObjeto.sprite = dadosObj.imagemObjeto;
    }

    public void SelecionaObjeto()
    {
        //imagemDestaque.SetActive(true);
        objetosManager.newImgDestaque = imagemDestaque;
        objetosManager.dados = dadosObj;

        objetosManager.BotaoObjClicado();
    }
}
