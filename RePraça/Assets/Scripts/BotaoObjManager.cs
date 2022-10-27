using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjManager : MonoBehaviour
{
    [Header("Dados do painel de infos")]
    public TextMeshProUGUI painelNomeObjeto;
    public Image painelImagemObjeto;
    public TextMeshProUGUI painelCategoria;
    public TextMeshProUGUI painelMedidas;
    public TextMeshProUGUI painelMaterial;
    public TextMeshProUGUI painelTxtDescricao;
    private GameObject painelBotaoAddObjeto;

    [Header("Prefab do botão de objetos")]
    public GameObject prefabBotaoAdd;

    [Header("DEIXAR VAZIO - coisas do script")]
    public GameObject currentImgDestaque;
    public GameObject newImgDestaque;
    public ObjetosData dados;

    [Header("Todos os script de dados")]
    public ObjetosData[] listaTodosDados;
    

    private GameObject objetoToBeConstruido;
    private BuildingManager buildingManager;

    private void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>(); //pega o building manager pra construir os objs

        for (int i = 0; i < listaTodosDados.Length; i++) //loop for que passa pela lista de todos os dados. nela vão estar TODOS os arquivos de dado de objeto
        {
            var botaoNovo = Instantiate(prefabBotaoAdd); //cria o botao
            botaoNovo.transform.SetParent(this.gameObject.transform, false); //coloca o botao como child desse objeto (a grid)
            
            BotaoObjSelect botaoCriado = botaoNovo.GetComponent<BotaoObjSelect>(); //pega o script do botao recém criado
            //botaoCriado.imagemObjeto.sprite = botaoCriado.dadosObj.imagemObjeto;
            
            botaoCriado.dadosObj = listaTodosDados[i]; //salva o arquivo de dados no botão recém criado
            //botaoCriado.Start(); //roda o start pq nele o botão recém criado define a imagem
        }
    }

    public void BotaoObjClicado()
    {
        if(currentImgDestaque != null) //se já tiver algum botão selecionado
        {
            currentImgDestaque.SetActive(false); //desliga a imagem que marca que ele tá selecionado
        }
        newImgDestaque.SetActive(true); //ativa a imagem no botão selecionado novo

        currentImgDestaque = newImgDestaque; //o botão novo vira o atual

        //Define as informações no painel de informações na direita
        painelNomeObjeto.text = dados.nome;
        painelImagemObjeto.sprite = dados.imagemObjeto;
        objetoToBeConstruido = dados.prefab;
        painelCategoria.text = dados.categoria;
        painelMedidas.text = dados.tamanho;
        painelMaterial.text = dados.materiais;
        painelTxtDescricao.text = dados.descricao;
    }

    public void AdicionaObjeto() //funcao do botao adicionar que ta no painel
    {
        buildingManager.SelectObject(objetoToBeConstruido); //passa o prefab pro building manager construir
    }
}
