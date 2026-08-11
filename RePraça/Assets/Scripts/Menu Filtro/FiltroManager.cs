using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FiltroManager : MonoBehaviour
{
    public Button primeiroBotao;

    private Button botaoAtual;

    private Color corBege = new Color(249/255f, 239/255f, 231/255f);
    private Color corAzul = new Color(19/255f, 28/255f, 59/255f);

    [Header("Animação visual")]
    [Min(0.05f)] public float duracaoPop = 0.14f;

    private void Start()
    {
        // Mantém a cor do primeiro botão coerente mesmo antes de haver cliques.
        if (primeiroBotao != null)
            DefinirSelecao(primeiroBotao, false);
    }

    // Este método trata exclusivamente do estado visual do botão. Os comportamentos
    // concretos (ordenar arquivo, mudar secção, filtrar objectos) ficam noutros scripts.
    public void administraFiltro(Button botaoNovo)
    {
        DefinirSelecao(botaoNovo, true);
    }

    // Usado pelos filtros criados automaticamente para indicar qual é o botão
    // "Todos" sem ser necessário preencher o campo Primeiro Botao no Inspector.
    public void DefinirPrimeiroBotao(Button botao)
    {
        primeiroBotao = botao;
        DefinirSelecao(botao, false);
    }

    private void DefinirSelecao(Button botaoNovo, bool animar)
    {
        if (botaoNovo == null || botaoNovo == botaoAtual)
            return;

        if (botaoAtual != null)
            AplicarCores(botaoAtual, false);

        botaoAtual = botaoNovo;
        AplicarCores(botaoAtual, true);

        if (animar)
        {
            LeanTween.cancel(botaoAtual.gameObject);
            botaoAtual.transform.localScale = Vector3.one;
            LeanTween.scale(botaoAtual.gameObject, Vector3.one * 0.96f, duracaoPop)
                .setEaseOutQuad()
                .setLoopPingPong(1);
        }
    }

    private void AplicarCores(Button botao, bool seleccionado)
    {
        if (botao.image != null)
            botao.image.color = seleccionado ? corAzul : corBege;

        TextMeshProUGUI texto = botao.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null)
            texto.color = seleccionado ? corBege : corAzul;
    }
}
