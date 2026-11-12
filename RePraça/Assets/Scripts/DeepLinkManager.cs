using System;
using System.Threading.Tasks;
using TMPro;
using PostHogUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeepLinkManager : MonoBehaviour
{
    private enum TipoAbertura { Remix, MapaLimpo }

    private struct PedidoAbertura
    {
        public TipoAbertura tipo;
        public string valor;

        public PedidoAbertura(TipoAbertura tipo, string valor)
        {
            this.tipo = tipo;
            this.valor = valor;
        }
    }

    public static DeepLinkManager Instance { get; private set; }

    [Header("UI Feedback")]
    public GameObject painelLoading;
    public CanvasGroup canvasGroupLoading;
    public GameObject conteudoLoading;
    public TextMeshProUGUI tituloLoading;
    public TextMeshProUGUI textoLoading;
    public GameObject iconeLoading;
    public GameObject iconeErro;

    [Header("Painel de Erro (p Erro)")]
    [Tooltip("Se ficar vazio, o erro continua a aparecer no painel de loading, como antes.")]
    public GameObject painelErro;
    public CanvasGroup canvasGroupErro;
    public GameObject conteudoErro;
    public TextMeshProUGUI tituloErro;
    public TextMeshProUGUI textoErro;

    [Header("Sistema de Aviso (Sobrescrita)")]
    public GameObject painelAviso;
    public string nomeCenaMenu = "telaInicial";

    [Header("Cena única do jogo")]
    public string nomeCenaJogo = "Jogo";

    [Header("Praças disponíveis")]
    [Tooltip("O mesmo catálogo atribuído ao BuildingManager. Valida links ?mapa= antes de abrir o jogo.")]
    public PracaCatalogo catalogoPracas;

    // Só grava PlayerPrefs quando a abertura é confirmada.
    private TipoAbertura? tipoAberturaPendente;
    private string layoutJsonPendente;
    private string mapaIdPendente;
    private bool downloadCancelado;
    private bool estaProcessandoLink;

    private string ultimoLinkRecebido;
    private float ultimoTimestampRecebido = -999f;
    private const float JANELA_DUPLICADO_SEGUNDOS = 3f;
    private static bool linkInicialJaVerificado;

    // Dados do último erro, pro e-mail de suporte pré-preenchido
    private const string EMAIL_SUPORTE = "repraca@feliperpv.com";
    private string linkDaFalha;
    private string motivoDaFalha;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.deepLinkActivated += OnDeepLinkActivated;
        DontDestroyOnLoad(gameObject);

        if (!linkInicialJaVerificado && !string.IsNullOrEmpty(Application.absoluteURL))
        {
            linkInicialJaVerificado = true;
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
            Instance = null;
        }
    }

    // Na 1ª abertura da app, segura o fluxo até a pessoa responder ao painel de RGPD
    private async Task AguardarConsentimentoAsync()
    {
        if (AnalyticsManager.Instance == null) return;

        while (!AnalyticsManager.Instance.JaRespondeu)
            await Task.Yield();
    }

    // Único ponto de entrada:
    // ?id=<uuid>   abre um remix salvo no servidor.
    // ?mapa=<slug> abre uma praça limpa do catálogo local.
    private async void OnDeepLinkActivated(string url)
    {
        if (!TentarLerPedido(url, out PedidoAbertura pedido))
        {
            if (EhLinkDeAbertura(url))
            {
                linkDaFalha = url;
                RegistrarFalhaLink("desconhecido", "invalid_link");
                MostrarPracaNaoEncontrada();
            }
            return;
        }

        if (estaProcessandoLink)
        {
            Debug.LogWarning("[DeepLinkManager] Já existe um link em processamento — novo pedido ignorado.");
            return;
        }

        if (url == ultimoLinkRecebido &&
            Time.realtimeSinceStartup - ultimoTimestampRecebido < JANELA_DUPLICADO_SEGUNDOS)
        {
            Debug.LogWarning("[DeepLinkManager] Link duplicado ignorado.");
            return;
        }

        ultimoLinkRecebido = url;
        ultimoTimestampRecebido = Time.realtimeSinceStartup;
        estaProcessandoLink = true;
        linkDaFalha = url;
        downloadCancelado = false;

        try
        {
            bool preparado = pedido.tipo == TipoAbertura.Remix
                ? await PrepararRemixAsync(pedido.valor)
                : PrepararMapaLimpo(pedido.valor);

            if (!preparado || downloadCancelado)
                return;

            FecharPainelLoading();

            // fecha o loading primeiro para não cobrir o painel de consentimento
            await AguardarConsentimentoAsync();
            if (downloadCancelado)
                return;

            // só agora o evento conta, se a pessoa aceitou
            PostHog.Capture(pedido.tipo == TipoAbertura.Remix ? "remix_clicked" : "clean_map_link_clicked");

            PedirConfirmacaoOuAbrir();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DeepLinkManager] Falha ao abrir link: {exception}");
            MostrarErro("Erro ao abrir a praça.", "Tente novamente mais tarde.");
            RegistrarFalhaLink(pedido.tipo == TipoAbertura.Remix ? "remix" : "mapa_limpo", "exception");
        }
        finally
        {
            estaProcessandoLink = false;
        }
    }

    private bool PrepararMapaLimpo(string mapaId)
    {
        PracaData praca = ObterPracaDoCatalogo(mapaId);
        if (praca == null)
        {
            RegistrarFalhaLink("mapa_limpo", "not_found");
            MostrarPracaNaoEncontrada();
            return false;
        }

        tipoAberturaPendente = TipoAbertura.MapaLimpo;
        mapaIdPendente = praca.id;
        layoutJsonPendente = null;
        return true;
    }

    private async Task<bool> PrepararRemixAsync(string idDaPraca)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            MostrarErro("Sem conexão!", "Verifique o seu Wi-Fi ou dados para transferir a praça.");
            RegistrarFalhaLink("remix", "no_internet");
            return false;
        }

        AbrirPainelLoading("A conectar ao servidor...", "A preparar para descarregar a praça.", true);

        SupabaseManager db = FindFirstObjectByType<SupabaseManager>();
        if (db == null)
        {
            MostrarErro("Erro interno.", "O gestor de base de dados não está a responder.");
            RegistrarFalhaLink("remix", "internal_error");
            return false;
        }

        while (!db.isReady)
        {
            if (downloadCancelado)
                return false;
            await Task.Yield();
        }

        AtualizarTextosLoading("A transferir dados...", "A obter os dados da galeria.");
        SupabaseManager.CityCreationModel dados = await db.BaixarDadosDaPraca(idDaPraca);

        if (downloadCancelado)
            return false;

        // Valida o layout antes de qualquer aviso ou troca de cena: se for ilegível,
        // mostra o erro e a pessoa continua onde estava.
        if (dados == null || !LayoutValido(dados.LayoutData))
        {
            RegistrarFalhaLink("remix", dados == null ? "not_found" : "invalid_layout");
            MostrarPracaNaoEncontrada();
            return false;
        }

        if (ObterPracaDoCatalogo(ObterMapaDoRemix(dados)) == null)
        {
            RegistrarFalhaLink("remix", "map_not_in_catalog");
            MostrarPracaNaoEncontrada();
            return false;
        }

        tipoAberturaPendente = TipoAbertura.Remix;
        layoutJsonPendente = dados.LayoutData;
        mapaIdPendente = null;
        AtualizarTextosLoading("<color=#98AB56>Praça transferida!</color>", "A preparar o ambiente...");
        return true;
    }

    private PracaData ObterPracaDoCatalogo(string identificador)
    {
        if (catalogoPracas == null)
        {
            Debug.LogError("[DeepLinkManager] PracaCatalogo não foi configurado no Inspector.");
            MostrarErro("Erro interno.", "O catálogo de praças não está disponível.");
            return null;
        }

        return catalogoPracas.ObterPorIdentificador(identificador);
    }

    private static bool LayoutValido(string layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson))
            return false;

        try
        {
            return JsonUtility.FromJson<JsonPayloadData>(layoutJson) != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string ObterMapaDoRemix(SupabaseManager.CityCreationModel dados)
    {
        if (!string.IsNullOrWhiteSpace(dados.MapaId))
            return dados.MapaId;

        try
        {
            JsonPayloadData layout = JsonUtility.FromJson<JsonPayloadData>(dados.LayoutData);
            return !string.IsNullOrWhiteSpace(layout?.mapaId) ? layout.mapaId : layout?.nomeDaCena;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void PedirConfirmacaoOuAbrir()
    {
        if (SceneManager.GetActiveScene().name != nomeCenaMenu && painelAviso != null)
        {
            painelAviso.SetActive(true);
            PostHog.Capture("overwrite_warning_shown");
            return;
        }

        GravarESeguirParaCena();
    }

    private void GravarESeguirParaCena()
    {
        if (!tipoAberturaPendente.HasValue)
            return;

        PlayerPrefs.DeleteKey("PracaRemixJSON");
        PlayerPrefs.DeleteKey("PracaIdNova");

        if (tipoAberturaPendente.Value == TipoAbertura.Remix)
        {
            if (string.IsNullOrEmpty(layoutJsonPendente)) return;
            PlayerPrefs.SetString("PracaRemixJSON", layoutJsonPendente);
            PostHog.Capture("remix_opened");
        }
        else
        {
            if (string.IsNullOrEmpty(mapaIdPendente)) return;
            PlayerPrefs.SetString("PracaIdNova", mapaIdPendente);
            PostHog.Capture("clean_map_opened");
        }

        PlayerPrefs.Save();
        LimparPedidoPendente();
        SceneManager.LoadSceneAsync(nomeCenaJogo);
    }

    private void LimparPedidoPendente()
    {
        tipoAberturaPendente = null;
        layoutJsonPendente = null;
        mapaIdPendente = null;
    }

    private static bool TentarLerPedido(string url, out PedidoAbertura pedido)
    {
        pedido = default;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || !EhCaminhoAbrirApp(uri))
            return false;

        string id = ObterParametro(uri, "id");
        string mapa = ObterParametro(uri, "mapa");

        if (!string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(mapa))
        {
            pedido = new PedidoAbertura(TipoAbertura.Remix, id);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(mapa) && string.IsNullOrWhiteSpace(id))
        {
            pedido = new PedidoAbertura(TipoAbertura.MapaLimpo, mapa);
            return true;
        }

        return false;
    }

    private static bool EhLinkDeAbertura(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && EhCaminhoAbrirApp(uri);
    }

    private static bool EhCaminhoAbrirApp(Uri uri)
    {
        return uri.AbsolutePath.TrimEnd('/').EndsWith("/abrir-app", StringComparison.OrdinalIgnoreCase);
    }

    private static string ObterParametro(Uri uri, string nome)
    {
        string[] pares = uri.Query.TrimStart('?').Split('&');
        foreach (string par in pares)
        {
            int separador = par.IndexOf('=');
            if (separador < 0) continue;

            string chave = Uri.UnescapeDataString(par.Substring(0, separador).Replace("+", " "));
            if (!string.Equals(chave, nome, StringComparison.OrdinalIgnoreCase)) continue;

            return Uri.UnescapeDataString(par.Substring(separador + 1).Replace("+", " ")).Trim();
        }

        return null;
    }

    //posthog: o link não abriu. Se a pessoa ainda não deu consentimento, o
    //PostHog está em OptOut e o evento é descartado (comportamento esperado).
    private void RegistrarFalhaLink(string tipo, string motivo)
    {
        motivoDaFalha = motivo; // vai no e-mail de suporte pré-preenchido
        PostHog.Capture("deep_link_failed", new System.Collections.Generic.Dictionary<string, object>
        {
            { "tipo", tipo },
            { "reason", motivo }
        });
    }

    private void MostrarPracaNaoEncontrada()
    {
        // O e-mail vira um link do TMP com id curto "suporte" (o TMP não aceita tags
        // compridas). O TextoLinkEmail no texto chama CriarMailtoSuporte() ao tocar.
        MostrarErro(
            "Praça não encontrada.",
            "O link pode ser inválido ou esta praça ainda não está disponível. Se acredita que isto é um erro, contacte " +
            $"<link=\"suporte\"><u>{EMAIL_SUPORTE}</u></link>.");
    }

    // mailto já com assunto e corpo preenchidos. Sem o ID do jogador (RGPD).
    public string CriarMailtoSuporte()
    {
        string corpo =
            "Olá! Tentei abrir uma praça pelo link e apareceu \"Praça não encontrada\".\n\n" +
            "(escreva aqui o que aconteceu, se quiser)\n\n" +
            "---\n" +
            $"Link: {linkDaFalha}\n" +
            $"Motivo: {motivoDaFalha}\n" +
            $"Versão da app: {Application.version}\n" +
            $"Sistema: {SystemInfo.operatingSystem}\n" +
            $"Aparelho: {SystemInfo.deviceModel}";

        return "mailto:" + EMAIL_SUPORTE +
               "?subject=" + Uri.EscapeDataString("rePraça: praça não encontrada") +
               "&body=" + Uri.EscapeDataString(corpo);
    }

    private void AbrirPainelLoading(string titulo, string mensagem, bool isLoading)
    {
        if (painelLoading == null)
        {
            Debug.LogError("[DeepLinkManager] painelLoading está nulo/destruído nesta cena.");
            return;
        }

        if (!painelLoading.activeInHierarchy)
        {
            painelLoading.SetActive(true);

            if (canvasGroupLoading != null)
            {
                canvasGroupLoading.alpha = 0f;
                LeanTween.alphaCanvas(canvasGroupLoading, 1f, 0.3f).setEaseOutQuad();
            }

            if (conteudoLoading != null)
            {
                conteudoLoading.transform.localScale = Vector3.zero;
                LeanTween.scale(conteudoLoading, Vector3.one, 0.3f).setEaseOutBack();
            }
        }

        if (iconeLoading != null) iconeLoading.SetActive(isLoading);
        if (iconeErro != null) iconeErro.SetActive(!isLoading);
        AtualizarTextosLoading(titulo, mensagem);
    }

    private void AtualizarTextosLoading(string titulo, string mensagem)
    {
        if (tituloLoading != null) tituloLoading.text = titulo;
        if (textoLoading != null) textoLoading.text = mensagem;
    }

    private void MostrarErro(string titulo, string mensagem)
    {
        // Sem p Erro ligado no Inspector: mantém o comportamento antigo.
        if (painelErro == null)
        {
            AbrirPainelLoading(titulo, mensagem, false);
            return;
        }

        // Fecha o loading (se estiver aberto) e mostra o erro no painel próprio.
        FecharPainelLoading();

        if (tituloErro != null) tituloErro.text = titulo;
        if (textoErro != null) textoErro.text = mensagem;

        if (painelErro.activeInHierarchy) return; // já aberto: só troca o texto

        painelErro.SetActive(true);

        // Mesma animação de entrada do painel de loading
        if (canvasGroupErro != null)
        {
            LeanTween.cancel(canvasGroupErro.gameObject);
            canvasGroupErro.alpha = 0f;
            LeanTween.alphaCanvas(canvasGroupErro, 1f, 0.3f).setEaseOutQuad();
        }

        if (conteudoErro != null)
        {
            LeanTween.cancel(conteudoErro);
            conteudoErro.transform.localScale = Vector3.zero;
            LeanTween.scale(conteudoErro, Vector3.one, 0.3f).setEaseOutBack();
        }
    }

    // Ligar ao btn-fechar do p Erro
    public void FecharErro()
    {
        if (painelErro == null || !painelErro.activeInHierarchy) return;

        LimparPedidoPendente();

        // Mesma animação de saída do painel de loading
        if (canvasGroupErro != null)
        {
            LeanTween.cancel(canvasGroupErro.gameObject);
            LeanTween.alphaCanvas(canvasGroupErro, 0f, 0.2f).setEaseOutQuad();
        }

        if (conteudoErro != null)
        {
            LeanTween.cancel(conteudoErro);
            LeanTween.scale(conteudoErro, Vector3.zero, 0.2f)
                .setEaseInBack()
                .setOnComplete(() => painelErro.SetActive(false));
        }
        else
        {
            LeanTween.delayedCall(0.2f, () => painelErro.SetActive(false));
        }
    }

    public void CancelarOuFecharLoading()
    {
        downloadCancelado = true;
        LimparPedidoPendente();
        FecharPainelLoading();
    }

    private void FecharPainelLoading()
    {
        if (painelLoading == null || !painelLoading.activeInHierarchy) return;

        if (canvasGroupLoading != null) LeanTween.cancel(canvasGroupLoading.gameObject);
        if (conteudoLoading != null) LeanTween.cancel(conteudoLoading);
        if (canvasGroupLoading != null)
            LeanTween.alphaCanvas(canvasGroupLoading, 0f, 0.2f).setEaseOutQuad();

        if (conteudoLoading != null)
        {
            LeanTween.scale(conteudoLoading, Vector3.zero, 0.2f)
                .setEaseInBack()
                .setOnComplete(() => painelLoading.SetActive(false));
        }
        else
        {
            LeanTween.delayedCall(0.2f, () => painelLoading.SetActive(false));
        }
    }

    public void ConfirmarAberturaDeLink()
    {
        if (painelAviso != null) painelAviso.SetActive(false);
        PostHog.Capture("overwrite_confirmed"); //posthog: aceitou trocar a praça aberta pelo link
        GravarESeguirParaCena();
    }

    public void CancelarAberturaDeLink()
    {
        if (painelAviso != null) painelAviso.SetActive(false);
        PostHog.Capture("overwrite_cancelled"); //posthog: preferiu ficar na praça aberta
        LimparPedidoPendente();
        PlayerPrefs.DeleteKey("PracaRemixJSON");
        PlayerPrefs.DeleteKey("PracaIdNova");
    }

    [Header("Teste no PC")]
    public string urlDeTeste = "https://feliperpv.com/repraca/galeria/abrir-app/?id=d4810622-64a8-4fd0-a1ce-14ce422f6a9c";

    [ContextMenu("▶ Simular Clique no Link")]
    public void SimularDeepLink()
    {
        if (Application.isPlaying)
            OnDeepLinkActivated(urlDeTeste);
        else
            Debug.LogWarning("Dê Play no jogo primeiro antes de testar o link!");
    }
}
