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

    // REMOVEMOS A VARIÁVEL FIXA DO NOME DA CENA AQUI

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

        string[] ficheiros = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (string caminho in ficheiros)
        {
            string json = File.ReadAllText(caminho);
            JsonPayloadData dados = JsonUtility.FromJson<JsonPayloadData>(json);

            GameObject novoBotao = Instantiate(prefabBotaoArquivo, conteudoScroll);

            TextMeshProUGUI textoBotao = novoBotao.GetComponentInChildren<TextMeshProUGUI>();
            if (textoBotao != null && dados != null)
            {
                // Agora podemos até mostrar o nome da cena original no botão do menu!
                textoBotao.text = dados.nomeDaCena + " (" + dados.dataCriacao + ")";
            }
            else
            {
                textoBotao.text = "Praça Desconhecida";
            }

            //foto da praça:
            string caminhoImagem = caminho.Replace(".json", ".jpg");

            // Procura o componente RawImage no seu Prefab
            RawImage miniaturaUI = novoBotao.GetComponentInChildren<RawImage>();

            if (miniaturaUI != null && File.Exists(caminhoImagem))
            {
                // Lê os bytes da imagem
                byte[] bytesImagem = File.ReadAllBytes(caminhoImagem);

                // Cria a textura vazia (o LoadImage sobrepõe as medidas automaticamente)
                Texture2D textura = new Texture2D(2, 2);
                textura.LoadImage(bytesImagem);

                // Aplica a textura carregada na UI
                miniaturaUI.texture = textura;
            }

            // Passamos o caminho e o NOME DA CENA diretamente para a função do botão
            novoBotao.GetComponent<Button>().onClick.AddListener(() => ClicouNumArquivo(caminho, dados.nomeDaCena));
        }
    }

    // A função agora recebe o nome da cena como parâmetro
    void ClicouNumArquivo(string caminhoDoFicheiro, string cenaParaCarregar)
    {
        // Se por acaso tentar abrir um ficheiro antigo salvo antes de adicionarmos a variável "nomeDaCena"
        if (string.IsNullOrEmpty(cenaParaCarregar))
        {
            Debug.LogError("O ficheiro não tem nome de cena guardado. A usar cena de contingência.");
            cenaParaCarregar = "NomeDaSuaCenaBase"; // Escreva aqui a cena principal para evitar crash em saves antigos
        }

        PlayerPrefs.SetString("PracaParaCarregar", caminhoDoFicheiro);
        PlayerPrefs.Save();

        // Carrega dinamicamente a cena correspondente!
        SceneManager.LoadScene(cenaParaCarregar);
    }

    // NOTA: Para o botão de "Criar Nova", como o utilizador vai escolher a cena inicial?
    // Se tiver várias praças para escolher, este botão deve abrir um sub-menu com as opções (ex: "Praça 1", "Praça 2")
    public void CriarNovaPraca(string nomeDaCenaEscolhida)
    {
        PlayerPrefs.DeleteKey("PracaParaCarregar");
        SceneManager.LoadScene(nomeDaCenaEscolhida);
    }
}