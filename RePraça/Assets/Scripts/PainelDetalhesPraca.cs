using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Anexe este componente ao objecto raiz do popup de detalhes do arquivo.
// As referências são configuradas no Inspector quando o popup estiver montado.
public class PainelDetalhesPraca : MonoBehaviour
{
    [Header("Raiz")]
    [Tooltip("Se ficar vazio, este próprio GameObject é usado como popup.")]
    public GameObject raizPopup;

    [Header("Conteúdo")]
    public RawImage rawImagePraca;
    public TextMeshProUGUI txtNome;
    public TextMeshProUGUI txtData;
    public TextMeshProUGUI txtFoco;
    public TextMeshProUGUI txtItens;

    [Header("Animação de navegação")]
    [Tooltip("Arraste aqui o objecto 'pop-up azul'. Se ficar vazio, é procurado automaticamente.")]
    public RectTransform cartaoAnimado;
    [Min(1f)] public float distanciaDeslizamento = 70f;
    [Min(0.05f)] public float duracaoDeslizamento = 0.16f;

    [Header("Animação de abertura")]
    [Min(0.05f)] public float duracaoFadeAbertura = 0.18f;
    [Min(0.05f)] public float duracaoPopAbertura = 0.36f;

    [Header("Botões")]
    public Button btnFechar;
    public Button btnAnterior;
    public Button btnSeguinte;
    public Button btnEditar;
    public Button btnEliminar;

    private ArquivoManager arquivoManager;
    private List<BtnArquivoItem> itens = new List<BtnArquivoItem>();
    private int indiceActual;
    private CanvasGroup grupoCartao;
    private CanvasGroup grupoRaiz;
    private Vector2 posicaoOriginalCartao;
    private Vector3 escalaOriginalCartao;
    private bool escalaOriginalGuardada;
    private bool estaANavegar;

    private void Awake()
    {
        if (raizPopup == null)
            raizPopup = gameObject;

        if (btnFechar != null)
            btnFechar.onClick.AddListener(Fechar);
        if (btnAnterior != null)
            btnAnterior.onClick.AddListener(MostrarAnterior);
        if (btnSeguinte != null)
            btnSeguinte.onClick.AddListener(MostrarSeguinte);
        if (btnEditar != null)
            btnEditar.onClick.AddListener(AbrirPracaActual);
        if (btnEliminar != null)
            btnEliminar.onClick.AddListener(EliminarPracaActual);

        PrepararAnimacaoCartao();
    }

