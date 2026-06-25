using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static NativeGallery;

public class CameraCapture : MonoBehaviour
{
    [Header("Cameras para gerar imagens")]
    public RenderTexture rtVistaTopo;
    public RenderTexture rtVistaAngulo;

    [Header("UI pre e pos exportar")]
    public GameObject telaExportar;
    public GameObject telaSiteGaleria;
    
    string album = "rePraca";
    MediaSaveCallback callback = null;

    // Referência para o BuildingManager
    private BuildingManager buildingManager;
    // Referência para o SupabaseManager
    private SupabaseManager supabaseManager;

    [Header("Upload progress UI Feedback")]
    public GameObject painelLoading; //painel com o status de loading pro server
    public TextMeshProUGUI tituloLoading;
    public TextMeshProUGUI textoLoading;
    public GameObject iconeLoading;
    public GameObject iconeErro;

    // Referências para a barra de progresso e o texto da percentagem
    public UnityEngine.UI.Slider barraProgresso;
    public TextMeshProUGUI textoPorcentagem;
    private float progressoAtual = 0f;
    private bool estaAEnviar = false;


    // Função Start para encontrar o BuildingManager quando a cena carrega
    void Start()
    {
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
        // Encontra o SupabaseManager na cena (GameObject chama "SupabaseManager")
        supabaseManager = GameObject.Find("SupabaseManager").GetComponent<SupabaseManager>();
    }

    public static string ScreenShotName(string nomeCena, string angulo) //define o nome do arquivo
    {
        /*return string.Format("praca_{0}-{1}_{2}.png",
                               nomeCena, angulo,
                               System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")); //data e hora atual*/

        return string.Format("praca_{0}-{1}.png",
                               nomeCena, angulo);

        //return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
        //                     Application.persistentDataPath,
        //                     width, height,
        //                     System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    void Update()
    {
        if (estaAEnviar && barraProgresso != null) //se o upload estiver ocorrendo
        {
            // O Supabase devolve um número de 0 a 100
            barraProgresso.value = progressoAtual;

            // O Mathf.RoundToInt arredonda para não mostrar casas decimais (ex: 45%)
            if (textoPorcentagem != null)
                textoPorcentagem.text = Mathf.RoundToInt(progressoAtual) + "%";
        }
    }

    // ADICIONADO O 'async' AQUI!
    public async void SaveTexture()
    {
        // 1. CHECAGEM DE INTERNET
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            painelLoading.SetActive(true);
            iconeLoading.SetActive(false);
            iconeErro.SetActive(true);
            barraProgresso.gameObject.SetActive(false);
            textoPorcentagem.gameObject.SetActive(false);
            tituloLoading.text = "Sem conexão com a internet!";
            textoLoading.text = "Verifique seu Wi-Fi ou Dados.";
            //await Task.Delay(3000); // Espera 3 segundos para o jogador ler
            //painelLoading.SetActive(false);
            return; // PARA A EXECUÇÃO AQUI (não tenta fazer upload sem internet)
        }

        // 2. ATIVA A UI DE LOADING
        //botaoSalvar.SetActive(false);

        painelLoading.SetActive(true);
        iconeLoading.SetActive(true);
        iconeErro.SetActive(false);
        barraProgresso.gameObject.SetActive(true);
        textoPorcentagem.gameObject.SetActive(true);
        tituloLoading.text = "Gerando imagens...";

        string sceneName = SceneManager.GetActiveScene().name;

        byte[] imagemTopo = toTexture2D(rtVistaTopo, 1200, 1200).EncodeToJPG(); //transforma a renderTexture em texture 2d
        string fileName = ScreenShotName(sceneName, "topo"); //define o nome do arquivo
        //System.IO.File.WriteAllBytes(fileName, bytes);
        NativeGallery.SaveImageToGallery(imagemTopo, album, fileName, callback); //plugin native gallery https://github.com/yasirkula/UnityNativeGallery

        //Debug.Log(string.Format("Took screenshot to: {0}", fileName));

        byte[] imagemAngulo = toTexture2D(rtVistaAngulo, 1920, 1200).EncodeToJPG(); //transforma a renderTexture em texture 2d
        string fileNameAng = ScreenShotName(sceneName, "angulo"); //define o nome do arquivo
        NativeGallery.SaveImageToGallery(imagemAngulo, album, fileNameAng, callback);

        // Chama a função de gerar o JSON logo após salvar as imagens!
        if (buildingManager != null)
        {
            buildingManager.GerarJsonDaPraca();
        }
        else
        {
            Debug.LogError("BuildingManager não encontrado no CameraCapture!");
        }

        // 3. LÓGICA DE UPLOAD COM FEEDBACK
        if (buildingManager != null && supabaseManager != null)
        {
            tituloLoading.text = "Enviando para a nuvem...";
            textoLoading.text = "Isto pode levar alguns segundos, aguarde.";

            string jsonPronto = buildingManager.GerarJsonDaPraca();
            int totalDeObjetos = buildingManager.objetosPosicionados.Count;

            estaAEnviar = true; // Liga a barra de progresso no Update

            // ESPERA O UPLOAD TERMINAR E GUARDA O RESULTADO (true/false)
            // ADICIONADO A FUNÇÃO DE PROGRESSO NO FINAL PARA A BARRA FUNCIONAR
            bool uploadSucesso = await supabaseManager.UploadCreationData("Visitante", jsonPronto, imagemAngulo, imagemTopo, totalDeObjetos, (progresso) => { progressoAtual = progresso; });

            estaAEnviar = false; // Desliga a barra de progresso no Update

            // 4. FEEDBACK FINAL
            if (uploadSucesso)
            {
                tituloLoading.text = "<color=#98AB56>Praça exportada com sucesso!</color>";
                textoLoading.text = ":)";

                //botaoSalvar.SetActive(false);
                telaExportar.SetActive(false);
                telaSiteGaleria.SetActive(true);
            }
            else
            {
                tituloLoading.text = "<color=#B76F51>Erro no servidor.</color>";
                textoLoading.text = "Confira sua conexão e tente novamente.";
                // Espera 2 segundos para o usuário ler a mensagem de sucesso/erro (DESCOMENTADO PARA A UI FUNCIONAR BEM)
                await Task.Delay(2500);
                //botaoSalvar.SetActive(true); // Reativa o botão se quiserem tentar de novo
            }

            painelLoading.SetActive(false);
        }
        else
        {
            Debug.LogError("BuildingManager ou SupabaseManager não encontrados no CameraCapture!");
        }

        
    }

    Texture2D toTexture2D(RenderTexture rTex, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        Destroy(tex);//prevents memory leak
        return tex;
    }
}