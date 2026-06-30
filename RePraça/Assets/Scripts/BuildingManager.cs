using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    private Vector3 pos; //posição do obj
    private RaycastHit hit;

    [SerializeField] private LayerMask layerMask;

    [Header("stuff")]
    public float rotateAmount;

    public float gridSize;
    bool gridOn;
    public bool canPlace = true;
    [SerializeField] private Toggle gridToggle;

    private SelectionManager selectionManager;
    private DiaNoite iluminacaoManager;

    [Header("o jogo gere")]
    //public List<string> objetosPosicionados = new List<string>(); //forma antiga de guardar o que estava adicionado na cena
    public List<ObjetoPosicionadoData> objetosPosicionados = new List<ObjetoPosicionadoData>();
    public ObjetosData dadosDoObjetoPendente; // Nova variável para guardar os dados da UI
    //public GameObject[] objects; //lista de objetos - todos os objetos ficavam aqui e eles eram construídos pelo seu index
    public GameObject pendingObject; //objeto selecionado


    void Start()
    {
        //coloca o objeto SelectManager da scene na variavel do codigo
        selectionManager = GameObject.Find("SelectManager").GetComponent<SelectionManager>();
        iluminacaoManager = GameObject.Find("IluminacaoManager").GetComponent<DiaNoite>(); //pega o script DiaNoite dentro do gameobject iluminacao manager
        
        escalaOriginalPainelObjetos = painelObjetosConteudoAnimado.transform.localScale; //guarda o tamanho do painel de objetos definido no editor
    }

    void Update()
    {
        if (Input.touchCount > 1) return; // Se o usuário estiver fazendo zoom com 2 dedos, não mexe o objeto

        // NOVO: Verifica se estamos a clicar na tela real ou num botão da UI
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

        // ORIGINAL MELHORADO: Só atira o raycast e move o 'pos' se o utilizador não estiver a clicar na UI!
        if (isInputAtivo && !isPointerOverUI)
        {
            Ray ray = Camera.main.ScreenPointToRay(cursorPosition);
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
                if (!EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId) && Input.GetTouch(0).phase != TouchPhase.Ended) //checa se o toque esta batendo em um botao
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
        else if(pendingObject == null)
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

            if (objetoExistente != null)
            {
                // SE ELE JÁ EXISTE (está sendo movido), apenas atualiza a posição e rotação
                objetoExistente.posicao = pendingObject.transform.position;
                objetoExistente.rotacao = pendingObject.transform.eulerAngles;
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

                // Salva o ID no nome do GameObject para facilitar na hora de deletar e mover
                pendingObject.name = novoObjeto.id;

                // Adiciona na lista
                objetosPosicionados.Add(novoObjeto);
            }

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
    }

    private void FixedUpdate()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); //posição que vai colocar o objeto sendo segurado

        if(Physics.Raycast(ray, out hit, 1000, layerMask)) //esse 1000 é a distância que ele vai, pode trocar por uma variavel se quiser //layermask vai ser pra impedir que construa coisa sobre coisa
        {
            pos = hit.point; //o point pega o impact point no worldspace, basicamente diz pro jogo onde colocar o objeto
        }
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
        Ray raioCentro = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hitCentro;

        if (Physics.Raycast(raioCentro, out hitCentro, 1000f, layerMask))
        {
            pos = hitCentro.point; // Define a posição de spawn para o meio da tela
        }

     
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
        float xDiff = pos % gridSize; //calcula o resto da posição pelo grid size

        //aí subtrai ou soma a posição pela diferença pra colocar a posição no grid mais próximo
        pos -= xDiff; 

        if(xDiff > (gridSize / 2))
        {
            pos += gridSize;
        }
        return pos;
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
        payload.nomeDoJogador = "Visitante"; // opcional? 
        payload.dataCriacao = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        payload.layoutDaPraca = objetosPosicionados;

        // Transforma a classe em uma string JSON formatada
        string jsonPronto = JsonUtility.ToJson(payload, true);

        Debug.Log("JSON Gerado: \n" + jsonPronto);
        return jsonPronto;
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
    public string nomeDoJogador;
    public string dataCriacao;
    public List<ObjetoPosicionadoData> layoutDaPraca;
}

