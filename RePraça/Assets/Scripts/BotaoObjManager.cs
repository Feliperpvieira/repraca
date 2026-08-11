using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BotaoObjManager : MonoBehaviour
{
    [Header("Dados do painel de infos")]
    public TextMeshProUGUI painelNomeObjeto;
    public Image painelImagemObjeto;
    public TextMeshProUGUI painelCategoria;
    public TextMeshProUGUI painelMedidasComp;
    public TextMeshProUGUI painelMedidasLarg;
    public TextMeshProUGUI painelMedidasAltura;
    public TextMeshProUGUI painelMaterial;
    public TextMeshProUGUI painelTxtDescricao;
    public GameObject painelCompleto;

    [Header("Prefab do botão de objetos")]
    public GameObject prefabBotaoAdd;

    [Header("Animação do Painel (LeanTween)")]
    public CanvasGroup painelCanvasGroup;
    public float tempoFade = 0.25f;
    // o quanto o painel deve deslizar
    public float distanciaEsconder = 150f;
    // O script vai calcular estas posições automaticamente no Start
    private float posicaoAbertaX;
    private float posicaoEscondidaX;
    // Precisa do RectTransform para mover UI
    private RectTransform painelRect;

    [Header("DEIXAR VAZIO - coisas do script")]
    public GameObject currentImgDestaque;
    public GameObject newImgDestaque;
    public ObjetosData dados;
    public List<BotaoObjSelect> botoesCriados = new List<BotaoObjSelect>(); //Lista que guarda os botões gerados para simular cliques neles depois

    [Header("Todos os script de dados")]
    public ObjetosData[] listaTodosDados;
    

    private GameObject objetoToBeConstruido;
    private BuildingManager buildingManager;
    private Coroutine rotinaFiltro;

    private void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>(); //pega o building manager pra construir os objs

        // Guarda a referência da posição do painel
        painelRect = painelCompleto.GetComponent<RectTransform>();

        // O script guarda a posição do painel no Editor
        posicaoAbertaX = painelRect.anchoredPosition.x;
        posicaoEscondidaX = posicaoAbertaX + distanciaEsconder; // Adiciona 150 para animar

        // A lista no Inspector continua a ser a fonte de dados, mas os cartões são
        // criados em ordem alfabética para a loja ser previsível para o utilizador.
        List<ObjetosData> dadosOrdenados = new List<ObjetosData>(listaTodosDados);
        CompareInfo comparador = CultureInfo.GetCultureInfo("pt-PT").CompareInfo;
        dadosOrdenados.Sort((a, b) => comparador.Compare(a != null ? a.nome : "", b != null ? b.nome : "", CompareOptions.IgnoreCase));

        for (int i = 0; i < dadosOrdenados.Count; i++) //loop que passa por todos os dados de objectos
        {
            if (dadosOrdenados[i] == null)
                continue;

            var botaoNovo = Instantiate(prefabBotaoAdd); //cria o botao
            botaoNovo.transform.SetParent(this.gameObject.transform, false); //coloca o botao como child desse objeto (a grid)

            BotaoObjSelect botaoCriado = botaoNovo.GetComponent<BotaoObjSelect>(); //pega o script do botao recém criado

            botaoCriado.dadosObj = dadosOrdenados[i]; //salva o arquivo de dados no botão recém criado

            botoesCriados.Add(botaoCriado); //Salva o botão criado na lista dos botões
        }
    }

    public void FiltraBotoes(string filtro)
    {
        // "Todos", vazio e null são equivalentes: mostram o catálogo completo.
        bool mostrarTodos = string.IsNullOrWhiteSpace(filtro) || filtro == "Todos";

        if (rotinaFiltro != null)
            StopCoroutine(rotinaFiltro);

        rotinaFiltro = StartCoroutine(AplicarFiltroComAnimacao(filtro, mostrarTodos));
    }

    // Devolve categorias únicas e alfabetizadas. O FiltroCategoriasObjetos usa
    // este método para gerar os botões, por isso não existe uma lista duplicada.
    public List<string> ObterCategoriasOrdenadas()
    {
        HashSet<string> categoriasUnicas = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (ObjetosData objeto in listaTodosDados)
        {
            if (objeto != null && !string.IsNullOrWhiteSpace(objeto.categoria))
                categoriasUnicas.Add(objeto.categoria.Trim());
        }

        List<string> categorias = new List<string>(categoriasUnicas);
        CompareInfo comparador = CultureInfo.GetCultureInfo("pt-PT").CompareInfo;
        categorias.Sort((a, b) => comparador.Compare(a, b, CompareOptions.IgnoreCase));
        return categorias;
    }

    private IEnumerator AplicarFiltroComAnimacao(string categoria, bool mostrarTodos)
    {
        // Primeiro os cartões visíveis desaparecem discretamente. Esperar 0,10 s
        // impede que a grelha salte para a nova disposição sem transição.
        foreach (BotaoObjSelect botao in botoesCriados)
        {
            if (botao == null || !botao.gameObject.activeSelf)
                continue;

            CanvasGroup grupo = ObterCanvasGroup(botao.gameObject);
            LeanTween.cancel(botao.gameObject);
            LeanTween.alphaCanvas(grupo, 0f, 0.10f).setEaseInQuad();
        }

        yield return new WaitForSeconds(0.10f);

        int indiceEntrada = 0;
        foreach (BotaoObjSelect botao in botoesCriados)
        {
            if (botao == null || botao.dadosObj == null)
                continue;

            bool pertenceCategoria = mostrarTodos || string.Equals(botao.dadosObj.categoria, categoria, System.StringComparison.OrdinalIgnoreCase);
            botao.gameObject.SetActive(pertenceCategoria);

            if (!pertenceCategoria)
                continue;

            CanvasGroup grupo = ObterCanvasGroup(botao.gameObject);
            grupo.alpha = 0f;
            botao.transform.localScale = Vector3.one * 0.94f;

            float atraso = Mathf.Min(indiceEntrada * 0.018f, 0.16f);
            LeanTween.alphaCanvas(grupo, 1f, 0.14f).setDelay(atraso).setEaseOutQuad();
            LeanTween.scale(botao.gameObject, Vector3.one, 0.14f).setDelay(atraso).setEaseOutBack();
            indiceEntrada++;
        }

        rotinaFiltro = null;
    }

    private static CanvasGroup ObterCanvasGroup(GameObject objecto)
    {
        CanvasGroup grupo = objecto.GetComponent<CanvasGroup>();
        return grupo != null ? grupo : objecto.AddComponent<CanvasGroup>();
    }

    public void BotaoObjClicado()
    {
        //animacao de abrir
        // Só roda a animação de Fade In se o painel estiver invisível, evita que anime quando abre o menu de objetos com algo selecionado anteriormente
        if (!painelCompleto.activeInHierarchy)
        {
            painelCompleto.SetActive(true);
            painelCanvasGroup.alpha = 0f;

            // Coloca o painel na posição escondida usando AnchoredPosition (seguro para UI)
            Vector2 startPos = painelRect.anchoredPosition;
            startPos.x = posicaoEscondidaX;
            painelRect.anchoredPosition = startPos;

            LeanTween.cancel(painelCompleto);

            // ANIMAÇÃO 1: FADE IN
            LeanTween.alphaCanvas(painelCanvasGroup, 1f, tempoFade).setEaseOutQuad();

            // ANIMAÇÃO 2: MOVER (Usa o LeanTween.value para alterar o AnchoredPosition)
            LeanTween.value(painelCompleto, posicaoEscondidaX, posicaoAbertaX, tempoFade)
                .setEaseOutQuad()
                .setOnUpdate((float val) =>
                {
                    Vector2 pos = painelRect.anchoredPosition;
                    pos.x = val;
                    painelRect.anchoredPosition = pos;
                });
        }

        //painelCompleto.SetActive(true);
        if (currentImgDestaque != null) //se já tiver algum botão selecionado
        {
            currentImgDestaque.SetActive(false); //desliga a imagem que marca que ele tá selecionado
        }
        newImgDestaque.SetActive(true); //ativa a imagem no botão selecionado novo

        currentImgDestaque = newImgDestaque; //o botão novo vira o atual

        //Define as informações no painel de informações na direita
        // /n para pular linha
        painelNomeObjeto.text = dados.nome;
        painelImagemObjeto.sprite = dados.imagemObjeto;
        objetoToBeConstruido = dados.prefab;
        painelCategoria.text = dados.categoria;
        painelMedidasComp.text = dados.comprimento;
        painelMedidasLarg.text = dados.largura;
        painelMedidasAltura.text = dados.altura;
        painelMaterial.text = dados.materiais;
        painelTxtDescricao.text = dados.descricao;
    }

    public void AdicionaObjeto() //funcao do botao adicionar que ta no painel
    {
        // Passa os dados completos (ScriptableObject) para o BuildingManager saber a categoria
        buildingManager.dadosDoObjetoPendente = dados;

        buildingManager.SelectObject(objetoToBeConstruido); //passa o prefab pro building manager construir
    }

    public void FecharPainelObjs()
    {
        // Desliga o botão que marca que o obj tá selecionado (com proteção para evitar erros)
        if (currentImgDestaque != null)
        {
            currentImgDestaque.SetActive(false);
            currentImgDestaque = null;
        }

        // Cancela animações anteriores (importante se o jogador clicar em abrir e fechar muito rápido)
        LeanTween.cancel(painelCompleto);

        // ANIMAÇÃO 1: FADE OUT
        // Tiramos o "painelCanvasGroup.alpha = 1f;" para que ele faça o fade a partir de onde estiver!
        LeanTween.alphaCanvas(painelCanvasGroup, 0f, tempoFade).setEaseOutQuad();

        // ANIMAÇÃO 2: MOVER PARA FORA E DESLIGAR
        // Usamos painelRect.anchoredPosition.x no início para evitar solavancos se a animação for interrompida
        LeanTween.value(painelCompleto, painelRect.anchoredPosition.x, posicaoEscondidaX, tempoFade)
            .setEaseOutQuad()
            .setOnUpdate((float val) =>
            {
                Vector2 pos = painelRect.anchoredPosition;
                pos.x = val;
                painelRect.anchoredPosition = pos;
            })
            .setOnComplete(() =>
            {
                painelCompleto.SetActive(false); // O painel só é desligado quando a animação atinge os 100%
            });
    }

    // função chamada pelo SelectionManager para simular o clique no botão quando usuario clica na tag de nome de um dos objetos selecionados
    public void AbrirDetalhesDoObjeto(string nomeDoPrefab)
    {
        // Procura na lista o botão que guarda o prefab com o exato mesmo nome
        BotaoObjSelect botaoCerto = botoesCriados.Find(b => b.dadosObj.prefab.name == nomeDoPrefab);

        if (botaoCerto != null)
        {
            botaoCerto.SelecionaObjeto(); // Simula o clique do utilizador no botão da loja
        }
        else
        {
            Debug.LogWarning("Não foi encontrado nenhum botão na loja para o objeto: " + nomeDoPrefab);
        }
    }

}
