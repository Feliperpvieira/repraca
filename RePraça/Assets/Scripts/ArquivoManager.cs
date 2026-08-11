using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class ArquivoManager : MonoBehaviour
{
    [Header("Configurações da UI")]
    public GameObject prefabBotaoArquivo;
    public Transform conteudoScroll;

    [Header("Popup de detalhes")]
    public PainelDetalhesPraca painelDetalhes;

    // Guarda todos os cards já instanciados, na ordem em que aparecem na lista —
    // é o que permite o popup navegar entre praças com as setas.
    private List<BtnArquivoItem> itensCarregados = new List<BtnArquivoItem>();

    void Start()
    {
        CarregarListaUI();
    }

    public void CarregarListaUI()
    {
        foreach (Transform filho in conteudoScroll)
        {
            Destroy(filho.gameObject);
        }
        itensCarregados.Clear();

        string[] ficheiros = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (string caminho in ficheiros)
        {
            string json = File.ReadAllText(caminho);
            JsonPayloadData dados = JsonUtility.FromJson<JsonPayloadData>(json);

            GameObject novoBotao = Instantiate(prefabBotaoArquivo, conteudoScroll);

            // Carrega a foto de topo salva junto com o .json (mesmo nome, extensão .jpg)
            Texture2D textura = null;
            string caminhoImagem = caminho.Replace(".json", ".jpg");
            if (File.Exists(caminhoImagem))
            {
                byte[] bytesImagem = File.ReadAllBytes(caminhoImagem);
                textura = new Texture2D(2, 2);
                textura.LoadImage(bytesImagem);
            }

            // Preenche Nome, Data e a foto, e guarda os dados completos no próprio
            // botão — é isso que permite o popup de detalhes usar tudo depois.
            BtnArquivoItem itemUI = novoBotao.GetComponent<BtnArquivoItem>();
            if (itemUI == null)
            {
                Debug.LogError("O prefab '" + prefabBotaoArquivo.name + "' precisa do componente BtnArquivoItem.");
                continue;
            }

            itemUI.Preencher(dados, caminho, textura);
            itensCarregados.Add(itemUI);

            Button botao = novoBotao.GetComponent<Button>();
            if (botao != null)
            {
                // O BtnArquivoItem já mantém o JSON, o caminho e a textura.
                // Assim, o clique só precisa passar a referência do próprio cartão.
                BtnArquivoItem itemDoBotao = itemUI;
                botao.onClick.AddListener(() => AbrirPopupDetalhes(itemDoBotao));
            }
            else
            {
                Debug.LogError("O prefab '" + prefabBotaoArquivo.name + "' precisa de um componente Button no objecto raiz.");
            }
        }
    }

    // Clique no card: abre o popup com os detalhes (nome, data, foto, itens...).
    // Enquanto o popup ainda não estiver montado na cena, cai de volta para
    // abrir a praça diretamente, para não deixar o clique sem nenhum efeito.
    public void AbrirPopupDetalhes(BtnArquivoItem item)
    {
        if (item == null)
            return;

        // Mantém o arquivo utilizável enquanto o popup ainda não existe na cena.
        if (painelDetalhes == null)
        {
            Debug.LogWarning("[ArquivoManager] O PainelDetalhesPraca ainda não está configurado. A abrir a praça directamente.");
            AbrirPraca(item.caminhoArquivo, item.dados != null ? item.dados.nomeDaCena : "");
            return;
        }

        int indice = itensCarregados.IndexOf(item);
        painelDetalhes.Abrir(this, itensCarregados, indice < 0 ? 0 : indice);
    }

    // Chamado pelo botão "Editar" do popup (e usado como fallback antes do popup existir)
    public void AbrirPraca(string caminhoDoFicheiro, string cenaParaCarregar)
    {
        if (string.IsNullOrEmpty(cenaParaCarregar))
        {
            Debug.LogError("O ficheiro não tem nome de cena guardado. A usar cena de contingência.");
            cenaParaCarregar = "NomeDaSuaCenaBase";
        }

        PlayerPrefs.SetString("PracaParaCarregar", caminhoDoFicheiro);
        PlayerPrefs.Save();

        SceneManager.LoadScene(cenaParaCarregar);
    }

    // Chamado pelo botão de lixeira do popup
    public void ExcluirArquivo(string caminhoDoFicheiro)
    {
        if (File.Exists(caminhoDoFicheiro))
            File.Delete(caminhoDoFicheiro);

        string caminhoImagem = caminhoDoFicheiro.Replace(".json", ".jpg");
        if (File.Exists(caminhoImagem))
            File.Delete(caminhoImagem);

        CarregarListaUI();
    }

    public void CriarNovaPraca(string nomeDaCenaEscolhida)
    {
        PlayerPrefs.DeleteKey("PracaParaCarregar");
        PlayerPrefs.DeleteKey("PracaRemixJSON");
        SceneManager.LoadScene(nomeDaCenaEscolhida);
    }
}
