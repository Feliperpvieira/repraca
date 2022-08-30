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


    void Update()
    {
        if(pendingObject != null) //checa se existe um objeto selecionado
        {
            pendingObject.transform.position = pos; //movimenta o objeto

            if (Input.GetMouseButtonDown(0)) //ao clicar
            {
                PlaceObject(); //coloca
            }
        }
    }

    public void PlaceObject()
    {
        pendingObject = null; //o objeto que estava selecionado não tá selecionado mais
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
}
