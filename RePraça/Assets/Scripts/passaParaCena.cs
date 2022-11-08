using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class passaParaCena : MonoBehaviour
{
    public void VaiParaCena(string nomeDaCena)
    {
        SceneManager.LoadScene(nomeDaCena);
    }
}
