using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PostHogUnity;

public class OpenURL : MonoBehaviour
{
    public string url;

    public void GoToURL()
    {
        //posthog: abriu link externo (galeria web, termos, privacidade...)
        PostHog.Capture("external_link_opened", new Dictionary<string, object>
        {
            { "url", url }
        });

        Application.OpenURL(url);
    }
}
