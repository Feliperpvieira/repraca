using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using System;
using System.Globalization;

public class ArquivoManager : MonoBehaviour
{
    private enum OrdemArquivo
    {
        Cronologica,
        Praca
    }

    [Header("Configurações da UI")]
    public GameObject prefabBotaoArquivo;
    public Transform conteudoScroll;

    [Header("Cena única do jogo")]
    public string nomeCenaJogo = "Jogo";

    [Header("Popup de detalhes")]
    public PainelDetalhesPraca painelDetalhes;

    // Guarda todos os cards já instanciados, na ordem em que aparecem na lista —
    // é o que permite o popup navegar entre praças com as setas.
    private List<BtnArquivoItem> itensCarregados = new List<BtnArquivoItem>();
    private OrdemArquivo ordemActual = OrdemArquivo.Cronologica;

    [Header("Animação dos cartões")]
    [Min(0.05f)] public float duracaoEntradaCartao = 0.16f;
    [Min(0f)] public float atrasoMaximoEntrada = 0.18f;

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
        List<ArquivoGuardado> arquivos = LerEOrdenarArquivos(ficheiros);

        for (int indice = 0; indice < arquivos.Count; indice++)
        {
            ArquivoGuardado arquivo = arquivos[indice];
            string caminho = arquivo.caminho;
            JsonPayloadData dados = arquivo.dados;

            GameObject novoBotao = Instantiate(prefabBotaoArquivo, conteudoScroll);

            // Carrega a foto de topo salva junto com o .json (mesmo nome, extensão .jpg)
            Texture2D textura = null;
            string caminhoImagem = caminho.Replace(".json", ".jpg");
            if (File.Exists(caminhoImagem))
            {
                byte[] bytesImagem = File.ReadAllBytes(caminhoImagem);
                textura = new Texture2D(2, 2, TextureFormat.RGB24, true);
                textura.LoadImage(bytesImagem);
                textura.filterMode = FilterMode.Bilinear;
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

            AnimarEntradaCartao(novoBotao, indice);
        }
    }

    // Chamado pelo botão "Cronológica". A data mais recente vem primeiro.
    public void OrdenarCronologicamente()
    {
        ordemActual = OrdemArquivo.Cronologica;
        CarregarListaUI();
    }

    // Chamado pelo botão "Praça". Agrupa alfabeticamente pelo nome da cena e,
    // dentro de cada praça, mantém a edição mais recente primeiro.
    public void OrdenarPorPraca()
    {
        ordemActual = OrdemArquivo.Praca;
        CarregarListaUI();
    }

    private List<ArquivoGuardado> LerEOrdenarArquivos(string[] ficheiros)
    {
        List<ArquivoGuardado> arquivos = new List<ArquivoGuardado>();

        foreach (string caminho in ficheiros)
        {
            try
            {
                JsonPayloadData dados = JsonUtility.FromJson<JsonPayloadData>(File.ReadAllText(caminho));
                if (dados != null)
                    arquivos.Add(new ArquivoGuardado(caminho, dados));
            }
            catch (Exception erro)
            {
                // Um ficheiro corrompido não deve impedir a abertura dos restantes.
                Debug.LogWarning("[ArquivoManager] Não foi possível ler '" + Path.GetFileName(caminho) + "': " + erro.Message);
            }
        }

        CompareInfo comparador = CultureInfo.GetCultureInfo("pt-PT").CompareInfo;
        arquivos.Sort((a, b) =>
        {
            if (ordemActual == OrdemArquivo.Praca)
            {
                int comparacaoNome = comparador.Compare(a.nomeDaPraca, b.nomeDaPraca, CompareOptions.IgnoreCase);
                if (comparacaoNome != 0)
                    return comparacaoNome;
            }

            // O sinal invertido coloca datas mais recentes primeiro.
            int comparacaoData = b.dataEdicao.CompareTo(a.dataEdicao);
            if (comparacaoData != 0)
                return comparacaoData;

            // Critério final estável para dois saves criados no mesmo minuto.
            return comparador.Compare(a.caminho, b.caminho, CompareOptions.IgnoreCase);
        });

        return arquivos;
    }

    private void AnimarEntradaCartao(GameObject cartao, int indice)
    {
        CanvasGroup grupo = cartao.GetComponent<CanvasGroup>();
        if (grupo == null)
            grupo = cartao.AddComponent<CanvasGroup>();

        grupo.alpha = 0f;
        cartao.transform.localScale = Vector3.one * 0.94f;

        float atraso = Mathf.Min(indice * 0.025f, atrasoMaximoEntrada);
        LeanTween.alphaCanvas(grupo, 1f, duracaoEntradaCartao).setDelay(atraso).setEaseOutQuad();
        LeanTween.scale(cartao, Vector3.one, duracaoEntradaCartao).setDelay(atraso).setEaseOutBack();
    }

    // Estrutura temporária: concentra os campos usados para ordenar sem guardar
    // lógica de apresentação dentro do próprio prefab do cartão.
    private class ArquivoGuardado
    {
        public readonly string caminho;
        public readonly JsonPayloadData dados;
        public readonly DateTime dataEdicao;
        public readonly string nomeDaPraca;

        public ArquivoGuardado(string caminhoDoArquivo, JsonPayloadData dadosDoArquivo)
        {
            caminho = caminhoDoArquivo;
            dados = dadosDoArquivo;
            nomeDaPraca = string.IsNullOrWhiteSpace(dados.nomeDaCena) ? "Praça desconhecida" : dados.nomeDaCena;

            DateTime dataLida;
            dataEdicao = DateTime.TryParse(dados.dataCriacao, out dataLida) ? dataLida : DateTime.MinValue;
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

    // Chamado pelo botão "Editar" do popup
    public void AbrirPraca(string caminhoDoFicheiro, string nomeDaCenaSalvo)
    {
        PlayerPrefs.SetString("PracaParaCarregar", caminhoDoFicheiro);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nomeCenaJogo);
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

    // CriarNovaPraca agora recebe o ID da praça (do catálogo), não mais um nome
    // de cena Unity — é chamado pelos botões do Menu, um por praça disponível.
    public void CriarNovaPraca(string mapaId)
    {
        PlayerPrefs.DeleteKey("PracaParaCarregar");
        PlayerPrefs.DeleteKey("PracaRemixJSON");
        PlayerPrefs.SetString("PracaIdNova", mapaId);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nomeCenaJogo);
    }
}
