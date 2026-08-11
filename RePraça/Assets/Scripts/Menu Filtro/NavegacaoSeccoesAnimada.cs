using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Navega entre painéis de conteúdo, por exemplo "Infos da praça", "Ajuda do app"
// e "Opções". É deliberadamente separado do FiltroManager, que só cuida das cores.
public class NavegacaoSeccoesAnimada : MonoBehaviour
{
    [Serializable]
    public class Secao
    {
        public Button botao;
        public GameObject conteudo;
    }

    [Header("Secções")]
    public List<Secao> secoes = new List<Secao>();
    [Min(0)] public int indiceInicial;

    [Header("Animação")]
    [Min(0.05f)] public float duracaoSaida = 0.10f;
    [Min(0.05f)] public float duracaoEntrada = 0.16f;

    private int indiceActual = -1;
    private readonly Dictionary<GameObject, CanvasGroup> grupos = new Dictionary<GameObject, CanvasGroup>();
    private readonly Dictionary<GameObject, Vector3> escalasOriginais = new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        for (int i = 0; i < secoes.Count; i++)
        {
            int indice = i;
            if (secoes[i].botao != null)
                secoes[i].botao.onClick.AddListener(() => MostrarSecao(indice));
        }
    }

    private void Start()
    {
        MostrarSecao(Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, secoes.Count - 1)), false);
    }

    public void MostrarSecao(int novoIndice)
    {
        MostrarSecao(novoIndice, true);
    }

    private void MostrarSecao(int novoIndice, bool animar)
    {
        if (novoIndice < 0 || novoIndice >= secoes.Count || novoIndice == indiceActual)
            return;

        GameObject painelAnterior = indiceActual >= 0 ? secoes[indiceActual].conteudo : null;
        GameObject painelNovo = secoes[novoIndice].conteudo;
        if (painelNovo == null)
            return;

        if (painelAnterior != null)
        {
            CanvasGroup grupoAnterior = ObterGrupo(painelAnterior);
            LeanTween.cancel(painelAnterior);

            if (animar && painelAnterior.activeSelf)
            {
                LeanTween.alphaCanvas(grupoAnterior, 0f, duracaoSaida).setEaseInQuad()
                    .setOnComplete(() => painelAnterior.SetActive(false));
            }
            else
            {
                painelAnterior.SetActive(false);
            }
        }

        indiceActual = novoIndice;
        painelNovo.SetActive(true);

        CanvasGroup grupoNovo = ObterGrupo(painelNovo);
        LeanTween.cancel(painelNovo);

        if (!animar)
        {
            grupoNovo.alpha = 1f;
            painelNovo.transform.localScale = ObterEscalaOriginal(painelNovo);
            return;
        }

        grupoNovo.alpha = 0f;
        painelNovo.transform.localScale = ObterEscalaOriginal(painelNovo) * 0.98f;
        LeanTween.alphaCanvas(grupoNovo, 1f, duracaoEntrada).setEaseOutQuad();
        LeanTween.scale(painelNovo, ObterEscalaOriginal(painelNovo), duracaoEntrada).setEaseOutBack();
    }

    private CanvasGroup ObterGrupo(GameObject painel)
    {
        if (!grupos.TryGetValue(painel, out CanvasGroup grupo))
        {
            grupo = painel.GetComponent<CanvasGroup>();
            if (grupo == null)
                grupo = painel.AddComponent<CanvasGroup>();
            grupos.Add(painel, grupo);
        }

        return grupo;
    }

    private Vector3 ObterEscalaOriginal(GameObject painel)
    {
        if (!escalasOriginais.TryGetValue(painel, out Vector3 escala))
        {
            escala = painel.transform.localScale;
            escalasOriginais.Add(painel, escala);
        }

        return escala;
    }
}
