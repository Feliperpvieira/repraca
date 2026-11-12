using UnityEngine;

// ID anónimo e aleatório do dispositivo, guardado em PlayerPrefs.
// É usado como player_name no Supabase (BuildingManager) e como distinct_id
// no PostHog (AnalyticsManager). Fica num lugar só para os dois lerem
// sempre o mesmo valor.
public static class IdJogador
{
    private const string CHAVE = "PlayerUUID";

    // Devolve o ID salvo; na primeira vez, cria um novo e guarda.
    public static string Obter()
    {
        string id = PlayerPrefs.GetString(CHAVE, "");

        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(CHAVE, id);
            PlayerPrefs.Save();
        }

        return id;
    }
}
