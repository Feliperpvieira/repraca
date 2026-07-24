using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeUIPopUpItens : MonoBehaviour
{
    [Header("Configurações")]
    public ObjetosData[] todosOsObjetos; // Puxe a mesma lista que usa na loja
    public Transform[] pontosDeSpawn; // Arraste os objetos vazios para aqui
    public float tempoEntreAcoes = 2.5f; // De quanto em quanto tempo algo acontece
    public int maxObjetosSimultaneos = 8; // Máximo de objetos na tela

    // Dicionário para ligar o objeto gerado ao ponto de spawn que ele está a ocupar
    private Dictionary<GameObject, Transform> objetosAtivos = new Dictionary<GameObject, Transform>();
    private List<Transform> pontosLivres = new List<Transform>();

    void Start()
    {
        // Todos os pontos começam livres
        pontosLivres.AddRange(pontosDeSpawn);

        // Inicia o ciclo infinito de construção e destruição
        StartCoroutine(RotinaDaPraca());
    }

    IEnumerator RotinaDaPraca()
    {
        while (true) // Corre para sempre enquanto o menu estiver aberto
        {
            yield return new WaitForSeconds(tempoEntreAcoes);

            // Verifica se bateu no teto máximo
            bool estaCheio = objetosAtivos.Count >= maxObjetosSimultaneos;

            // Se estiver cheio, apaga obrigatoriamente. 
            // Se NÃO estiver cheio, tem apenas 20% de chance de apagar (para dar movimento) e 80% de chance de criar.
            bool querApagar = estaCheio || (objetosAtivos.Count > 0 && Random.value > 0.80f);

            if (querApagar)
            {
                ApagarObjetoAleatorio();
            }
            else if (pontosLivres.Count > 0)
            {
                CriarObjetoAleatorio();
            }
        }
    }

    void CriarObjetoAleatorio()
    {
        // 1. Escolhe um ponto livre aleatório e remove-o da lista de pontos livres
        int indexPonto = Random.Range(0, pontosLivres.Count);
        Transform pontoEscolhido = pontosLivres[indexPonto];
        pontosLivres.RemoveAt(indexPonto);

        // 2. Escolhe um objeto aleatório
        ObjetosData dados = todosOsObjetos[Random.Range(0, todosOsObjetos.Length)];

        // 3. ROTAÇÃO CORRIGIDA: Mantém o X e Z originais para o objeto não "tropeçar", e randomiza só o Y
        Vector3 rotOriginal = dados.prefab.transform.eulerAngles;
        Quaternion novaRotacao = Quaternion.Euler(rotOriginal.x, Random.Range(0f, 360f), rotOriginal.z);

        // 4. Cria o objeto
        GameObject novoObj = Instantiate(dados.prefab, pontoEscolhido.position, novaRotacao);

        // 5. CORREÇÃO DE ERRO NULL: Remove o CheckPlacement para não procurar o BuildingManager no menu
        CheckPlacement checkScript = novoObj.GetComponent<CheckPlacement>();
        if (checkScript != null)
        {
            Destroy(checkScript);
        }

        // (Prevenção extra) Remove também o Outline, caso o seu prefab venha com ele ativado por defeito
        Outline outlineScript = novoObj.GetComponent<Outline>();
        if (outlineScript != null)
        {
            Destroy(outlineScript);
        }

        // 6. Salva no dicionário para sabermos quem está onde
        objetosAtivos.Add(novoObj, pontoEscolhido);

        // 7. ANIMAÇÃO DE APARECER
        Vector3 escalaOriginal = novoObj.transform.localScale;
        novoObj.transform.localScale = Vector3.zero;
        LeanTween.scale(novoObj, escalaOriginal, 0.4f).setEaseOutBack();
    }

    void ApagarObjetoAleatorio()
    {
        // 1. Escolhe um objeto aleatório dos que estão na tela
        List<GameObject> chaves = new List<GameObject>(objetosAtivos.Keys);
        GameObject objParaApagar = chaves[Random.Range(0, chaves.Count)];

        // 2. Devolve o ponto de spawn à lista de pontos livres para ser usado no futuro
        Transform pontoLibertado = objetosAtivos[objParaApagar];
        pontosLivres.Add(pontoLibertado);

        // 3. Remove do dicionário
        objetosAtivos.Remove(objParaApagar);

        // 4. ANIMAÇÃO DE DESAPARECER
        LeanTween.scale(objParaApagar, Vector3.zero, 0.2f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                Destroy(objParaApagar);
            });
    }
}