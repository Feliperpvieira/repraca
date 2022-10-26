using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjManager : MonoBehaviour
{
    public TextMeshProUGUI painelNomeObjeto;
    public Image painelImagemObjeto;
    public GameObject painelBotaoAddObjeto;

    public GameObject currentImgDestaque;
    public GameObject newImgDestaque;
    public ObjetosData dados;

    public ObjetosData[] listaTodosDados;
    public GameObject prefabBotaoAdd;

    private GameObject objetoToBeConstruido;
    private BuildingManager buildingManager;

    private void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();

        for (int i = 0; i < listaTodosDados.Length; i++)
        {
            var botaoNovo = Instantiate(prefabBotaoAdd);
            botaoNovo.transform.SetParent(this.gameObject.transform, false);
            
            BotaoObjSelect botaoCriado = botaoNovo.GetComponent<BotaoObjSelect>();
            //botaoCriado.imagemObjeto.sprite = botaoCriado.dadosObj.imagemObjeto;

            botaoCriado.dadosObj = listaTodosDados[i];
            botaoCriado.Start();
        }
    }

    public void BotaoObjClicado()
    {
        if(currentImgDestaque != null)
        {
            currentImgDestaque.SetActive(false);
        }
        newImgDestaque.SetActive(true);

        currentImgDestaque = newImgDestaque;
        //Debug.Log(dados.nome);
        painelNomeObjeto.text = dados.nome;
        painelImagemObjeto.sprite = dados.imagemObjeto;
        objetoToBeConstruido = dados.prefab;
    }

    public void AdicionaObjeto()
    {
        buildingManager.SelectObject(objetoToBeConstruido);
    }
}
