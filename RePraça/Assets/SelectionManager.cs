using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectionManager : MonoBehaviour
{

    public GameObject selectedObject; //guarda o objeto que foi selecionado
    public TextMeshProUGUI objNameText;
    private BuildingManager buildingManager;

    public GameObject objSelectUI; //pop-up que aparece ao selecionar objeto

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
                    Select(hit.collider.gameObject); //seleciona o objeto
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && selectedObject != null)
        {
            Deselect();
        }
    }

    private void Select(GameObject obj)
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
        }

        objNameText.text = obj.name; //coloca o nome do objeto selecionado no campo de texto do nome da interface
        objSelectUI.SetActive(true); //faz o pop-up de seleção de objeto aparecer

        selectedObject = obj; //iguala a variavel de objeto selecionado ao obj da função select
    }

    private void Deselect()
    {
        objSelectUI.SetActive(false); //faz o pop-up de seleção de objeto sumir
        selectedObject.GetComponent<Outline>().enabled = false; //desativa o outline

        selectedObject = null;
    }

    public void Move()
    {
        buildingManager.pendingObject = selectedObject;
    }

    public void Delete()
    {
        GameObject objToDestroy = selectedObject;
        Deselect(); //desseleciona o objeto antes de deletar
        Destroy(objToDestroy);
    }
}
