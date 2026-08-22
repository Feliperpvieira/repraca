using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleRGPD : MonoBehaviour
{
    [Header("UI da Configuração")]
    public Toggle toggleAnalytics;

    void Start()
    {
        if (AnalyticsManager.Instance == null)
        {
            Debug.LogWarning("AnalyticsManager ainda não foi inicializado.");
            return;
        }

        // Mostra o estado atual sem disparar o evento (senão chamava
        // DefinirConsentimento outra vez, sem necessidade)
        toggleAnalytics.SetIsOnWithoutNotify(AnalyticsManager.Instance.EstaConsentido());

        // Cada vez que o utilizador mexe no Toggle, o próprio onValueChanged
        // já entrega o bool certinho pro parâmetro de DefinirConsentimento
        toggleAnalytics.onValueChanged.AddListener(AnalyticsManager.Instance.DefinirConsentimento);
    }
}
