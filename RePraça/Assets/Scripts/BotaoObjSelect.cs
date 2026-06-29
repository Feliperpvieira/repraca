using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjSelect : MonoBehaviour
{
    public ObjetosData dadosObj; //scriptable object com os dados
    public GameObject imagemDestaque; //imagem da borda inferior, marca qual botao está selecionado
    public Image imagemObjeto; //imagem do objeto no botão

    private BotaoObjManager objetosManager; //pega o arquivo botaoObjManager

    public void Start()
    {
        objetosManager = GameObject.Find("GridAddObjetos").GetComponent<BotaoObjManager>(); //pega o arquivo BotaoObjManager no gameObject GridAddObjetos
        imagemObjeto.sprite = dadosObj.imagemObjeto; //define a imagem do botao como a imagem do scriptable object
    }

    public void SelecionaObjeto() //funcao usada ao clicar no botão
    {
        // LEANTWEEN: Animação de "aperto" (escala reduz para 0.9 e volta a 1)
        LeanTween.cancel(this.gameObject);
        transform.localScale = Vector3.one; // Garante que começa no tamanho certo
        LeanTween.scale(this.gameObject, new Vector3(0.9f, 0.9f, 0.9f), 0.1f).setLoopPingPong(1);

        //Passa os dados do arquivo dadosObj pro script BotaoObjManager do GridAddObjects, ele vai fazer tudo a partir daqui
        objetosManager.newImgDestaque = imagemDestaque;
        objetosManager.dados = dadosObj;

        objetosManager.BotaoObjClicado();
    }
}
