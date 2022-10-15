using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiaNoite : MonoBehaviour
{
    public Light solDaCena; //luz que é o sol de dia
    public Light luaDaCena; //luz que é a lua de noite
    public Toggle toggleNoiteDia; //toggle entre luz de dia e de noite

    float lerpDuration = 1.5f; //duração da animação

    //funcao que é chamada ao apertar o toggle
    public void ToggleIluminacao()
    {
        StartCoroutine(MudaNoiteDia());
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

        while (timeElapsed < lerpDuration) //animacao da luz girando no tempo definido
        {
            solDaCena.transform.rotation = Quaternion.Slerp(startRotationSol, targetRotationSol, timeElapsed / lerpDuration);
            luaDaCena.transform.rotation = Quaternion.Slerp(startRotationLua, targetRotationLua, timeElapsed / lerpDuration);

            timeElapsed += Time.deltaTime;
            yield return null;
        }
        //garante que o posicionamento das luzes esta certo após a animacao terminar
        solDaCena.transform.rotation = targetRotationSol;
        luaDaCena.transform.rotation = targetRotationLua;


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


        toggleNoiteDia.interactable = true; //retoma o toggle
    }


}
