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

    private void Awake()
    {
        esseBotao = gameObject.GetComponent<Button>();
    }

    private void Start()
    {
        // Os botões criados em runtime configuram esta referência pelo método
        // Configurar. Os botões antigos continuam a funcionar pelo Inspector.
        if (objetoFiltro != null)
            scriptFiltro = objetoFiltro.GetComponent<FiltroManager>();
    }

    // Permite que FiltroCategoriasObjetos reutilize o mesmo prefab visual sem
    // depender de referências que só existiam nos botões montados manualmente.
    public void Configurar(FiltroManager novoFiltro)
    {
        scriptFiltro = novoFiltro;
        objetoFiltro = novoFiltro != null ? novoFiltro.gameObject : null;
    }

    public void botaoClicado()
    {
        if (scriptFiltro == null && objetoFiltro != null)
            scriptFiltro = objetoFiltro.GetComponent<FiltroManager>();

        if (scriptFiltro != null)
            scriptFiltro.administraFiltro(esseBotao);
        else
            Debug.LogWarning("[botaoFiltroSelec] Este botão não tem um FiltroManager configurado.");
    }
}