    public void Abrir(ArquivoManager gestor, List<BtnArquivoItem> itensDoArquivo, int indiceInicial)
    {
        // Awake ainda não correu se o próprio popup começar desactivado na cena.
        if (raizPopup == null)
            raizPopup = gameObject;

        arquivoManager = gestor;
        itens = itensDoArquivo ?? new List<BtnArquivoItem>();
        indiceActual = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, itens.Count - 1));

        AtualizarConteudo();
        raizPopup.SetActive(true);
        PrepararAnimacaoCartao();
        ReporPosicaoCartao();
        AnimarAbertura();
    }

    public void Fechar()
    {
        estaANavegar = false;
        if (cartaoAnimado != null)
            LeanTween.cancel(cartaoAnimado.gameObject);
        if (raizPopup != null)
            LeanTween.cancel(raizPopup);

        raizPopup.SetActive(false);
    }

    private void MostrarAnterior()
    {
        NavegarPara(indiceActual - 1, -1);
    }

    private void MostrarSeguinte()
    {
        NavegarPara(indiceActual + 1, 1);
    }

    private void AbrirPracaActual()
    {
        BtnArquivoItem item = ObterItemActual();
        if (item == null || arquivoManager == null)
            return;

        arquivoManager.AbrirPraca(item.caminhoArquivo, item.dados != null ? item.dados.nomeDaCena : "");
    }

    public void EliminarPracaActual()
    {
        BtnArquivoItem item = ObterItemActual();
        if (item == null || arquivoManager == null)
            return;

        // Se quiseres uma confirmação, liga este botão a um pequeno popup de
        // confirmação e chama este método apenas no botão "Eliminar" dele.
        arquivoManager.ExcluirArquivo(item.caminhoArquivo);
        Fechar();
    }

    private void AtualizarConteudo()
    {
        BtnArquivoItem item = ObterItemActual();
        JsonPayloadData dados = item != null ? item.dados : null;

        if (txtNome != null)
            txtNome.text = dados != null && !string.IsNullOrEmpty(dados.nomeDaCena)
                ? dados.nomeDaCena
                : "Praça desconhecida";

        if (txtData != null)
            txtData.text = "Última edição: " + (dados != null ? BtnArquivoItem.FormatarData(dados.dataCriacao) : "");

        if (rawImagePraca != null)
            rawImagePraca.texture = item != null ? item.miniatura : null;

        AtualizarTextoOpcional(txtFoco, dados != null ? dados.tituloDaPraca : "", "Título: ", "Sem título");
        AtualizarTextoOpcional(txtItens, dados != null ? dados.comentarioDaPraca : "", "", "Nenhum comentário adicionado");

        DefinirBotoesNavegacao(!estaANavegar);
    }

    private BtnArquivoItem ObterItemActual()
    {
        if (indiceActual < 0 || indiceActual >= itens.Count)
            return null;

        return itens[indiceActual];
    }

    private void NavegarPara(int novoIndice, int direccao)
    {
        if (estaANavegar || novoIndice < 0 || novoIndice >= itens.Count)
            return;

        PrepararAnimacaoCartao();
        if (cartaoAnimado == null || grupoCartao == null)
        {
            indiceActual = novoIndice;
            AtualizarConteudo();
            return;
        }

        estaANavegar = true;
        DefinirBotoesNavegacao(false);

        Vector2 destinoSaida = posicaoOriginalCartao + Vector2.left * direccao * distanciaDeslizamento;
        LeanTween.alphaCanvas(grupoCartao, 0f, duracaoDeslizamento).setEaseInQuad();
        AnimarPosicaoCartao(cartaoAnimado.anchoredPosition, destinoSaida, duracaoDeslizamento, LeanTweenType.easeInQuad, () =>
        {
            indiceActual = novoIndice;
            AtualizarConteudo();

            Vector2 origemEntrada = posicaoOriginalCartao + Vector2.right * direccao * distanciaDeslizamento;
            cartaoAnimado.anchoredPosition = origemEntrada;
            grupoCartao.alpha = 0f;

            LeanTween.alphaCanvas(grupoCartao, 1f, duracaoDeslizamento).setEaseOutQuad();
            AnimarPosicaoCartao(origemEntrada, posicaoOriginalCartao, duracaoDeslizamento, LeanTweenType.easeOutQuad, () =>
            {
                estaANavegar = false;
                DefinirBotoesNavegacao(true);
            });
        });
    }

    private void PrepararAnimacaoCartao()
    {
        if (cartaoAnimado == null && rawImagePraca != null)
            cartaoAnimado = rawImagePraca.transform.parent as RectTransform;

        if (cartaoAnimado == null)
            return;

        if (grupoCartao == null)
        {
            grupoCartao = cartaoAnimado.GetComponent<CanvasGroup>();
            if (grupoCartao == null)
                grupoCartao = cartaoAnimado.gameObject.AddComponent<CanvasGroup>();
        }

        posicaoOriginalCartao = cartaoAnimado.anchoredPosition;
        if (!escalaOriginalGuardada)
        {
            escalaOriginalCartao = cartaoAnimado.localScale;
            escalaOriginalGuardada = true;
        }
    }

    private void ReporPosicaoCartao()
    {
        if (cartaoAnimado == null)
            return;

        cartaoAnimado.anchoredPosition = posicaoOriginalCartao;
        if (grupoCartao != null)
            grupoCartao.alpha = 1f;
        if (escalaOriginalGuardada)
            cartaoAnimado.localScale = escalaOriginalCartao;
    }

    private void AnimarAbertura()
    {
        if (raizPopup == null || cartaoAnimado == null || !escalaOriginalGuardada)
            return;

        // A raiz contém o blur, os controlos e o cartão. O fade revela o conjunto;
        // o pop é aplicado só ao cartão central para preservar a estabilidade das setas.
        if (grupoRaiz == null)
        {
            grupoRaiz = raizPopup.GetComponent<CanvasGroup>();
            if (grupoRaiz == null)
                grupoRaiz = raizPopup.AddComponent<CanvasGroup>();
        }

        LeanTween.cancel(raizPopup);
        LeanTween.cancel(cartaoAnimado.gameObject);
        grupoRaiz.alpha = 0f;
        cartaoAnimado.localScale = escalaOriginalCartao * 0.92f;

        LeanTween.alphaCanvas(grupoRaiz, 1f, duracaoFadeAbertura).setEaseOutQuad();
        LeanTween.scale(cartaoAnimado, escalaOriginalCartao, duracaoPopAbertura).setEaseOutBack();
    }

    private void AnimarPosicaoCartao(Vector2 origem, Vector2 destino, float duracao, LeanTweenType curva, System.Action aoConcluir)
    {
        LeanTween.value(cartaoAnimado.gameObject, 0f, 1f, duracao)
            .setEase(curva)
            .setOnUpdate((float progresso) => cartaoAnimado.anchoredPosition = Vector2.LerpUnclamped(origem, destino, progresso))
            .setOnComplete(aoConcluir);
    }

    private void DefinirBotoesNavegacao(bool activos)
    {
        if (btnAnterior != null)
            btnAnterior.interactable = activos && indiceActual > 0;
        if (btnSeguinte != null)
            btnSeguinte.interactable = activos && indiceActual < itens.Count - 1;
    }

    private static void AtualizarTextoOpcional(TextMeshProUGUI texto, string conteudo, string prefixo, string textoVazio)
    {
        if (texto == null)
            return;

        bool temConteudo = !string.IsNullOrWhiteSpace(conteudo);
        texto.gameObject.SetActive(true);
        texto.text = temConteudo ? prefixo + conteudo : textoVazio;

        Color cor = texto.color;
        cor.a = temConteudo ? 1f : 0.8f;
        texto.color = cor;
    }
}
