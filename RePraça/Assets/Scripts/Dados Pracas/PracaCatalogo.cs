using System.Collections.Generic;
using UnityEngine;

// Lista mestra de todas as praças disponíveis no jogo. Preencher no Inspector
// arrastando cada asset PracaData criado (mesmo padrão do listaTodosDados no
// BotaoObjManager, mas pra praças em vez de mobiliário).
[CreateAssetMenu(menuName = "rePraça/Catálogo de Praças")]
public class PracaCatalogo : ScriptableObject
{
    public List<PracaData> pracas = new List<PracaData>();

    // Procura uma praça pelo id salvo (json local, remix, ou seleção no Menu).
    // Devolve null se não encontrar — quem chamar precisa tratar esse caso.
    public PracaData ObterPorId(string id)
    {
        return pracas.Find(p => p != null && p.id == id);
    }
}