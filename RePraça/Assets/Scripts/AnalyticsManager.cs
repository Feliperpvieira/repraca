using UnityEngine;
using PostHogUnity;

// vai antes de qualquer outro script pra garantir que a decisão de consentimento já esteja aplicada antes que qualquer PostHog.Capture ser chamado
[DefaultExecutionOrder(-1000)]
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [Header("Painel de consentimento (primeira utilização)")]
    // o painel deve ser filho do mesmo GameObject que o script pra sobreviver às trocas de cena junto com o DontDestroyOnLoad
    public GameObject painelConsentimento;

    [Header("Config PostHog")]
    // ATENÇÃO: desativa "Auto Initialize" no PostHogSettings e remove/mova o
    // PostHogSettings.asset do Resources — senão o SDK inicializa duas vezes.
    // Preenche estes campos com os mesmos valores que tinhas no Inspector.
    public string apiKey;
    public string host = "https://eu.i.posthog.com";

    private const string CHAVE_JA_PERGUNTADO = "AnalyticsJaPerguntado";
    private const string CHAVE_CONSENTIU = "AnalyticsConsentiu";

    private const string CHAVE_PLAYER_UUID = "PlayerUUID";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup() manual em vez de depender do Auto Initialize do
            // PostHogSettings.asset. O Auto Initialize corre via
            // RuntimeInitializeOnLoadMethod, que o Unity executa ANTES do
            // Awake de qualquer script da cena — inclusive antes deste,
            // mesmo com DefaultExecutionOrder(-1000). É por isso que o
            // evento "Application Opened" (o log do utilizador X a entrar)
            // já ia para o PostHog antes de teres hipótese de chamar
            // OptOut(). Ao chamar Setup() aqui, manualmente, a inicialização
            // só acontece depois de já sabermos se há consentimento.
            PostHog.Setup(new PostHogConfig
            {
                ApiKey = apiKey,
                Host = host,
                FlushAt = 20,
                FlushIntervalSeconds = 30,
                MaxQueueSize = 1000,
                MaxBatchSize = 50,
                CaptureApplicationLifecycleEvents = true,
                CaptureExceptions = true,
                PersonProfiles = PersonProfiles.IdentifiedOnly,
                PreloadFeatureFlags = true,
                SendFeatureFlagEvent = true,
                LogLevel = PostHogLogLevel.Warning
            });

            Debug.Log($"[Analytics] PostHog configurado (host: {host}).");


            InicializarConsentimento();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    

    private void InicializarConsentimento()
    {
        bool jaPerguntado = PlayerPrefs.GetInt(CHAVE_JA_PERGUNTADO, 0) == 1;

        if (!jaPerguntado)
        {
            // Primeira vez a abrir a app: fica desligado por padrão até
            // a pessoa responder ao painel — nunca manda evento nenhum
            // antes disso.
            Debug.Log("[Analytics] Primeira utilização — a aguardar resposta do painel de consentimento.");

            PostHog.OptOut();
            if (painelConsentimento != null) painelConsentimento.SetActive(true);
            return;
        }

        // Já respondeu antes — aplica a escolha guardada, sem mostrar nada
        Debug.Log($"[Analytics] Consentimento salvo de uma sessão anterior: {(EstaConsentido() ? "aceite" : "recusado")}. A reaplicar.");

        AplicarConsentimento(EstaConsentido());
    }

    // Ligar ao botão "Sim" do painel
    public void AceitarAnalytics()
    {
        GuardarEAplicar(true);
        FecharPainel();
    }

    // Ligar ao botão "Não" do painel
    public void RecusarAnalytics()
    {
        GuardarEAplicar(false);
        FecharPainel();
    }

    // Ligar ao Toggle das Definições (o próprio onValueChanged do Toggle
    // já entrega um bool, então dá pra ligar direto sem código extra)
    public void DefinirConsentimento(bool consentiu)
    {
        GuardarEAplicar(consentiu);
    }

    // Útil pra definir o estado inicial do Toggle ao abrir a tela de Definições
    public bool EstaConsentido()
    {
        return PlayerPrefs.GetInt(CHAVE_CONSENTIU, 0) == 1;
    }

    private void GuardarEAplicar(bool consentiu)
    {
        PlayerPrefs.SetInt(CHAVE_JA_PERGUNTADO, 1);
        PlayerPrefs.SetInt(CHAVE_CONSENTIU, consentiu ? 1 : 0);
        PlayerPrefs.Save();

        AplicarConsentimento(consentiu);
    }

    private async void AplicarConsentimento(bool consentiu)
    {
        if (consentiu)
        {
            // liga o tracking primeiro, para o IdentifyAsync já ir associado ao utilizador
            PostHog.OptIn();
            await IdentificarUtilizadorAsync();
        }
        else
        {
            // desfaz qualquer identificação anterior antes de desligar, para não
            // ficar nenhum vínculo entre o distinct_id antigo e este dispositivo
            await PostHog.ResetAsync();
            PostHog.OptOut();
        }
    }

    // busca (ou cria) o ID anónimo do dispositivo e associa-o ao PostHog.
    // só é chamado depois de haver consentimento — nunca antes.
    private async System.Threading.Tasks.Task IdentificarUtilizadorAsync()
    {
        string userIdAnonimo = PlayerPrefs.GetString(CHAVE_PLAYER_UUID, System.Guid.NewGuid().ToString());
        PlayerPrefs.SetString(CHAVE_PLAYER_UUID, userIdAnonimo);
        PlayerPrefs.Save();

        await PostHog.IdentifyAsync(userIdAnonimo);
    }

    private void FecharPainel()
    {
        if (painelConsentimento != null) painelConsentimento.SetActive(false);
    }
}