using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using TMPro;
using PostHogUnity;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }

    [Header("UI Feedback")]
    public GameObject painelLoading;
    public CanvasGroup canvasGroupLoading;
    public GameObject conteudoLoading;
    public TextMeshProUGUI tituloLoading;
    public TextMeshProUGUI textoLoading;
    public GameObject iconeLoading;
    public GameObject iconeErro;

    [Header("Sistema de Aviso (Sobrescrita)")]
    public GameObject painelAviso;
    public string nomeCenaMenu = "Menu";

    private string cenaParaCarregarPend;

    // Guarda o JSON baixado APENAS em memória. Só é gravado no PlayerPrefs
    // no exato momento em que a cena vai carregar de facto — assim, se a app
    // fechar com o painel de aviso ainda na tela (sem confirmar nem descartar),
    // nada fica gravado no disco e não sobra "praça fantasma" na próxima abertura.
    private string layoutJsonPendente;

    // Flag para cancelar o processo se o utilizador clicar no X
    private bool downloadCancelado = false;

    // Impede que dois links sejam processados ao mesmo tempo
    private bool estaProcessandoLink = false;

    // Impede reprocessar o MESMO id recebido há poucos segundos.
    // Isto é comum no Android/iOS: ao voltar o foco para a app, o sistema
    // pode reentregar o mesmo Intent/URL, disparando deepLinkActivated de novo.
    private string ultimoIdRecebido = null;
    private float ultimoTimestampRecebido = -999f;
    private const float JANELA_DUPLICADO_SEGUNDOS = 3f;

    // Garante que a URL inicial (Application.absoluteURL) só é lida UMA VEZ
    // em toda a sessão da app, mesmo que Awake() volte a correr por algum motivo.
    private static bool linkInicialJaVerificado = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Application.deepLinkActivated += onDeepLinkActivated;
            DontDestroyOnLoad(gameObject);

            if (!linkInicialJaVerificado)
            {
                linkInicialJaVerificado = true;

                if (!String.IsNullOrEmpty(Application.absoluteURL))
                {
                    onDeepLinkActivated(Application.absoluteURL);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void onDeepLinkActivated(string url)
    {
        Debug.Log($"[DeepLinkManager] onDeepLinkActivated chamado às {Time.realtimeSinceStartup:F2}s | URL: {url}");

        PostHog.Capture("remix_clicked");

        if (!url.Contains("/abrir-app/?id="))
            return;

        string idDaPraca = url.Split(new string[] { "?id=" }, StringSplitOptions.None)[1];

        // --- Guarda 1: já existe um link a ser processado agora mesmo? ---
        if (estaProcessandoLink)
        {
            Debug.LogWarning("[DeepLinkManager] Já existe um link em processamento — novo pedido ignorado.");
            return;
        }

        // --- Guarda 2: é o mesmo ID recebido há poucos segundos? (reentrega do SO) ---
        if (idDaPraca == ultimoIdRecebido && (Time.realtimeSinceStartup - ultimoTimestampRecebido) < JANELA_DUPLICADO_SEGUNDOS)
        {
            Debug.LogWarning($"[DeepLinkManager] Link duplicado ignorado (mesmo ID '{idDaPraca}' recebido há menos de {JANELA_DUPLICADO_SEGUNDOS}s).");
            return;
        }

        ultimoIdRecebido = idDaPraca;
        ultimoTimestampRecebido = Time.realtimeSinceStartup;
        estaProcessandoLink = true;
        downloadCancelado = false;

        Debug.Log("Universal Link recebido! ID: " + idDaPraca);

        try
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                MostrarErro("Sem conexão!", "Verifique o seu Wi-Fi ou Dados para transferir a praça.");
                return;
            }

            AbrirPainelLoading("A conectar ao servidor...", "A preparar para descarregar a praça.", true);

            SupabaseManager db = FindObjectOfType<SupabaseManager>();

            if (db == null)
            {
                MostrarErro("<color=#B76F51>Erro interno.</color>", "O gestor de base de dados não está a responder.");
                return;
            }

            // Espera o Supabase conectar
            while (!db.isReady)
            {
                if (downloadCancelado) return;
                await Task.Yield();
            }

            AtualizarTextosLoading("A transferir dados...", "A obter os dados da galeria.");

            var dados = await db.BaixarDadosDaPraca(idDaPraca);

            if (downloadCancelado) return;

            if (dados == null)
            {
                MostrarErro("<color=#B76F51>Praça não encontrada.</color>", "O link pode ser inválido ou a praça foi eliminada.");
                return;
            }

            layoutJsonPendente = dados.LayoutData;

            cenaParaCarregarPend = dados.SceneName;
            if (string.IsNullOrEmpty(cenaParaCarregarPend)) cenaParaCarregarPend = "Barão de Corumba";

            AtualizarTextosLoading("<color=#98AB56>Praça transferida!</color>", "A preparar o ambiente...");

            //// Pausa manual para a UI ler "Sucesso" (também pode ser cancelada no X)
            //float timer = 0;
            //while (timer < 0.8f)
            //{
            //    if (downloadCancelado) return;
            //    timer += Time.deltaTime;
            //    await Task.Yield();
            //}

            FecharPainelLoading();

            // Lógica original de aviso e carregamento de cena
            if (SceneManager.GetActiveScene().name != nomeCenaMenu)
            {
                if (painelAviso != null)
                {
                    painelAviso.SetActive(true);
                }
                else
                {
                    Debug.LogError("[DeepLinkManager] painelAviso está nulo/destruído nesta cena. " +
                        "O painel provavelmente só existe na cena Menu e foi destruído ao trocar de cena. " +
                        "Mova-o para o mesmo GameObject persistente do DeepLinkManager.");
                }
                PostHog.Capture("overwrite_warning_shown");
            }
            else
            {
                GravarESeguirParaCena();
                PostHog.Capture("remix_opened");
            }
        }
        finally
        {
            estaProcessandoLink = false;
        }
    }

    // Só agora, no momento em que a cena vai carregar de facto, o JSON é
    // persistido no PlayerPrefs — nunca antes disso.
    private void GravarESeguirParaCena()
    {
        if (string.IsNullOrEmpty(cenaParaCarregarPend) || string.IsNullOrEmpty(layoutJsonPendente))
            return;

        PlayerPrefs.SetString("PracaRemixJSON", layoutJsonPendente);
        PlayerPrefs.Save();

        SceneManager.LoadSceneAsync(cenaParaCarregarPend);

        cenaParaCarregarPend = null;
        layoutJsonPendente = null;
    }

    // ==========================================
    // CONTROLO DE UI E ANIMAÇÕES
    // ==========================================

    private void AbrirPainelLoading(string titulo, string mensagem, bool isLoading)
    {
        if (painelLoading == null)
        {
            Debug.LogError("[DeepLinkManager] painelLoading está nulo/destruído nesta cena. " +
                "Verifique se o painel de loading está a persistir entre cenas (DontDestroyOnLoad).");
            return;
        }

        if (!painelLoading.activeInHierarchy)
        {
            painelLoading.SetActive(true);

            // Fade in do fundo
            if (canvasGroupLoading != null)
            {
                canvasGroupLoading.alpha = 0f;
                LeanTween.alphaCanvas(canvasGroupLoading, 1f, 0.3f).setEaseOutQuad();
            }

            // Efeito elástico da caixa
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
        AbrirPainelLoading(titulo, mensagem, false);
    }

    // ==========================================
    // CANCELAMENTO / FECHAR (BOTÃO X)
    // ==========================================

    public void CancelarOuFecharLoading()
    {
        // Diz à função de download para morrer e limpa qualquer cena pendente
        downloadCancelado = true;
        cenaParaCarregarPend = null;
        layoutJsonPendente = null;
        FecharPainelLoading();
    }

    private void FecharPainelLoading()
    {
        if (painelLoading != null && painelLoading.activeInHierarchy)
        {
            if (canvasGroupLoading != null) LeanTween.cancel(canvasGroupLoading.gameObject);
            if (conteudoLoading != null) LeanTween.cancel(conteudoLoading);

            // Fade out do fundo
            if (canvasGroupLoading != null)
            {
                LeanTween.alphaCanvas(canvasGroupLoading, 0f, 0.2f).setEaseOutQuad();
            }

            // Efeito de fecho na caixa de conteúdo
            if (conteudoLoading != null)
            {
                LeanTween.scale(conteudoLoading, Vector3.zero, 0.2f)
                    .setEaseInBack()
                    .setOnComplete(() =>
                    {
                        painelLoading.SetActive(false);
                    });
            }
            else
            {
                LeanTween.delayedCall(0.2f, () => { painelLoading.SetActive(false); });
            }
        }
    }

    // ==========================================
    // FUNÇÕES PARA OS BOTÕES DO POP-UP DE AVISO
    // ==========================================

    public void ConfirmarAberturaDeLink()
    {
        PostHog.Capture("overwrite_current-plaza_discarded");
        if (painelAviso != null) painelAviso.SetActive(false);

        GravarESeguirParaCena();
    }

    public void CancelarAberturaDeLink()
    {
        PostHog.Capture("overwrite_current-plaza_maintained");
        if (painelAviso != null) painelAviso.SetActive(false);

        cenaParaCarregarPend = null;
        layoutJsonPendente = null;
        // Segurança extra: garante que não fica nada residual gravado no disco
        PlayerPrefs.DeleteKey("PracaRemixJSON");
    }


    // ==========================================
    // ÁREA DE TESTES NO UNITY EDITOR
    // ==========================================

    [Header("Teste no PC")]
    public string urlDeTeste = "https://feliperpv.com/repraca/galeria/abrir-app/?id=d4810622-64a8-4fd0-a1ce-14ce422f6a9c";

    // O [ContextMenu] cria um botão escondido no menu de opções do script no Inspector
    [ContextMenu("▶ Simular Clique no Link")]
    public void SimularDeepLink()
    {
        if (Application.isPlaying)
        {
            // Força a execução da função principal como se o sistema operativo a tivesse chamado
            onDeepLinkActivated(urlDeTeste);
        }
        else
        {
            Debug.LogWarning("Dê Play no jogo primeiro antes de testar o link!");
        }
    }
}