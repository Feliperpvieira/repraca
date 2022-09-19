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

    // Start is called before the first frame update
    void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
    }

    // Update is called once per frame
    void Update()
    {
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

        if (buildingManager.pendingObject == null) //ativa os botões pra mover e deselecionar quando o objeto não estiver sendo movido
        {
            //botaoMoverObj.interactable = true;
            deselectObj.interactable = true;
            editObjSelect.SetActive(false); //desativa os botoes de girar e posicionar
        }
        else if (buildingManager.pendingObject != null)
        {
            //botaoMoverObj.interactable = false;
            deselectObj.interactable = false;
            editObjSelect.SetActive(true); //reativa os botoes de girar e posicionar
        }
    }

    public void Select(GameObject obj)
    {
        if (obj == selectedObject) return; //se o objeto selecionado for o que já ta selecionado ele retorna
        if (selectedObject != null) Deselect(); //se clicar em outro objeto ele vai desselecionar o que você tem atualmente

        Outline outline = obj.GetComponent<Outline>(); //referencia ao outline https://assetstore.unity.com/packages/tools/particles-effects/quick-outline-115488

        if (outline == null)
        {
            obj.AddComponent<Outline>(); //se não tiver outline, adiciona um
        }
        else
        {
            outline.enabled = true; //se já tiver outline, muda enabled para true
            outline.OutlineColor = Color.white;
        }

        objNameText.text = obj.name; //coloca o nome do objeto selecionado no campo de texto do nome da interface

       if(buildingManager.pendingObject == null)
        {
            objSelectUI.SetActive(true); //faz o pop-up de seleção de objeto aparecer
        }

        selectedObject = obj; //iguala a variavel de objeto selecionado ao obj da função select        
    }

    public void Deselect()
    {
        objSelectUI.SetActive(false); //faz o pop-up de seleção de objeto sumir
        selectedObject.GetComponent<Outline>().enabled = false; //desativa o outline

        selectedObject = null;
    }

    public void Move()
    {
        buildingManager.pendingObject = selectedObject;
        objSelectUI.SetActive(false);
    }

    public void Delete()
    {
        GameObject objToDestroy = selectedObject;
        buildingManager.objetosPosicionados.Remove(objToDestroy.name);
        Deselect(); //desseleciona o objeto antes de deletar
        Destroy(objToDestroy);
    }
}
