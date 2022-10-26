using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjSelect : MonoBehaviour
{
    public ObjetosData dadosObj;
    public GameObject imagemDestaque;

    private BotaoObjManager objetosManager;

    public void Start()
    {
        objetosManager = GameObject.Find("GridAddObjetos").GetComponent<BotaoObjManager>();
    }

    public void SelecionaObjeto()
    {
        //imagemDestaque.SetActive(true);
        objetosManager.newImgDestaque = imagemDestaque;
        objetosManager.dados = dadosObj;

        objetosManager.BotaoObjClicado();
    }
}
