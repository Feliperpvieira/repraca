using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static NativeGallery;
using PostHogUnity;
using UnityEngine.UI;

public class CameraCapture : MonoBehaviour
{
    [Header("Cameras para gerar imagens")]
    public RenderTexture rtVistaTopo; //render texture
    public RenderTexture rtVistaAngulo;
    public GameObject cameraTopo; //cameras que geram a textura
    public GameObject cameraAngulo;

    [Header("UI pre e pos exportar")]
    public GameObject telaExportar;
    public GameObject telaSiteGaleria;
    public GameObject painelAnimadoSucesso;
    public GameObject canvasPrincipal;
    public TMP_InputField tituloPraca;
    public TMP_InputField comentarioPraca;

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

    private Vector3 tamanhoOrgPainel;
    private CanvasGroup grupoTelaExportar;
    private RectTransform conteudoTelaExportar;
    private Vector3 tamanhoOrgConteudoExportar;


    // Função Start para encontrar o BuildingManager quando a cena carrega
    void Start()
    {
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
        // Encontra o SupabaseManager na cena (GameObject chama "SupabaseManager")
        supabaseManager = GameObject.Find("SupabaseManager").GetComponent<SupabaseManager>();

        tamanhoOrgPainel = painelAnimadoSucesso.transform.localScale; //salva o tamanho do painel de conclusao na UI
        PrepararAnimacaoTelaExportar();

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
        // Isto tem de ocorrer antes do primeiro await. O botão também chama
        // SalvarPracaLocalmente logo a seguir, pela ordem configurada no Inspector.
        GuardarMetadadosIntroduzidos();

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

        string sceneName = buildingManager.pracaAtual.id;

        byte[] imagemTopo = toTexture2D(rtVistaTopo, 1200, 1200).EncodeToJPG(); //transforma a renderTexture em texture 2d
        string fileName = ScreenShotName(sceneName, "topo"); //define o nome do arquivo
        //System.IO.File.WriteAllBytes(fileName, bytes);

        //Debug.Log(string.Format("Took screenshot to: {0}", fileName));

        byte[] imagemAngulo = toTexture2D(rtVistaAngulo, 1920, 1200).EncodeToJPG(); //transforma a renderTexture em texture 2d
        string fileNameAng = ScreenShotName(sceneName, "angulo"); //define o nome do arquivo

        if (PlayerPrefs.GetInt("SalvarGaleria") == 1) // config de salvar na galeria do dispositivo
        {
            //metodo antigo falhava no iOS, substituido pelo abaixo com espera
            //NativeGallery.SaveImageToGallery(imagemTopo, album, fileName, callback); //plugin native gallery https://github.com/yasirkula/UnityNativeGallery
            //NativeGallery.SaveImageToGallery(imagemAngulo, album, fileNameAng, callback);

            //  fazer o código esperar
            TaskCompletionSource<bool> esperaPrimeiraImagem = new TaskCompletionSource<bool>();

            // salvar a imagem de Topo
            NativeGallery.SaveImageToGallery(imagemTopo, album, fileName, (sucesso, caminho) =>
            {
                esperaPrimeiraImagem.SetResult(sucesso);
            });

            // pausa aqui em background (sem travar o jogo) até o iOS terminar
            await esperaPrimeiraImagem.Task;

            // a imagem de Ângulo
            NativeGallery.SaveImageToGallery(imagemAngulo, album, fileNameAng, callback);
        }

        // 3. LÓGICA DE UPLOAD COM FEEDBACK
        if (buildingManager != null && supabaseManager != null)
        {
            tituloLoading.text = "Enviando para a nuvem...";
            textoLoading.text = "Isto pode levar alguns segundos, aguarde.";

            string jsonPronto = buildingManager.GerarJsonDaPraca();
            int totalDeObjetos = buildingManager.objetosPosicionados.Count;

            //posthog: exportou e quantos objetos na praça exportada
            PostHog.Capture("export_completed", new Dictionary<string, object>
            {
                { "total_objects", buildingManager.objetosPosicionados.Count }
            });

            estaAEnviar = true; // Liga a barra de progresso no Update

            // ESPERA O UPLOAD TERMINAR E GUARDA O RESULTADO (true/false)

            // ADICIONADO A FUNÇÃO DE PROGRESSO NO FINAL PARA A BARRA FUNCIONAR

            // 1. Obtém todos os IDs necessários do BuildingManager
            string idJogador = buildingManager.idJogador;
            string idDaPraca = buildingManager.idDaPracaAtual;
            string idDoPai = buildingManager.idDaPracaPai;
            string mapaId = buildingManager.pracaAtual != null ? buildingManager.pracaAtual.id : "";


            // 2. Chama a função passando os argumentos
            bool uploadSucesso = await supabaseManager.UploadCreationData(
                idJogador,
                idDaPraca,
                idDoPai,
                mapaId,
                jsonPronto,
                imagemAngulo,
                imagemTopo,
                totalDeObjetos,
                buildingManager.tituloDaPraca,
                buildingManager.comentarioDaPraca,
                (progresso) => { progressoAtual = progresso; }
            );

            estaAEnviar = false; // Desliga a barra de progresso no Update

            // 4. FEEDBACK FINAL
            if (uploadSucesso)
            {
                tituloLoading.text = "<color=#98AB56>Praça exportada com sucesso!</color>";
                textoLoading.text = ":)";

                //botaoSalvar.SetActive(false);
                telaExportar.SetActive(false);

                // ativa tudo, com o blur de fundo
                telaSiteGaleria.SetActive(true);

                // ANIMA APENAS O PAINEL DO MEIO
                if (painelAnimadoSucesso != null)
                {
                    
                    painelAnimadoSucesso.transform.localScale = Vector3.zero; 
                    LeanTween.cancel(painelAnimadoSucesso);
                    LeanTween.scale(painelAnimadoSucesso, tamanhoOrgPainel, 0.4f).setEaseOutBack();
                }
                
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

    // Função para abrir a tela de exportar
    public void SalvarExportar()
    {
        if (buildingManager != null)
        {
            // Ao reabrir um arquivo, repõe os metadados já guardados.
            if (tituloPraca != null)
                tituloPraca.text = buildingManager.tituloDaPraca;
            if (comentarioPraca != null)
                comentarioPraca.text = buildingManager.comentarioDaPraca;
        }

        telaExportar.SetActive(true);
        canvasPrincipal.SetActive(false);
        cameraTopo.SetActive(true);
        cameraAngulo.SetActive(true);

        PrepararAnimacaoTelaExportar();
        LeanTween.cancel(telaExportar);
        LeanTween.cancel(conteudoTelaExportar.gameObject);
        grupoTelaExportar.alpha = 0f;
        conteudoTelaExportar.localScale = tamanhoOrgConteudoExportar * 0.92f;
        LeanTween.alphaCanvas(grupoTelaExportar, 1f, 0.18f).setEaseOutQuad();
        LeanTween.scale(conteudoTelaExportar, tamanhoOrgConteudoExportar, 0.36f).setEaseOutBack();
    }

    // Função para fechar a tela de exportar e voltar à edição
    public void VoltarEdicao()
    {
        GuardarMetadadosIntroduzidos();
        PrepararAnimacaoTelaExportar();

        LeanTween.cancel(telaExportar);
        LeanTween.cancel(conteudoTelaExportar.gameObject);
        LeanTween.alphaCanvas(grupoTelaExportar, 0f, 0.14f).setEaseInQuad();
        LeanTween.scale(conteudoTelaExportar, tamanhoOrgConteudoExportar * 0.96f, 0.14f).setEaseInQuad()
            .setOnComplete(() =>
            {
                telaExportar.SetActive(false);
                canvasPrincipal.SetActive(true);
                cameraTopo.SetActive(false);
                cameraAngulo.SetActive(false);
            });
    }

    private void GuardarMetadadosIntroduzidos()
    {
        if (buildingManager == null)
            return;

        buildingManager.DefinirMetadadosDaPraca(
            tituloPraca != null ? tituloPraca.text : "",
            comentarioPraca != null ? comentarioPraca.text : ""
        );
    }

    private void PrepararAnimacaoTelaExportar()
    {
        if (telaExportar == null)
            return;

        if (grupoTelaExportar == null)
        {
            grupoTelaExportar = telaExportar.GetComponent<CanvasGroup>();
            if (grupoTelaExportar == null)
                grupoTelaExportar = telaExportar.AddComponent<CanvasGroup>();
        }

        if (conteudoTelaExportar == null)
        {
            Transform conteudo = telaExportar.transform.Find("conteudo");
            conteudoTelaExportar = conteudo as RectTransform;
            if (conteudoTelaExportar != null)
                tamanhoOrgConteudoExportar = conteudoTelaExportar.localScale;
        }
    }

    // Função para ser chamada pelo botão de fechar/OK da tela de sucesso
    public void FecharTelaSucesso()
    {
        if (telaSiteGaleria.activeInHierarchy)
        {
            if (painelAnimadoSucesso != null)
            {
                LeanTween.cancel(painelAnimadoSucesso);

                // O painel do meio encolhe primeiro...
                LeanTween.scale(painelAnimadoSucesso, Vector3.zero, 0.2f)
                    .setEaseInQuad()
                    .setOnComplete(() =>
                    {
                        // Quando terminar de encolher, desliga a tela toda (tirando o blur da frente)
                        telaSiteGaleria.SetActive(false);

                        // ...e regressa a UI principal
                        if (canvasPrincipal != null)
                        {
                            canvasPrincipal.SetActive(true);
                            CanvasGroup cg = canvasPrincipal.GetComponent<CanvasGroup>();
                            if (cg == null)
                            {
                                cg = canvasPrincipal.AddComponent<CanvasGroup>();
                            }

                            cg.alpha = 0f;
                            LeanTween.alphaCanvas(cg, 1f, 0.3f).setEaseOutQuad();
                        }
                    });
            }
        }
    }
}
