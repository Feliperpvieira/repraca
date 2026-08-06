using UnityEngine;
using PostHogUnity;

public class AnalyticsManager : MonoBehaviour
{
    async void Start()
    {
        // busca o ID anónimo que o jogo cria para cada dispositivo
        // Se for a primeira vez que joga, cria um novo
        string userIdAnomimo = PlayerPrefs.GetString("PlayerUUID", System.Guid.NewGuid().ToString());

        // Garante que o ID fica guardado
        PlayerPrefs.SetString("PlayerUUID", userIdAnomimo);
        PlayerPrefs.Save();

        // diz ao PostHog quem é este utilizador, mantendo anonimo
        await PostHog.IdentifyAsync(userIdAnomimo);

        Debug.Log("PostHog ativado para utilizador anónimo: " + userIdAnomimo);
    }
}