using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FiltroManager : MonoBehaviour
{
    public Button primeiroBotao;

    private Button botaoAtual;
    private Button botaoAnterior;

    TextMeshProUGUI textoAtual;
    TextMeshProUGUI textoAnterior;

    private Color corBege = new Color(249/255f, 239/255f, 231/255f);
    private Color corAzul = new Color(19/255f, 28/255f, 59/255f);


    public void administraFiltro(Button botaoNovo)
    {
        if (botaoNovo != botaoAnterior)
        {
            botaoAtual = botaoNovo;

            botaoAtual.image.color = corAzul;
            textoAtual = botaoAtual.transform.GetComponentInChildren<TextMeshProUGUI>();
            textoAtual.color = corBege;


            if (botaoAnterior == null)
            {
                botaoAnterior = primeiroBotao;
            }

            botaoAnterior.image.color = corBege;
            textoAnterior = botaoAnterior.transform.GetComponentInChildren<TextMeshProUGUI>();
            textoAnterior.color = corAzul;

            botaoAnterior = botaoAtual;
            textoAnterior = textoAtual;
        }
        
    }
}
