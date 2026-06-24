using UnityEngine;
using System.Collections;

public class OpenURL : MonoBehaviour
{
    public string url;

    public void GoToURL()
    {
        Application.OpenURL(url);
    }
}
