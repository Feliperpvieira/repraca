using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Pôr no mesmo GameObject de um TextMeshProUGUI que tenha tags <link="...">.
// Ao tocar no link: id "suporte" abre o e-mail pré-preenchido do DeepLinkManager;
// qualquer outro id é aberto como endereço (ex: <link="https://..."> curto).
// O texto precisa de "Raycast Target" ligado para receber o toque.
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextoLinkEmail : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI texto;

    private void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Canvas em Screen Space - Overlay usa câmara nula; nos outros modos usa a do evento
        Camera camara = texto.canvas != null && texto.canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        int indice = TMP_TextUtilities.FindIntersectingLink(texto, eventData.position, camara);
        if (indice == -1) return;

        string id = texto.textInfo.linkInfo[indice].GetLinkID();

        if (id == "suporte")
        {
            if (DeepLinkManager.Instance != null)
                Application.OpenURL(DeepLinkManager.Instance.CriarMailtoSuporte());
            return;
        }

        if (!string.IsNullOrEmpty(id))
            Application.OpenURL(id);
    }
}
