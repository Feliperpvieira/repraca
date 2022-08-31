using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPlacement : MonoBehaviour
{

    BuildingManager buildingManager; //conecta o building manager a esse codigo

    void Start()
    {
        //ele vai pegar o object BuildingManager na scene do unity pra conectar com esse codigo
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
    }

    private void OnTriggerStay(Collider other)
    {
        //tag cantPlace é pra nao pode colocar em geral: caminhos, arvores, etc; objetos é pra objetos posicionados. objetos é selecionavel, cantPlace nao
        //tag que impede de se construir em cima, quando entrar (ou se manter) em um collider com o trigger o canPlace vira false
        if (other.gameObject.CompareTag("CantPlace") || other.gameObject.CompareTag("Objetos"))
        {
            buildingManager.canPlace = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //mesma tag de cima, mas agora diz que quando o trigger sair (exit) ele pode construir de novo
        if (other.gameObject.CompareTag("CantPlace") || other.gameObject.CompareTag("Objetos")) 
        {
            buildingManager.canPlace = true;
        }
    }
}
