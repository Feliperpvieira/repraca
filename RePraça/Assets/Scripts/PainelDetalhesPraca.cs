using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Anexe este componente ao objecto raiz do popup de detalhes do arquivo.
// As referências são configuradas no Inspector quando o popup estiver montado.
public class PainelDetalhesPraca : MonoBehaviour
{
    [Header("Raiz")]
    [Tooltip("Se ficar vazio, este próprio GameObject é usado como popup.")]
    public GameObject raizPopup;

    [Header("Conteúdo")]
    public RawImage rawImagePraca;
    public TextMeshProUGUI txtNome;
    public TextMeshProUGUI txtData;
    public TextMeshProUGUI txtFoco;
    public TextMeshProUGUI txtItens;

    [Header("Botões")]
    public Button btnFechar;
    public Button btnAnterior;
    public Button btnSeguinte;
    public Button btnEditar;
    public Button btnEliminar;

    private ArquivoManager arquivoManager;
    private List<BtnArquivoItem> itens = new List<BtnArquivoItem>();
    private int indiceActual;

    private void Awake()
    {
        if (raizPopup == null)
            raizPopup = gameObject;

        if (btnFechar != null)
            btnFechar.onClick.AddListener(Fechar);
        if (btnAnterior != null)
            btnAnterior.onClick.AddListener(MostrarAnterior);
        if (btnSeguinte != null)
            btnSeguinte.onClick.AddListener(MostrarSeguinte);
        if (btnEditar != null)
            btnEditar.onClick.AddListener(AbrirPracaActual);
        if (btnEliminar != null)
            btnEliminar.onClick.AddListener(EliminarPracaActual);
    }

    public void Abrir(ArquivoManager gestor, List<BtnArquivoItem> itensDoArquivo, int indiceInicial)
    {
        // Awake ainda não correu se o próprio popup começar desactivado na cena.
        if (raizPopup == null)
            raizPopup = gameObject;

        arquivoManager = gestor;
        itens = itensDoArquivo ?? new List<BtnArquivoItem>();
        indiceActual = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, itens.Count - 1));

        AtualizarConteudo();
        raizPopup.SetActive(true);
    }

    public void Fechar()
    {
        raizPopup.SetActive(false);
    }

    private void MostrarAnterior()
    {
        if (indiceActual > 0)
        {
            indiceActual--;
            AtualizarConteudo();
        }
    }

    private void MostrarSeguinte()
    {
        if (indiceActual < itens.Count - 1)
        {
            indiceActual++;
            AtualizarConteudo();
        }
    }

    private void AbrirPracaActual()
    {
        BtnArquivoItem item = ObterItemActual();
        if (item == null || arquivoManager == null)
            return;

        arquivoManager.AbrirPraca(item.caminhoArquivo, item.dados != null ? item.dados.nomeDaCena : "");
    }

    public void EliminarPracaActual()
    {
        BtnArquivoItem item = ObterItemActual();
        if (item == null || arquivoManager == null)
            return;

        // Se quiseres uma confirmação, liga este botão a um pequeno popup de
        // confirmação e chama este método apenas no botão "Eliminar" dele.
        arquivoManager.ExcluirArquivo(item.caminhoArquivo);
        Fechar();
    }

    private void AtualizarConteudo()
    {
        BtnArquivoItem item = ObterItemActual();
        JsonPayloadData dados = item != null ? item.dados : null;

        if (txtNome != null)
            txtNome.text = dados != null && !string.IsNullOrEmpty(dados.nomeDaCena)
                ? dados.nomeDaCena
                : "Praça desconhecida";

        if (txtData != null)
            txtData.text = "Última edição: " + (dados != null ? BtnArquivoItem.FormatarData(dados.dataCriacao) : "");

        if (rawImagePraca != null)
            rawImagePraca.texture = item != null ? item.miniatura : null;

        if (txtFoco != null)
            txtFoco.text = "Foco: " + ObterFoco(dados);

        if (txtItens != null)
            txtItens.text = "Itens: " + ResumirItens(dados);

        if (btnAnterior != null)
            btnAnterior.interactable = indiceActual > 0;
        if (btnSeguinte != null)
            btnSeguinte.interactable = indiceActual < itens.Count - 1;
    }

    private BtnArquivoItem ObterItemActual()
    {
        if (indiceActual < 0 || indiceActual >= itens.Count)
            return null;

        return itens[indiceActual];
    }

    private static string ObterFoco(JsonPayloadData dados)
    {
        if (dados == null || dados.layoutDaPraca == null || dados.layoutDaPraca.Count == 0)
            return "Ainda sem elementos";

        Dictionary<string, int> totaisPorCategoria = new Dictionary<string, int>();
        string categoriaPrincipal = "Elementos";
        int maiorTotal = 0;

        foreach (ObjetoPosicionadoData item in dados.layoutDaPraca)
        {
            string categoria = string.IsNullOrEmpty(item.categoria) ? "Elementos" : item.categoria;
            if (!totaisPorCategoria.ContainsKey(categoria))
                totaisPorCategoria[categoria] = 0;

            totaisPorCategoria[categoria]++;
            if (totaisPorCategoria[categoria] > maiorTotal)
            {
                maiorTotal = totaisPorCategoria[categoria];
                categoriaPrincipal = categoria;
            }
        }

        return categoriaPrincipal;
    }

    private static string ResumirItens(JsonPayloadData dados)
    {
        if (dados == null || dados.layoutDaPraca == null || dados.layoutDaPraca.Count == 0)
            return "Ainda não foram adicionados elementos";

        Dictionary<string, int> totaisPorNome = new Dictionary<string, int>();
        List<string> ordemDosNomes = new List<string>();

        foreach (ObjetoPosicionadoData item in dados.layoutDaPraca)
        {
            string nome = string.IsNullOrEmpty(item.nome) ? "Elemento sem nome" : item.nome;
            if (!totaisPorNome.ContainsKey(nome))
            {
                totaisPorNome[nome] = 0;
                ordemDosNomes.Add(nome);
            }

            totaisPorNome[nome]++;
        }

        StringBuilder resumo = new StringBuilder();
        for (int i = 0; i < ordemDosNomes.Count; i++)
        {
            if (i > 0)
                resumo.Append(", ");

            string nome = ordemDosNomes[i];
            resumo.Append(totaisPorNome[nome]);
            resumo.Append("× ");
            resumo.Append(nome);
        }

        return resumo.ToString();
    }
}
