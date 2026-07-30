using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

public class DeepLinkManager : MonoBehaviour
{
    // A instância garante que este script não se duplica
    public static DeepLinkManager Instance { get; private set; }

    [Header("UI Feedback")]
    public GameObject painelLoading; // Opcional: Arraste um texto/painel de "A baixar praça..." aqui

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Avisa a Unity para executar a nossa função sempre que um link for clicado com o app aberto
            Application.deepLinkActivated += onDeepLinkActivated;

            // Impede que o "escutador" seja destruído ao mudar de cena
            DontDestroyOnLoad(gameObject);

            // Verifica se o jogo foi ABERTO a partir do link (estava fechado antes)
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                onDeepLinkActivated(Application.absoluteURL);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void onDeepLinkActivated(string url)
    {
        // O URL será: https://seusite.com/abrir-app/?id=xxxx
        if (url.Contains("/abrir-app/?id="))
        {
            // Extrai o ID
            string idDaPraca = url.Split(new string[] { "?id=" }, StringSplitOptions.None)[1];
            Debug.Log("Universal Link recebido! ID: " + idDaPraca);

            if (painelLoading != null) painelLoading.SetActive(true);

            SupabaseManager db = FindObjectOfType<SupabaseManager>();

            if (db != null)
            {
                // Espera o Supabase conectar
                while (!db.isReady)
                {
                    await Task.Yield(); // Espera 1 frame e verifica de novo
                }

                var dados = await db.BaixarDadosDaPraca(idDaPraca);

                if (dados != null)
                {
                    PlayerPrefs.SetString("PracaRemixJSON", dados.LayoutData);
                    PlayerPrefs.Save();

                    string cenaParaAbrir = dados.SceneName;
                    if (string.IsNullOrEmpty(cenaParaAbrir)) cenaParaAbrir = "Barão de Corumba";

                    if (painelLoading != null) painelLoading.SetActive(false);
                    SceneManager.LoadSceneAsync(cenaParaAbrir);
                }
            }
        }
    }
}