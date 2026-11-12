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
    // Para desselecionar objetos e voltar a dia ao abrir a tela de guardar
    private SelectionManager selectionManager;
    private DiaNoite iluminacaoManager;

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
    private bool estaASalvar = false; // evita salvar duas vezes com toque duplo

    [Header("Painel de opções de salvar")]
    public OpcoesSalvarPraca opcoesSalvar;

    [Header("Botão 'Ver galeria' do ecrã de sucesso")]
    public OpenURL linkVerGaleria;          // o OpenURL do botão "Ver galeria"
    public string urlGaleria = "https://repraca.feliperpv.com/galeria/praca/"; // + {mapaId}/?id={pracaId}

    [Header("Ecrã de sucesso: textos e ícones por modo")]
    public TMP_Text tituloSucesso;
    public TMP_Text textoSucesso;
    public TMP_Text textoBotaoGaleria;
    public GameObject iconePublicada;       // nuvem
    public GameObject iconeGuardadaLocal;   // dispositivo / disquete
    [TextArea] public string tituloPublicada = "Sucesso!";
    [TextArea] public string textoPublicada = "A tua imaginação foi publicada na galeria de praças e já pode ser vista junto de todas as outras!";
    public string botaoPublicada = "Ver a minha praça";
    [TextArea] public string tituloLocal = "Praça guardada!";
    [TextArea] public string textoLocal = "A tua imaginação ficou guardada neste dispositivo e podes voltar a ela no Arquivo. Entretanto, vê o que outras pessoas imaginaram para esta praça.";
    public string botaoLocal = "Ver outras imaginações";

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
        // Mesmos nomes de GameObject que o BuildingManager usa
        GameObject sm = GameObject.Find("SelectManager");
        if (sm != null) selectionManager = sm.GetComponent<SelectionManager>();
        GameObject im = GameObject.Find("IluminacaoManager");
        if (im != null) iluminacaoManager = im.GetComponent<DiaNoite>();

        tamanhoOrgPainel = painelAnimadoSucesso.transform.localScale; //salva o tamanho do painel de conclusao na UI
        PrepararAnimacaoTelaExportar();

    }

    public static string ScreenShotName(string nomeCena, string angulo) //define o nome do arquivo
    {
        /*return string.Format("praca_{0}-{1}_{2}.png",
                               nomeCena, angulo,
                               System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")); //data e hora atual*/

        return string.Format("praca_{0}-{1}.jpg", // os bytes são JPG (EncodeToJPG)
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

    // Chamado pelos botões de salvar/exportar (substitui o par
    // SaveTexture + SalvarPracaLocalmente que estava no Inspector).
    // Faz tudo por esta ordem: cópia -> salvar no dispositivo -> galeria de fotos -> upload.
    public async void ConfirmarSalvar()
    {
        if (estaASalvar || buildingManager == null)
            return;
        estaASalvar = true;

        try
        {
            GuardarMetadadosIntroduzidos();

            // 1. SALVAR COMO CÓPIA: troca o id antes de qualquer gravação,
            // para o arquivo local e o upload já usarem o id novo.
            if (opcoesSalvar != null && opcoesSalvar.SalvarComoCopia && buildingManager.PodeSalvarComoCopia())
                buildingManager.TransformarEmCopia();

            // 2. SALVAR NO DISPOSITIVO (sempre)
            buildingManager.SalvarPracaLocalmente();

            // A cópia já está gravada: a partir daqui o jogador está a editá-la,
            // por isso o toggle volta a desligado (e fica disponível).
            if (opcoesSalvar != null)
                opcoesSalvar.AtualizarOpcaoCopia();

            bool exportarOnline = OpcoesSalvarPraca.ExportarOnline;
            bool salvarGaleria = OpcoesSalvarPraca.SalvarGaleria;

            // 3. CHECAGEM DE INTERNET (só se for enviar para a galeria online)
            if (exportarOnline && Application.internetReachability == NetworkReachability.NotReachable)
            {
                painelLoading.SetActive(true);
                iconeLoading.SetActive(false);
                iconeErro.SetActive(true);
                barraProgresso.gameObject.SetActive(false);
                textoPorcentagem.gameObject.SetActive(false);
                tituloLoading.text = "Sem conexão com a internet!";
                textoLoading.text = "Verifique seu Wi-Fi ou Dados.";

                PostHog.Capture("export_failed", new Dictionary<string, object>
                {
                    { "reason", "no_internet" }
                });
                return; // PARA A EXECUÇÃO AQUI (não tenta fazer upload sem internet)
            }

            // 4. ATIVA A UI DE LOADING
            painelLoading.SetActive(true);
            iconeLoading.SetActive(true);
            iconeErro.SetActive(false);
            barraProgresso.gameObject.SetActive(exportarOnline);
            textoPorcentagem.gameObject.SetActive(exportarOnline);
            tituloLoading.text = "Gerando imagens...";
            textoLoading.text = "";

            // As imagens grandes só são precisas para a galeria de fotos e para o upload
            byte[] imagemTopo = null;
            byte[] imagemAngulo = null;
            if (salvarGaleria || exportarOnline)
            {
                imagemTopo = toTexture2D(rtVistaTopo, 1200, 1200).EncodeToJPG(50); //transforma a renderTexture em texture 2d
                imagemAngulo = toTexture2D(rtVistaAngulo, 1440, 900).EncodeToJPG(50);
            }

            // 5. GALERIA DE FOTOS DO DISPOSITIVO
            if (salvarGaleria)
            {
                string sceneName = buildingManager.pracaAtual.id;
                string fileName = ScreenShotName(sceneName, "topo"); //define o nome do arquivo
                string fileNameAng = ScreenShotName(sceneName, "angulo");

                //metodo antigo falhava no iOS, substituido pelo abaixo com espera
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

            // 6a. SÓ LOCAL: mesmo ecrã de sucesso, com textos de "guardada" e link para a galeria da praça
            if (!exportarOnline)
            {
                painelLoading.SetActive(false);
                MostrarTelaSucesso(false, buildingManager.pracaAtual != null ? buildingManager.pracaAtual.id : "", "");
                return;
            }

            // 6b. LÓGICA DE UPLOAD COM FEEDBACK
            if (supabaseManager != null)
            {
                tituloLoading.text = "Enviando para a nuvem...";
                textoLoading.text = "Isto pode levar alguns segundos, aguarde.";

                string jsonPronto = buildingManager.GerarJsonDaPraca();
                int totalDeObjetos = buildingManager.objetosPosicionados.Count;

                progressoAtual = 0f;
                estaAEnviar = true; // Liga a barra de progresso no Update

                // Obtém todos os IDs necessários do BuildingManager
                string idJogador = buildingManager.idJogador;
                string idDaPraca = buildingManager.idDaPracaAtual;
                string idDoPai = buildingManager.idDaPracaPai;
                string mapaId = buildingManager.pracaAtual != null ? buildingManager.pracaAtual.id : "";

                // ESPERA O UPLOAD TERMINAR E GUARDA O RESULTADO (true/false)
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

                // 7. FEEDBACK FINAL
                if (uploadSucesso)
                {
                    //posthog: exportou e quantos objetos na praça exportada (só quando o upload deu certo)
                    PostHog.Capture("export_completed", new Dictionary<string, object>
                    {
                        { "total_objects", totalDeObjetos }
                    });

                    tituloLoading.text = "<color=#98AB56>Praça exportada com sucesso!</color>";
                    textoLoading.text = ":)";

                    MostrarTelaSucesso(true, mapaId, idDaPraca);
                }
                else
                {
                    PostHog.Capture("export_failed", new Dictionary<string, object>
                    {
                        { "reason", "server_error" },
                        { "total_objects", totalDeObjetos }
                    });

                    tituloLoading.text = "<color=#B76F51>Erro no servidor.</color>";
                    textoLoading.text = "Confira sua conexão e tente novamente.";
                    // Espera para o usuário ler a mensagem de erro
                    await Task.Delay(2500);
                }

                painelLoading.SetActive(false);
            }
            else
            {
                Debug.LogError("SupabaseManager não encontrado no CameraCapture!");
                painelLoading.SetActive(false);
            }
        }
        finally
        {
            estaAEnviar = false;
            estaASalvar = false;
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

        PostHog.Capture("export_screen_opened"); //posthog: abriu a tela de exportar

        // Imagem limpa: sem objeto selecionado (outline) e sempre de dia
        if (selectionManager != null && selectionManager.selectedObject != null)
            selectionManager.Deselect();
        if (iluminacaoManager != null)
            iluminacaoManager.ForcarDia();

        // Painel de opções fechado e opções atualizadas (a cópia mantém-se até guardar)
        if (opcoesSalvar != null)
            opcoesSalvar.PrepararParaNovaExportacao();

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
        PostHog.Capture("export_screen_closed"); //posthog: voltou pra edição sem exportar
        GuardarMetadadosIntroduzidos();
        FecharTelaExportar();
    }

    // Animação de saída da tela de exportar. Usada pelo "voltar" e depois de
    // salvar só no dispositivo (aí sem o evento export_screen_closed).
    private void FecharTelaExportar()
    {
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

    // Ecrã de sucesso partilhado: publicada (link para a praça) ou só guardada (link para a galeria da praça)
    private void MostrarTelaSucesso(bool publicada, string mapaId, string idDaPraca)
    {
        if (tituloSucesso != null) tituloSucesso.text = publicada ? tituloPublicada : tituloLocal;
        if (textoSucesso != null) textoSucesso.text = publicada ? textoPublicada : textoLocal;
        if (textoBotaoGaleria != null) textoBotaoGaleria.text = publicada ? botaoPublicada : botaoLocal;
        if (iconePublicada != null) iconePublicada.SetActive(publicada);
        if (iconeGuardadaLocal != null) iconeGuardadaLocal.SetActive(!publicada);

        if (linkVerGaleria != null)
        {
            string url = urlGaleria + System.Uri.EscapeDataString(mapaId) + "/";
            if (publicada && !string.IsNullOrEmpty(idDaPraca))
                url += "?id=" + System.Uri.EscapeDataString(idDaPraca);
            linkVerGaleria.url = url;
        }

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

                        // As câmaras das imagens já não são precisas (o "voltar" da tela de exportar também as desliga)
                        cameraTopo.SetActive(false);
                        cameraAngulo.SetActive(false);

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
