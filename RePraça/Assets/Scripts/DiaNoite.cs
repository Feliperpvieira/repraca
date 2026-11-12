using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PostHogUnity;

public class DiaNoite : MonoBehaviour
{
    public Light solDaCena; //luz que é o sol de dia
    public Light luaDaCena; //luz que é a lua de noite
    public Toggle toggleNoiteDia; //toggle entre luz de dia e de noite

    float lerpDuration = 1.5f; //duração da animação
    Quaternion rotacaoSolDia; //rotações do sol e da lua no estado "dia", guardadas no Start
    Quaternion rotacaoLuaDia;

    [Header("Ambiente (Trilight)")] //cores do ambiente (trilight) para dia e noite
    public Color skyDia = new Color(0.3f, 0.32f, 0.36f);
    public Color equatorDia = new Color(0.25f, 0.25f, 0.24f);
    public Color groundDia = new Color(0.11f, 0.12f, 0.10f);
    public Color skyNoite = new Color(0.08f, 0.10f, 0.22f);
    public Color equatorNoite = new Color(0.07f, 0.08f, 0.15f);
    public Color groundNoite = new Color(0.04f, 0.04f, 0.07f);

    [Header("Reflexos")] //intensidade dos reflexos para dia e noite
    public float reflexoDia = 0.5f;
    public float reflexoNoite = 0.15f;

    void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        AplicaAmbiente(toggleNoiteDia.isOn ? 0f : 1f); // 0 = dia, 1 = noite

        // Guarda como ficam o sol e a lua de dia (cada troca soma 180 graus em X),
        // para o ForcarDia poder pôr tudo de dia sem animação
        Quaternion meiaVolta = toggleNoiteDia.isOn ? Quaternion.identity : Quaternion.Euler(180, 0, 0);
        rotacaoSolDia = solDaCena.transform.rotation * meiaVolta;
        rotacaoLuaDia = luaDaCena.transform.rotation * meiaVolta;
    }

    void AplicaAmbiente(float t)
    {
        RenderSettings.ambientSkyColor = Color.Lerp(skyDia, skyNoite, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(equatorDia, equatorNoite, t);
        RenderSettings.ambientGroundColor = Color.Lerp(groundDia, groundNoite, t);
        RenderSettings.reflectionIntensity = Mathf.Lerp(reflexoDia, reflexoNoite, t); // t: 0 = dia, 1 = noite p/ reflexos
    }

    //funcao que é chamada ao apertar o toggle
    public void ToggleIluminacao()
    {
        // o toggle já mudou quando isto é chamado: isOn = dia (ver MudaNoiteDia)
        PostHog.Capture("day-night_toggled", new Dictionary<string, object>
        {
            { "modo", toggleNoiteDia.isOn ? "dia" : "noite" }
        }); //posthog: mudou a luz entre dia e noite
        StartCoroutine(MudaNoiteDia());
    }

    // Chamado ao abrir a tela de guardar: se estiver de noite, passa para dia NA HORA
    // (sem animação, para a imagem nunca sair a meio da transição).
    // Não envia day-night_toggled, porque não foi o jogador a mexer no toggle.
    public void ForcarDia()
    {
        StopAllCoroutines(); //corta uma animação dia/noite que ainda esteja a correr

        toggleNoiteDia.SetIsOnWithoutNotify(true); //isOn = dia, sem chamar ToggleIluminacao
        toggleNoiteDia.interactable = true;

        solDaCena.transform.rotation = rotacaoSolDia;
        luaDaCena.transform.rotation = rotacaoLuaDia;
        AplicaAmbiente(0f);
        AcendeOsPostes(); //de dia, desliga as luzes dos postes
    }

    //Coroutine que faz o sol e lua girarem no tempo da animação
    IEnumerator MudaNoiteDia()
    {
        toggleNoiteDia.interactable = false; //impede que toque no toggle enquanto a animacao roda
        float timeElapsed = 0;

        //inicio e fim da animação do sol e lua, ele soma 180 graus nos doias pra fazer eles girarem meio dia
        Quaternion startRotationSol = solDaCena.transform.rotation;
        Quaternion targetRotationSol = solDaCena.transform.rotation * Quaternion.Euler(180, 0, 0);

        Quaternion startRotationLua = luaDaCena.transform.rotation;
        Quaternion targetRotationLua = luaDaCena.transform.rotation * Quaternion.Euler(180, 0, 0);

        //calcula inicio e alvo da interpolação do ambiente (trilight) baseado no toggle, 0 = dia, 1 = noite
        float alvo = toggleNoiteDia.isOn ? 0f : 1f;
        float inicio = 1f - alvo;

        while (timeElapsed < lerpDuration) //animacao da luz girando no tempo definido
        {
            solDaCena.transform.rotation = Quaternion.Slerp(startRotationSol, targetRotationSol, timeElapsed / lerpDuration);
            luaDaCena.transform.rotation = Quaternion.Slerp(startRotationLua, targetRotationLua, timeElapsed / lerpDuration);
            AplicaAmbiente(Mathf.Lerp(inicio, alvo, timeElapsed / lerpDuration));
            
            timeElapsed += Time.deltaTime;
            yield return null;
        } 
        AplicaAmbiente(alvo); //garante que o ambiente esta no alvo correto apos a animacao terminar

        //garante que o posicionamento das luzes esta certo após a animacao terminar
        solDaCena.transform.rotation = targetRotationSol;
        luaDaCena.transform.rotation = targetRotationLua;

        AcendeOsPostes();

        toggleNoiteDia.interactable = true; //retoma o toggle
    }

    public void AcendeOsPostes() //em uma funcao separada para poder ser acionado do building manager ao construir novos postes de noite
    {
        GameObject[] allLights = GameObject.FindGameObjectsWithTag("LuzPoste"); //procura todos os game objects com a tag LuzPoste

        foreach (GameObject i in allLights) //para cada objeto (a luz spotlight) com a tag LuzPoste
        {
            Light[] luzesPoste = i.GetComponents<Light>(); //adiciona o componente Light dentro de um array

            foreach (Light n in luzesPoste) //para cada componente Light das luzes
            {
                if (toggleNoiteDia.isOn) //se estiver de dia
                {
                    n.enabled = false; //desliga a luz
                }
                else //se estiver de noite
                {
                    n.enabled = true; //ativa a luz
                }
            }
        }
    }

}
