using UnityEngine;

// Dados fixos de uma praça específica (mapa real). Criar um asset deste tipo
// por praça: botão direito no Project > Create > rePraça > Dados da Praça.
//
// O "id" é o identificador ESTÁVEL da praça — nunca deve ser alterado depois
// de a praça já ter sido publicada, pois é ele que liga saves locais, links
// de remix e registos no Supabase à praça certa. "nomeExibicao" e o nome da
// cena aditiva podem mudar à vontade sem quebrar nada, desde que o "id" fique igual.
[CreateAssetMenu(menuName = "rePraça/Dados da Praça")]
public class PracaData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("Slug único e estável (ex: 'barao-de-corumba'). NUNCA mude depois de publicar.")]
    public string id;

    [Tooltip("Nome mostrado na UI. Pode ser alterado livremente, sem afetar saves antigos.")]
    public string nomeExibicao;

    [Header("Conteúdo informativo (tela de Informações)")]
    public string bairro;
    [TextArea] public string descricao;
    public Sprite fotoReal;

    [Header("Cena do terreno (carregada de forma aditiva)")]
    [Tooltip("Nome exato da cena que contém só o terreno/prédios/lightmap desta praça.")]
    public string cenaAditiva;

    [Header("Câmera")]
    // Qualificado com "UnityEngine." explicitamente porque o projeto também
    // referencia o pacote Supabase, que traz System.Numerics.Vector3/Vector2 —
    // sem isso o compilador fica em dúvida entre os dois tipos (CS0104).
    public UnityEngine.Vector3 posicaoInicialCamera;
    public UnityEngine.Vector2 boundsX = new UnityEngine.Vector2(-10f, 11f); // x = min, y = max
    public UnityEngine.Vector2 boundsZ = new UnityEngine.Vector2(-23f, 9f);  // x = min, y = max
    public float fovInicial = 60f;
}