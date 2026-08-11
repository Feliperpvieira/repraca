using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cria automaticamente os filtros do catálogo a partir das categorias presentes
// em BotaoObjManager.listaTodosDados. Deve ficar no objecto FiltroCatObjetos.
public class FiltroCategoriasObjetos : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Gestor do catálogo GridAddObjetos.")]
    public BotaoObjManager gestorObjetos;
    [Tooltip("Mantém o aspecto azul/bege comum aos restantes filtros.")]
    public FiltroManager gestorVisual;
    [Tooltip("Content com Horizontal Layout Group, normalmente Scroll/Botoes.")]
    public Transform contentorBotoes;
    [Tooltip("Prefab branco dos filtros, já com Button e TextMeshPro.")]
    public GameObject prefabBotaoFiltro;

    [Header("Animação")]
    [Min(0.05f)] public float duracaoEntrada = 0.14f;

    private readonly List<GameObject> botoesCriados = new List<GameObject>();

    private IEnumerator Start()
    {
        // O BotaoObjManager cria os cartões no Start. Esperar um frame garante
        // que lê todas as categorias depois de o catálogo estar preparado.
        yield return null;
        ConstruirFiltros();
    }

    // Pode ser chamado manualmente se forem adicionados objectos ao catálogo
    // durante a execução do jogo.
    public void ConstruirFiltros()
    {
        ResolverReferenciasEmFalta();
        if (gestorObjetos == null || gestorVisual == null || contentorBotoes == null || prefabBotaoFiltro == null)
        {
            Debug.LogError("[FiltroCategoriasObjetos] Faltam referências no Inspector.");
            return;
        }

        LimparBotoesAntigos();

        CriarBotao("Todos", true, 0);

        List<string> categorias = gestorObjetos.ObterCategoriasOrdenadas();
        for (int i = 0; i < categorias.Count; i++)
            CriarBotao(categorias[i], false, i + 1);

        // A lista inicia sempre completa e em ordem alfabética.
        gestorObjetos.FiltraBotoes("Todos");
    }

    private void CriarBotao(string categoria, bool ePrimeiro, int indice)
    {
        GameObject novoBotao = Instantiate(prefabBotaoFiltro, contentorBotoes);
        novoBotao.name = "Filtro - " + categoria;
        botoesCriados.Add(novoBotao);

        TextMeshProUGUI texto = novoBotao.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null)
            texto.text = categoria;

        Button botao = novoBotao.GetComponent<Button>();
        botaoFiltroSelec estiloBotao = novoBotao.GetComponent<botaoFiltroSelec>();
        if (estiloBotao != null)
            estiloBotao.Configurar(gestorVisual);

        if (botao != null)
        {
            string categoriaDoBotao = categoria; // Evita que closures partilhem a última categoria do ciclo.
            botao.onClick.AddListener(() => gestorObjetos.FiltraBotoes(categoriaDoBotao));

            if (ePrimeiro)
                gestorVisual.DefinirPrimeiroBotao(botao);
        }

        AnimarEntrada(novoBotao, indice);
    }

    private void LimparBotoesAntigos()
    {
        // Remove também os botões provisórios que estavam no Content da cena.
        // Não toca no Scroll, Mask ou Horizontal Layout Group que os contém.
        foreach (Transform filho in contentorBotoes)
            Destroy(filho.gameObject);

        botoesCriados.Clear();
    }

    private void ResolverReferenciasEmFalta()
    {
        if (gestorObjetos == null)
            gestorObjetos = FindObjectOfType<BotaoObjManager>();
        if (gestorVisual == null)
            gestorVisual = GetComponent<FiltroManager>();
        if (contentorBotoes == null)
        {
            Transform scroll = transform.Find("Scroll");
            contentorBotoes = scroll != null ? scroll.Find("Botoes") : null;
        }
    }

    private void AnimarEntrada(GameObject botao, int indice)
    {
        CanvasGroup grupo = botao.GetComponent<CanvasGroup>();
        if (grupo == null)
            grupo = botao.AddComponent<CanvasGroup>();

        grupo.alpha = 0f;
        botao.transform.localScale = Vector3.one * 0.92f;

        float atraso = Mathf.Min(indice * 0.025f, 0.18f);
        LeanTween.alphaCanvas(grupo, 1f, duracaoEntrada).setDelay(atraso).setEaseOutQuad();
        LeanTween.scale(botao, Vector3.one, duracaoEntrada).setDelay(atraso).setEaseOutBack();
    }
}
