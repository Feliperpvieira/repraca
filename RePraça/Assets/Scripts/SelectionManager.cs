using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{

    public GameObject selectedObject; //guarda o objeto que foi selecionado
    public TextMeshProUGUI objNameText;
    private BuildingManager buildingManager;

    public GameObject objSelectUI; //pop-up que aparece ao selecionar objeto
    //public Button botaoMoverObj;
    public GameObject editObjSelect;
    public Button deselectObj;

    private bool isEditingMode = false; // Rastreador de estado para o menu de edição

    // Start is called before the first frame update
    void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Impede a seleção acidental de um objeto ao fazer Zoom/Pan.
        if (Input.touchCount > 1) return; // Se houver mais de 1 dedo na tela, ignora todo o resto

        //vê onde o clique ocorreu e identifica se ele bateu em algum objeto
        if (Input.GetMouseButtonDown(0)) //se clicar com o mouse
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); //ele projeta um raycast a partir do mouse
            RaycastHit hit; //se acertar algum objeto
            if(Physics.Raycast(ray, out hit, 1000))
            {
                if (hit.collider.gameObject.CompareTag("Objetos")) //e o objeto tiver a tag objetos
                {
                    if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer) //se for uma plataforma com touch
                    {
                        if (!EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) //se nao estiver touch em algo da UI sobre o objeto
                        {
                            Select(hit.collider.gameObject); //seleciona o objeto
                        }
                    }
                    else if (!EventSystem.current.IsPointerOverGameObject()) //plataformas de mouse, se nao estiver com o mouse sobre a UI
                    {
                        Select(hit.collider.gameObject); //seleciona o objeto
                    }
                    
                }
                else //caso contrário, se clicar em uma área que NAO seja um objeto
                {             
                    if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer) //se for uma plataforma com touch
                    {
                        if (!EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId) && Input.GetTouch(0).phase != TouchPhase.Ended) //se nao estiver touch em algo da UI sobre o objeto
                        {
                            if (selectedObject != null && buildingManager.pendingObject == null) //e se tiver um objeto selecionado e nao estiver movendo nenhum objeto
                            {
                                Deselect(); //deseleciona o objeto selecionado
                            }
                        }
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && selectedObject != null)
        {
            Deselect();
        }

        bool currentlyEditing = (buildingManager.pendingObject != null);

        // Só roda este bloco no exato momento em que o modo muda de Editar para Não-Editar (ou vice-versa)
        if (currentlyEditing != isEditingMode)
        {
            isEditingMode = currentlyEditing; // Atualiza o rastreador

            if (isEditingMode)
            {
                // SITUAÇÃO 1: Entrou no modo edição (Clicou em Mover ou Adicionou novo da loja)
                // O painel das ferramentas (Girar, Lixeira, Colocar) "salta" para a tela!
                deselectObj.interactable = false;
                editObjSelect.SetActive(true);
                editObjSelect.transform.localScale = Vector3.zero;
                LeanTween.cancel(editObjSelect);


                // Verifica se a tela do nome está na hierarquia (visível)
                if (objSelectUI.activeInHierarchy)
                {
                    // Se estiver a sair da seleção de um objeto (vindo do botão Mover), espera 0.15s para a ui de seleção sumir
                    LeanTween.scale(editObjSelect, Vector3.one, 0.3f).setEaseOutBack().setDelay(0.1f);
                }
                else
                {
                    // Se não houver nada na tela (vindo da Loja de itens), a ui aparece imediato
                    LeanTween.scale(editObjSelect, Vector3.one, 0.3f).setEaseOutBack();
                }

            }
            else
            {
                // SITUAÇÃO 2: Saiu do modo edição (Posicionou o objeto ou cancelou)
                // O painel das ferramentas encolhe e desaparece suavemente.
                deselectObj.interactable = true;
                LeanTween.cancel(editObjSelect);
                LeanTween.scale(editObjSelect, Vector3.zero, 0.15f)
                    .setEaseInQuad()
                    .setOnComplete(() =>
                    {
                        editObjSelect.SetActive(false);
                    });
            }
        }
    }

    public void Select(GameObject obj)
    {
        if (obj == selectedObject) return; //se o objeto selecionado for o que já ta selecionado ele retorna (ignora)
        // 1. DESATIVA O OUTLINE DO OBJETO ANTERIOR (mini deselect)
        if (selectedObject != null)
        {
            selectedObject.GetComponent<Outline>().enabled = false; //referencia ao outline https://assetstore.unity.com/packages/tools/particles-effects/quick-outline-115488
        }

        // 2. ATIVA O OUTLINE DO NOVO OBJETO
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null)
        {
            obj.AddComponent<Outline>();
        }
        else
        {
            outline.enabled = true;
            outline.OutlineColor = Color.white;
        }

        // 3. ATUALIZA O NOME NO PAINEL
        // NOVA LÓGICA DE NOME: Procura o objeto na lista do BuildingManager usando o nome (que agora é o ID)
        ObjetoPosicionadoData dadosSalvos = buildingManager.objetosPosicionados.Find(item => item.id == obj.name);
        if (dadosSalvos != null) //por algum motivo sem isso da bug
        {
            objNameText.text = dadosSalvos.nome;
        }


        // 4. ANIMAÇÃO DA UI
        if (buildingManager.pendingObject == null)
        {
            if (!objSelectUI.activeInHierarchy)
            {
                // SITUAÇÃO A (Estava fechada): O menu "brota" do zero
                objSelectUI.SetActive(true);
                objSelectUI.transform.localScale = Vector3.zero;
                LeanTween.scale(objSelectUI, Vector3.one, 0.3f).setEaseOutBack();
            }
            else
            {
                // SITUAÇÃO B (Já estava aberta e mudou de objeto): 
                // Cancela qualquer animação perdida, garante que o tamanho está correto e dá uma "Pulsada"
                LeanTween.cancel(objSelectUI);
                objSelectUI.transform.localScale = Vector3.one;
                LeanTween.scale(objSelectUI, new Vector3(1.1f, 1.1f, 1.1f), 0.1f).setLoopPingPong(1);
            }
        }
        else
        {
            // SITUAÇÃO C (A CORREÇÃO!): Estamos a adicionar um objeto novo da loja.
            // O menu do objeto anterior que estava selecionado tem de encolher e desaparecer!
            if (objSelectUI.activeInHierarchy)
            {
                LeanTween.cancel(objSelectUI);
                LeanTween.scale(objSelectUI, Vector3.zero, 0.15f)
                    .setEaseInQuad()
                    .setOnComplete(() =>
                    {
                        objSelectUI.SetActive(false);
                    });
            }
        }

        selectedObject = obj; //iguala a variavel de objeto selecionado ao obj da função select        
    }

    public void Deselect()
    {
        if (selectedObject != null)
        {
            selectedObject.GetComponent<Outline>().enabled = false; //desativa o outline
        }

        // LEANTWEEN: Animação de fechar o menu
        if (objSelectUI.activeInHierarchy)
        {
            LeanTween.cancel(objSelectUI); // Cancela caso o jogador clique muito rápido
            LeanTween.scale(objSelectUI, Vector3.zero, 0.15f)
                .setEaseInQuad()
                .setOnComplete(() =>
                {
                    objSelectUI.SetActive(false);
                });
        }

        selectedObject = null;
    }

    public void Move()
    {
        buildingManager.pendingObject = selectedObject;

        // LEANTWEEN: Animação de fechar o menu ao mover
        if (objSelectUI.activeInHierarchy)
        {
            LeanTween.cancel(objSelectUI);
            LeanTween.scale(objSelectUI, Vector3.zero, 0.15f)
                .setEaseInQuad()
                .setOnComplete(() =>
                {
                    objSelectUI.SetActive(false);
                });
        }
    }

    public void Delete()
    {
        GameObject objToDestroy = selectedObject;

        // Procura na lista o objeto que tem o mesmo ID (salvo no nome do GameObject) e remove
        buildingManager.objetosPosicionados.RemoveAll(item => item.id == objToDestroy.name);

        //buildingManager.objetosPosicionados.Remove(objToDestroy.name); //substituido pelo novo
        Deselect(); //desseleciona o objeto antes de deletar

        // O objeto encolhe até 0 em 0.2 segundos, dando um pequeno "solavanco" para dentro (EaseInBack)
        // E só quando a animação termina (setOnComplete) é que o Unity destrói o objeto da memória.
        LeanTween.scale(objToDestroy, Vector3.zero, 0.2f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                Destroy(objToDestroy);
            });
    }

    // Função para ser chamada quando o utilizador clica na tag com o nome do objeto
    public void AbrirNaLoja()
    {
        if (selectedObject == null) return;

        // 1. Encontra os dados do objeto posicionado para sabermos qual é o prefab original
        ObjetoPosicionadoData dadosSalvos = buildingManager.objetosPosicionados.Find(item => item.id == selectedObject.name);

        if (dadosSalvos != null)
        {
            // Guarda o nome do prefab antes que a seleção seja apagada
            string nomeDoPrefab = dadosSalvos.nome;

            // 2. Abre a interface da loja (graças ao que fizemos antes, isto fecha o menu de edição automaticamente!)
            if (buildingManager.painelObjetos.activeInHierarchy == false)
            {
                buildingManager.PainelAddObjetos();
            }

            // 3. Procura o BotaoObjManager (usamos 'true' para ele o encontrar mesmo que o painel estivesse escondido)
            BotaoObjManager lojaManager = FindObjectOfType<BotaoObjManager>(true);
            if (lojaManager != null)
            {
                // 4. Manda a loja selecionar automaticamente o botão desse objeto
                lojaManager.AbrirDetalhesDoObjeto(nomeDoPrefab);
            }
        }
    }

}
