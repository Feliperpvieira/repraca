using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Anexe este componente no prefab "Btn Arquivo Item".
// Ele preenche a UI do card (Nome, Data, foto de topo) e guarda os dados
// completos da praça, para que o popup de detalhes possa usá-los depois
// sem precisar reler o ficheiro do disco.
public class BtnArquivoItem : MonoBehaviour
{
    [Header("Preenchido em runtime pelo ArquivoManager")]
    public JsonPayloadData dados;
    public string caminhoArquivo;
    public Texture2D miniatura;

    public TextMeshProUGUI txtNome;
    public TextMeshProUGUI txtData;
    public RawImage rawImagePraca;

    //private void Awake()
    //{
    //    // Nomes exatos dos filhos no prefab (vistos no seu Hierarchy) —
    //    // se algum dia renomear os objetos no prefab, ajuste aqui também.
    //    txtNome = transform.Find("Nome")?.GetComponent<TextMeshProUGUI>();
    //    txtData = transform.Find("Data")?.GetComponent<TextMeshProUGUI>();
    //    rawImagePraca = transform.Find("RawImage Praca")?.GetComponent<RawImage>();
    //}

    public void Preencher(JsonPayloadData dadosDaPraca, string caminho, Texture2D textura)
    {
        dados = dadosDaPraca;
        caminhoArquivo = caminho;
        miniatura = textura;

        if (txtNome != null)
            txtNome.text = (dados != null && !string.IsNullOrEmpty(dados.nomeDaCena))
                ? dados.nomeDaCena
                : "Praça Desconhecida";

        if (txtData != null)
            txtData.text = dados != null ? FormatarData(dados.dataCriacao) : "";

        if (rawImagePraca != null && textura != null)
            rawImagePraca.texture = textura;
    }

    public static string FormatarData(string dataOriginal)
    {
        // dataCriacao vem como "yyyy-MM-dd HH:mm" (formato do BuildingManager).
        // Aqui convertemos para dd/MM/yyyy, igual ao que já aparece nos seus popups.
        if (System.DateTime.TryParse(dataOriginal, out System.DateTime data))
            return data.ToString("dd/MM/yyyy");

        return "";
    }
}
