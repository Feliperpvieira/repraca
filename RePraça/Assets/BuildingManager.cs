using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingManager : MonoBehaviour
{

    public GameObject[] objects; //lista de objetos
    private GameObject pendingObject; //objeto selecionado

    private Vector3 pos; //posição do obj
    private RaycastHit hit;

    [SerializeField] private LayerMask layerMask;

    public float rotateAmount;

    public float gridSize;
    bool gridOn = true;
    [SerializeField] private Toggle gridToggle;


    void Update()
    {
        if(pendingObject != null) //checa se existe um objeto selecionado
        {
            if (gridOn) //se a grid estiver ligada
            {
                //pega a posição de cada coord do mouse e arredonda elas
                pendingObject.transform.position = new Vector3(
                    RoundToNearestGrid(pos.x),
                    RoundToNearestGrid(pos.y),
                    RoundToNearestGrid(pos.z)
                    );
            }
            else //se a grid estiver desligada move o objeto livremente 
            {
                pendingObject.transform.position = pos; //movimenta o objeto
            }

            if (Input.GetMouseButtonDown(0)) //ao clicar
            {
                PlaceObject(); //coloca
            }

            if (Input.GetKeyDown(KeyCode.R)) //se apertar a tecla R ele gira o objeto
            {
                RotateObject();
            }
        }
    }

    public void PlaceObject()
    {
        pendingObject = null; //o objeto que estava selecionado não tá selecionado mais
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

    public void SelectObject(int index) //seleciona o objeto pelo index dele no array objects
    {
        pendingObject = Instantiate(objects[index], pos, transform.rotation);
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

    float RoundToNearestGrid(float pos)
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
}
