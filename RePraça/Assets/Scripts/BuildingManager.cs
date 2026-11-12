using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using PostHogUnity;
using UnityEngine.SceneManagement;

public class BuildingManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject painelObjetos; //painel com botoes e info pra adicionar objetos
    public GameObject botaoAddObjetos; //botao de adicionar novos objetos
    public GameObject interfaceTopoSistema; //botoes do topo da tela
    public Button botaoConcluir; //botao de exportar praça, para deixar inativo quando mexe objeto
    private Vector3 escalaOriginalPainelObjetos;
    public GameObject painelObjetosConteudoAnimado;
    //[SerializeField] private Material[] materialPlacement; //materiais pra indicar por cor se pode ou não colocar um novo objeto ali - substituido por outline

    [Header("Cameras para gerar imagens")] 
    public RenderTexture rtVistaTopo; //mesma camera do camera capture

    private Vector3 pos; //posição do obj
    [SerializeField] private Camera cameraPrincipal;

    [SerializeField] private LayerMask layerMask;

    [Header("stuff")]
    public float rotateAmount;

    public float gridSize;
    bool gridOn;
    public bool canPlace = true;
    [SerializeField] private Toggle gridToggle;

    private SelectionManager selectionManager;
    private DiaNoite iluminacaoManager;

    [Header("Praças")]
    public PracaCatalogo catalogo;        // arrastar o asset PracaCatalogo no Inspector
    public CameraPanZoom cameraPanZoom;   // arrastar o script da câmera principal

    [Header("Praças (gerida pelo jogo)")]
    public PracaData pracaAtual;          // preenchido em runtime, nunca no Inspector

    [Header("o jogo gere")]
    //public List<string> objetosPosicionados = new List<string>(); //forma antiga de guardar o que estava adicionado na cena
    public List<ObjetoPosicionadoData> objetosPosicionados = new List<ObjetoPosicionadoData>();
    public ObjetosData dadosDoObjetoPendente; // Nova variável para guardar os dados da UI
    //public GameObject[] objects; //lista de objetos - todos os objetos ficavam aqui e eles eram construídos pelo seu index
    public GameObject pendingObject; //objeto selecionado

    public string idJogador; //identificacao unica, anonima e aleatoria para cada dispositivo
 
    public string idDaPracaAtual = ""; //numero random pra identificar a praça criada localmente e no server
    public string idDaPracaPai = ""; //se for um fork de outra praça, salva a original
    [HideInInspector] public string tituloDaPraca = "";
    [HideInInspector] public string comentarioDaPraca = "";

    [Header("Cena do menu (para voltar se o JSON for inválido)")]
    [SerializeField] private string nomeCenaMenu = "telaInicial";

    void Start()
    {
        // Resolve, nesta ordem de prioridade, o que carregar:
        // 1) Arquivo local salvo (botão Arquivo)
        // 2) JSON de remix vindo de um link (galeria/deep link)
        // 3) Praça nova escolhida no Menu, ainda sem nenhum objeto posicionado
        string jsonParaCarregar = null;
        bool isRemix = false;

        if (PlayerPrefs.HasKey("PracaParaCarregar"))
        {
            string caminho = PlayerPrefs.GetString("PracaParaCarregar");
            if (File.Exists(caminho))
                jsonParaCarregar = File.ReadAllText(caminho);
            PlayerPrefs.DeleteKey("PracaParaCarregar");
        }
        else if (PlayerPrefs.HasKey("PracaRemixJSON"))
        {
            jsonParaCarregar = PlayerPrefs.GetString("PracaRemixJSON");
            isRemix = true;
            PlayerPrefs.DeleteKey("PracaRemixJSON");
        }

        string mapaId = null;
        if (jsonParaCarregar != null)
        {
            JsonPayloadData dadosLidos = null;
            try
            {
                dadosLidos = JsonUtility.FromJson<JsonPayloadData>(jsonParaCarregar);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BuildingManager] JSON da praça inválido: " + e.Message);
            }

            // Rede de segurança: o arquivo local e o remix já são validados antes
            // de chegar aqui. Se mesmo assim vier algo ilegível, volta pro menu em
            // vez de abrir uma praça qualquer.
            if (dadosLidos == null)
            {
                SceneManager.LoadScene(nomeCenaMenu);
                return;
            }

            mapaId = ResolverMapaId(dadosLidos);
        }
        else if (PlayerPrefs.HasKey("PracaIdNova"))
        {
            mapaId = PlayerPrefs.GetString("PracaIdNova");
            PlayerPrefs.DeleteKey("PracaIdNova");
        }

        StartCoroutine(IniciarPraca(mapaId, jsonParaCarregar, isRemix));
    }


    void Awake()
    {
        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;

        //coloca o objeto SelectManager da scene na variavel do codigo
        selectionManager = GameObject.Find("SelectManager").GetComponent<SelectionManager>();
        iluminacaoManager = GameObject.Find("IluminacaoManager").GetComponent<DiaNoite>(); //pega o script DiaNoite dentro do gameobject iluminacao manager

        escalaOriginalPainelObjetos = painelObjetosConteudoAnimado.transform.localScale; //guarda o tamanho do painel de objetos definido no editor
    }

    void Update()
    {
        if (Input.touchCount > 1) return; // Se o usuário estiver fazendo zoom com 2 dedos, não mexe o objeto

        // Verifica se estamos a clicar na tela real ou num botão da UI
        bool isPointerOverUI = false;
        bool isInputAtivo = false;
        Vector3 cursorPosition = Input.mousePosition;

        if (Input.touchCount > 0)
        {
            // Checa UI para toques na tela (Mobile)
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            isInputAtivo = true;
            cursorPosition = Input.GetTouch(0).position;
        }
        else
        {
            // Checa UI para cliques do Mouse (PC)
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

            // No PC, queremos que o objeto siga o mouse sempre. 
            // No mobile, se não houver toques na tela, não movemos o objeto!
            if (Application.isMobilePlatform)
            {
                isInputAtivo = Input.GetMouseButton(0);
            }
            else
            {
                isInputAtivo = true;
            }
        }

        // Só calcula o raycast enquanto há um objeto a posicionar. Isto evita
        // trabalho contínuo quando a praça está apenas a ser navegada.
        if (pendingObject != null && isInputAtivo && !isPointerOverUI && cameraPrincipal != null)
        {
            Ray ray = cameraPrincipal.ScreenPointToRay(cursorPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, layerMask))
            {
                pos = hit.point;
            }
        }

        if (pendingObject != null) //checa se existe um objeto selecionado
        {
            botaoAddObjetos.SetActive(false);
            botaoConcluir.interactable = false; //nao da pra exportar a praça se selecionar algo
            UpdateMaterials(); //atualiza a cor pra definir se pode ou nao colocar lá

            if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer) //se for uma plataforma com touchscreen
            {
                if (Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId) && Input.GetTouch(0).phase != TouchPhase.Ended) //checa se o toque esta batendo em um botao
                {
                    MoveObjectOnMap(); //se NAO estiver tocando num botao atualiza a posicao do objeto no mapa
                }

            }
            else if (!EventSystem.current.IsPointerOverGameObject()) //else se estiver no pc usa o ponteiro do mouse
            {
                MoveObjectOnMap(); //se o ponteiro do mouse NAO estiver sobre um botao atualiza a posicao do objeto no mapa
            }

            if (Input.GetKeyDown(KeyCode.P)) //se apertar P & canPlace for true
            {
                PlaceObject();
            }

            if (Input.GetKeyDown(KeyCode.R)) //se apertar a tecla R ele gira o objeto
            {
                RotateObject();
            }
        }
        else if (pendingObject == null)
        {
            //checa true ou false se a interface do topo da tela esta ativa
            bool nenhumPainelAberto = interfaceTopoSistema.activeInHierarchy; //atribui o estado true ou false a variavel

            botaoAddObjetos.SetActive(nenhumPainelAberto); //o botao add objetos segue o mesmo estado do topo da tela
            botaoConcluir.interactable = true;
        }
    }

    void MoveObjectOnMap()
    {
        selectionManager.Select(pendingObject); //resseleciona o objeto a cada movimento pra impedir que acabe selecionando outro objeto durante a movimentação

        if (gridOn) //se a grid estiver ligada
        {
            //pega a posição de cada coord do mouse e arredonda elas
            pendingObject.transform.position = new Vector3(
                RoundToNearestGrid(pos.x),
                //RoundToNearestGrid(pos.y),
                pos.y,
                RoundToNearestGrid(pos.z)
                );
        }
        else //se a grid estiver desligada move o objeto livremente 
        {
            pendingObject.transform.position = pos; //movimenta o objeto
        }
    }

    public void PlaceObject()
    {
        //pendingObject.GetComponent<MeshRenderer>().material = materialPlacement[2]; //define a cor final ao posicionar o objeto
        if (canPlace)
        {
            // 1. Procura se o objeto já existe na lista (caso ele esteja apenas sendo movido)
            // Se ele já existir, o nome do GameObject vai ser igual ao ID dele salvo na lista
            ObjetoPosicionadoData objetoExistente = objetosPosicionados.Find(item => item.id == pendingObject.name);

            string nomePosthog = ""; //salva o nome do objeto para o posthog

            if (objetoExistente != null)
            {
                // SE ELE JÁ EXISTE (está sendo movido), apenas atualiza a posição e rotação
                objetoExistente.posicao = pendingObject.transform.position;
                objetoExistente.rotacao = pendingObject.transform.eulerAngles;

                nomePosthog = objetoExistente.nome; 
            }
            else
            {
                // SE ELE NÃO EXISTE (é um objeto novo), cria um novo pacote de dados
                ObjetoPosicionadoData novoObjeto = new ObjetoPosicionadoData();
                novoObjeto.id = System.Guid.NewGuid().ToString(); // Cria o ID único
                novoObjeto.nome = pendingObject.name; // Pega o nome do prefab (ex: "Banco")
                novoObjeto.categoria = dadosDoObjetoPendente.categoria; // Pega a categoria
                novoObjeto.posicao = pendingObject.transform.position;
                novoObjeto.rotacao = pendingObject.transform.eulerAngles;

                nomePosthog = novoObjeto.nome;

                // Salva o ID no nome do GameObject para facilitar na hora de deletar e mover
                pendingObject.name = novoObjeto.id;

                // Adiciona na lista
                objetosPosicionados.Add(novoObjeto);
            }

            //posthog: salva que um objeto foi posicionado e qual era
            PostHog.Capture("object_placed", new Dictionary<string, object>
            {
                { "item_name", nomePosthog }
            });

            pendingObject = null; //o objeto que estava selecionado não tá selecionado mais
            selectionManager.Deselect();
        }
        else if (pendingObject != null)
        {
            // FEEDBACK VISUAL DE ERRO: O objeto dá uma "tremidinha"
            if (!LeanTween.isTweening(pendingObject)) //evita que rode se ja estiver durante uma animacao
            {
                LeanTween.rotateAroundLocal(pendingObject, Vector3.up, 15f, 0.05f).setLoopPingPong(3); // Gira 15 graus super rápido (0.05s) no próprio eixo e volta (ping-pong) 3 vezes
            }

        }

    }

    public void RotateObject()
    {
        pendingObject.transform.Rotate(Vector3.up, rotateAmount); //up -> gira no y, rotateAmount -> variavel definida lá em cima

        PostHog.Capture("object_rotated", new Dictionary<string, object>
        {
            { "item_name", NomeDoObjeto(pendingObject) }
        }); //posthog rodar
    }

    void UpdateMaterials()
    {
        Outline outline = pendingObject.GetComponent<Outline>();
        if (canPlace)
        {
            //se canPlace for true, coloca o material 0 do array
            //pendingObject.GetComponent<MeshRenderer>().material = materialPlacement[0];
            outline.OutlineColor = Color.green;
        }
        if (!canPlace)
        {
            //se for false coloca o material 1
            //pendingObject.GetComponent<MeshRenderer>().material = materialPlacement[1];
            outline.OutlineColor = Color.red;
        }
    }

    public void SelectObject(GameObject objeto) //seleciona o objeto, é chamado pelo BotaoManager, o objeto é o prefab que ta no scriptable object dados
    {
        // Calcula o centro exato do que a câmera está a ver neste momento, para que os novos objetos apareçam no meio da tela
        RaycastHit hitCentro;

        if (cameraPrincipal != null)
        {
            Ray raioCentro = cameraPrincipal.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            if (Physics.Raycast(raioCentro, out hitCentro, 1000f, layerMask))
                pos = hitCentro.point; // Define a posição de spawn para o meio da tela
        }


        // O Unity não chama OnTriggerExit quando um objeto é destruído. Se o último
        // objeto foi apagado enquanto estava vermelho, o canPlace ficava preso em false.
        // Se o novo nascer sobre algo bloqueado, o OnTriggerStay volta a pôr false.
        canPlace = true;

        pendingObject = Instantiate(objeto, pos, transform.rotation);
        pendingObject.name = objeto.name;

        // ANIMACAO - Guarda o tamanho original do prefab
        Vector3 escalaOriginal = pendingObject.transform.localScale;

        // LEANTWEEN: O objeto nasce com escala 0 (invisível)
        pendingObject.transform.localScale = Vector3.zero;

        // LEANTWEEN: Ele cresce até a escala ORIGINAL
        LeanTween.scale(pendingObject, escalaOriginal, 0.4f).setEaseOutBack();

        selectionManager.Select(pendingObject);
        //pendingObject.AddComponent<Outline>(); //não precisa mais adicionar o outline pq ele é adicionado no Select()
        Outline outline = pendingObject.GetComponent<Outline>();
        outline.OutlineColor = Color.green;
        outline.OutlineWidth = 5f;

        //materialPlacement[2] = pendingObject.GetComponent<MeshRenderer>().material; //coloca o material original do objeto como o usado pós posicionar - foi substituido pelo outline

        PainelAddObjetos();

        if (!iluminacaoManager.toggleNoiteDia.isOn) //se o toggleNoiteDia NÃO estiver on (!) então tá de noite
        {
            iluminacaoManager.AcendeOsPostes(); //acende os postes inclusive o recém adicionado (se tiver colocado um poste, se nao só reacende os velhos)
        }
    }

    public void ToggleGrid() //liga desliga a grid
    {
        if (gridToggle.isOn)
        {
            gridOn = true;
        }
        else
        {
            gridOn = false;
        }
    }

    float RoundToNearestGrid(float pos) //era usado quando tinha um botao de grid no app
    {
        // arredonda para o ponto da grid mais próximo, também em coordenadas negativas
        return Mathf.Round(pos / gridSize) * gridSize;
    }

    public void PainelAddObjetos() //liga e desliga as coisas do painel de adicionar objetos
    {
        // pega o canvas group de todos os elementos para poder animar a transparencia
        CanvasGroup cgTopo = interfaceTopoSistema.GetComponent<CanvasGroup>();
        CanvasGroup cgBotaoAdd = botaoAddObjetos.GetComponent<CanvasGroup>();
        CanvasGroup cgPainel = painelObjetos.GetComponent<CanvasGroup>();


        if (painelObjetos.activeInHierarchy == true)
        {
            // ===== FECHANDO O PAINEL =====
            LeanTween.cancel(interfaceTopoSistema);
            LeanTween.cancel(botaoAddObjetos);
            LeanTween.cancel(painelObjetos);

            LeanTween.alphaCanvas(cgPainel, 0f, 0.2f).setOnComplete(() =>
            {
                painelObjetos.SetActive(false); // Só desliga de vez no fim da animação
            });

            // A interface do topo (e o botão) ligam imediatamente, mas invisíveis...
            interfaceTopoSistema.SetActive(true);
            botaoAddObjetos.SetActive(true); // (O Update vai mantê-lo ligado porque o topo agora está on)

            cgTopo.alpha = 0f;
            cgBotaoAdd.alpha = 0f;

            // ...e fazem o Fade In suave!
            LeanTween.alphaCanvas(cgTopo, 1f, 0.3f).setEaseOutQuad();
            LeanTween.alphaCanvas(cgBotaoAdd, 1f, 0.3f).setEaseOutQuad();
        }
        else if (painelObjetos.activeInHierarchy == false)
        {
            PostHog.Capture("menu_items_opened");
            // ===== ABRINDO O PAINEL =====
            LeanTween.cancel(interfaceTopoSistema);
            LeanTween.cancel(botaoAddObjetos);
            LeanTween.cancel(painelObjetos);

            if (selectionManager.selectedObject != null)
            {
                selectionManager.Deselect();
            }

            // A interface do topo e o botão fazem Fade Out juntos
            LeanTween.alphaCanvas(cgBotaoAdd, 0f, 0.2f);
            LeanTween.alphaCanvas(cgTopo, 0f, 0.2f).setOnComplete(() =>
            {
                interfaceTopoSistema.SetActive(false);
                // Assim que o topo desliga, o Update desliga o botão "+" automaticamente
            });

            // O painel da loja liga transparente...
            painelObjetos.SetActive(true);
            cgPainel.alpha = 0f;

            // ...faz o Fade In...
            LeanTween.alphaCanvas(cgPainel, 1f, 0.2f).setEaseOutQuad();
        }
    }

    //gerar o JSON com os dados todos da cena construída
    public string GerarJsonDaPraca()
    {
        // Prepara o pacote final com todas as informações
        JsonPayloadData payload = new JsonPayloadData();

        // ID anónimo do dispositivo (lê ou cria na primeira vez)
        idJogador = IdJogador.Obter();

        // Se a praça ainda não tem ID (é uma praça nova), gera um novo UUID
        if (string.IsNullOrEmpty(idDaPracaAtual))
        {
            idDaPracaAtual = System.Guid.NewGuid().ToString();
        }

        payload.pracaId = idDaPracaAtual;
        payload.pracaPaiId = idDaPracaPai;
        payload.nomeDoJogador = idJogador;
        payload.dataCriacao = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        //antes: payload.nomeDaCena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; // Pega o nome exato da cena atual em que o utilizador está a jogar
        payload.mapaId = pracaAtual != null ? pracaAtual.id : "";
        payload.nomeDaCena = pracaAtual != null ? pracaAtual.nomeExibicao : ""; // mantido pro site

        payload.tituloDaPraca = tituloDaPraca;
        payload.comentarioDaPraca = comentarioDaPraca;
        payload.layoutDaPraca = objetosPosicionados;

        // Transforma a classe em uma string JSON formatada
        string jsonPronto = JsonUtility.ToJson(payload, true);

        Debug.Log("JSON Gerado: \n" + jsonPronto);
        return jsonPronto;
    }

    // Chamado pelo CameraCapture antes de guardar localmente ou exportar.
    // Campos apenas com espaços são tratados como opcionais vazios.
    public void DefinirMetadadosDaPraca(string titulo, string comentario)
    {
        tituloDaPraca = NormalizarTextoOpcional(titulo, 50);
        comentarioDaPraca = NormalizarTextoOpcional(comentario, 500);
    }

    private static string NormalizarTextoOpcional(string texto, int limite)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "";

        string textoLimpo = texto.Trim();
        return textoLimpo.Length > limite ? textoLimpo.Substring(0, limite) : textoLimpo;
    }

    // "Salvar como cópia" só faz sentido se esta praça já tem um arquivo no
    // dispositivo (foi aberta do Arquivo ou já foi salva nesta sessão).
    // Praça nova ou remix ainda não salvo = não há original para manter.
    public bool PodeSalvarComoCopia()
    {
        if (string.IsNullOrEmpty(idDaPracaAtual))
            return false;

        return File.Exists(Path.Combine(Application.persistentDataPath, idDaPracaAtual + ".json"));
    }

    // Passa a trabalhar numa cópia: id novo, e a original fica como "pai"
    // (mesma lógica do remix). O arquivo original não é tocado.
    // Tem de ser chamado ANTES de SalvarPracaLocalmente e do upload.
    public void TransformarEmCopia()
    {
        idDaPracaPai = idDaPracaAtual;
        idDaPracaAtual = System.Guid.NewGuid().ToString();
    }


    //Salvar a praça no dispositivo, localmente
    public void SalvarPracaLocalmente()
    {
        // obtem o JSON da cena atual
        string jsonPronto = GerarJsonDaPraca();

        // O nome do ficheiro É o ID da praça. Se o ficheiro já existir, o WriteAllText substitui-o
        string caminhoFicheiro = Path.Combine(Application.persistentDataPath, idDaPracaAtual + ".json");
        System.IO.File.WriteAllText(caminhoFicheiro, jsonPronto);

        // Captura a textura e salva um jpg com o mesmo nome do JSON
        if (rtVistaTopo != null)
        {
            string caminhoImagem = Path.Combine(Application.persistentDataPath, idDaPracaAtual + ".jpg");

            // 1. Criamos uma RenderTexture temporária pequenina (256x256 é super leve e perfeito para UI)
            RenderTexture rtMiniatura = RenderTexture.GetTemporary(256, 256, 0);

            // 2. O truque mágico: Copia a vista gigante para a pequenina. A Unity encolhe a imagem inteira automaticamente!
            Graphics.Blit(rtVistaTopo, rtMiniatura);

            // 3. Agora lemos os pixels dessa nova versão pequenina e convertemos para Texture2D
            Texture2D tex = toTexture2D(rtMiniatura, 256, 256);
            byte[] bytes = tex.EncodeToJPG();

            // 4. Guardamos no telemóvel
            System.IO.File.WriteAllBytes(caminhoImagem, bytes);

            // 5. Limpeza de memória importantíssima
            Destroy(tex);
            RenderTexture.ReleaseTemporary(rtMiniatura); // Apaga a memória temporária que criámos
        }

        // Escreve o ficheiro no telemóvel
        Debug.Log("Praça salva com sucesso em: " + caminhoFicheiro);

        //posthog: guardou a praça no dispositivo
        PostHog.Capture("square_saved_locally", new Dictionary<string, object>
        {
            { "mapa_id", pracaAtual != null ? pracaAtual.id : "" },
            { "total_objects", objetosPosicionados.Count }
        });
    }

    // função copiada do CameraCapture
    Texture2D toTexture2D(RenderTexture rTex, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }


    // Carrega o terreno certo (cena aditiva) e só depois reconstrói os objetos
    // salvos — precisa esperar o terreno pra ter colliders prontos pro raycast
    // de posicionamento.
    private IEnumerator IniciarPraca(string mapaId, string jsonParaCarregar, bool isRemix)
    {
        // Rede de segurança: se por algum motivo não achamos o id, usa a primeira
        // praça do catálogo em vez de deixar a cena vazia sem chão.
        if (string.IsNullOrEmpty(mapaId) && catalogo.pracas.Count > 0)
            mapaId = catalogo.pracas[0].id;

        pracaAtual = catalogo.ObterPorId(mapaId);
        if (pracaAtual == null)
        {
            Debug.LogError($"[BuildingManager] Praça com id '{mapaId}' não encontrada no catálogo.");
            yield break;
        }

        AsyncOperation carregamento = SceneManager.LoadSceneAsync(pracaAtual.cenaAditiva, LoadSceneMode.Additive);
        yield return carregamento;

        if (cameraPanZoom != null)
            cameraPanZoom.AplicarConfiguracaoDaPraca(pracaAtual);

        if (!string.IsNullOrEmpty(jsonParaCarregar))
            CarregarPraca(jsonParaCarregar, isRemix);

        //posthog: praça aberta no Jogo e de onde veio (praça nova do menu, arquivo local ou remix de link)
        string origem = isRemix ? "remix" : (!string.IsNullOrEmpty(jsonParaCarregar) ? "arquivo" : "nova");
        PostHog.Capture("square_loaded", new Dictionary<string, object>
        {
            { "mapa_id", pracaAtual.id },
            { "origem", origem },
            { "total_objects", objetosPosicionados.Count }
        });
    }

    // Nome do objeto (ex: "Banco") pro PostHog. Objetos já posicionados têm o ID
    // no nome do GameObject, então procura o nome real na lista.
    public string NomeDoObjeto(GameObject objeto)
    {
        if (objeto == null) return "";
        ObjetoPosicionadoData dados = objetosPosicionados.Find(item => item.id == objeto.name);
        return dados != null ? dados.nome : objeto.name;
    }

    // Tenta descobrir o id do mapa a partir de um JSON salvo. Cobre o caso de
    // arquivos antigos, salvos antes desta migração, que não têm "mapaId".
    private string ResolverMapaId(JsonPayloadData dados)
    {
        if (!string.IsNullOrEmpty(dados.mapaId))
            return dados.mapaId;

        // Fallback pra saves antigos: tenta casar pelo nome de exibição salvo.
        // Se não achar, cai no catch-all do IniciarPraca (primeira praça do catálogo).
        PracaData porNome = catalogo.ObterPorIdentificador(dados.nomeDaCena);
        return porNome != null ? porNome.id : null;
    }

    // Reconstrói a praça a partir de um JSON já carregado em memória (não lê mais
    // arquivo — isso já foi feito em Start()).
    public void CarregarPraca(string jsonParaCarregar, bool isRemixDaGaleria)
    {
        JsonPayloadData pracaSalva = JsonUtility.FromJson<JsonPayloadData>(jsonParaCarregar);

        // Link da galeria para uma praça feita NESTE dispositivo: continua a mesma
        // praça (mesmo praca_id), para a próxima exportação entrar como versão nova
        // no Histórico e não como remix de si própria. Para ramificar, há "Guardar cópia".
        bool ehPracaDesteDispositivo = isRemixDaGaleria
            && !string.IsNullOrEmpty(pracaSalva.pracaId)
            && pracaSalva.nomeDoJogador == IdJogador.Obter();

        if (isRemixDaGaleria && !ehPracaDesteDispositivo)
        {
            idDaPracaPai = pracaSalva.pracaId;
            idDaPracaAtual = System.Guid.NewGuid().ToString();
        }
        else
        {
            idDaPracaAtual = pracaSalva.pracaId;
            idDaPracaPai = pracaSalva.pracaPaiId;
        }

        DefinirMetadadosDaPraca(pracaSalva.tituloDaPraca, pracaSalva.comentarioDaPraca);

        foreach (var objData in objetosPosicionados)
        {
            GameObject objNaCena = GameObject.Find(objData.id);
            if (objNaCena != null) Destroy(objNaCena);
        }
        objetosPosicionados.Clear();

        BotaoObjManager lojaManager = FindFirstObjectByType<BotaoObjManager>(FindObjectsInactive.Include);

        foreach (ObjetoPosicionadoData item in pracaSalva.layoutDaPraca)
        {
            ObjetosData dadosOriginais = null;
            foreach (ObjetosData objData in lojaManager.listaTodosDados)
            {
                if (objData != null && objData.prefab != null && objData.prefab.name == item.nome)
                {
                    dadosOriginais = objData;
                    break;
                }
            }

            if (dadosOriginais != null)
            {
                GameObject novoObj = Instantiate(dadosOriginais.prefab, item.posicao, Quaternion.Euler(item.rotacao));
                novoObj.name = item.id;
                objetosPosicionados.Add(item);
            }
            else
            {
                Debug.LogWarning("Não foi possível encontrar o modelo 3D para: " + item.nome);
            }
        }

        Debug.Log("Praça carregada com sucesso!");
    }


}


//nova lista de itens posicionados na fase pelo player
[System.Serializable]
public class ObjetoPosicionadoData
{
    public string id; // Útil para quando for deletar um objeto
    public string nome;
    public string categoria;
    public Vector3 posicao; //coordenadas x e y do objeto na cena
    public Vector3 rotacao; //rotacao do objeto na cena
}

[System.Serializable] //transforma em um json pro upload
public class JsonPayloadData
{
    public string pracaId; // ID Único DESTA criação do jogador
    public string pracaPaiId; // ID da praça original (vazio se for uma criação do zero)
    public string nomeDoJogador; //nao é um nome nome mas um id unico por dispositivo
    public string mapaId;        // NOVO: id estável da PracaData usada (ex: "barao-de-corumba")
    public string nomeDaCena; //nome da scene no unity pra ter o nome bonito pra chamar
    public string dataCriacao;
    public string tituloDaPraca;
    public string comentarioDaPraca;
    public List<ObjetoPosicionadoData> layoutDaPraca;
}
