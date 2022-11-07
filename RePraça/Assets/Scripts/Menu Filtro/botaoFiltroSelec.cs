using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class botaoFiltroSelec : MonoBehaviour
{
    private Button esseBotao;

    [Header("Filtro em que o botão está")]
    public GameObject objetoFiltro;

    private FiltroManager scriptFiltro;

    // Start is called before the first frame update
    void Start()
    {
        scriptFiltro = objetoFiltro.GetComponent<FiltroManager>(); //pega o script do filtro do objeto de filtro, definido no inspetor
        esseBotao = gameObject.GetComponent<Button>();
    }

    public void botaoClicado()
    {
        scriptFiltro.administraFiltro(esseBotao);
    }
}
