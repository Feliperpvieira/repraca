using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class filtroObjetos : MonoBehaviour
{
    private BotaoObjManager objetosManager; //pega o arquivo botaoObjManager
    //public string categoria;

    private void Start()
    {
        objetosManager = GameObject.Find("GridAddObjetos").GetComponent<BotaoObjManager>(); //pega o arquivo BotaoObjManager no gameObject GridAddObjetos
    }

    public void filtraBotoes(string categoria)
    {
        objetosManager.FiltraBotoes(categoria);
        Debug.Log(categoria);
    }
}
