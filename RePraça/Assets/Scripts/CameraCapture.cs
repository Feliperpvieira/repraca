using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NativeGallery;

public class CameraCapture : MonoBehaviour
{
    public RenderTexture rtVistaTopo;
    public RenderTexture rtVistaAngulo;

    string album = "rePraca";
    MediaSaveCallback callback = null;

    public static string ScreenShotName(string nomeCena, string angulo) //define o nome do arquivo
    {
        return string.Format("praça_{0}-{1}_{2}.png",
                               nomeCena, angulo,
                               System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")); //data e hora atual

        //return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
        //                     Application.persistentDataPath,
        //                     width, height,
        //                     System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    public void SaveTexture()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        byte[] imagemTopo = toTexture2D(rtVistaTopo, 1200, 1200).EncodeToPNG(); //transforma a renderTexture em texture 2d
        string fileName = ScreenShotName(sceneName, "topo"); //define o nome do arquivo
        //System.IO.File.WriteAllBytes(fileName, bytes);
        NativeGallery.SaveImageToGallery(imagemTopo, album, fileName, callback); //plugin native gallery https://github.com/yasirkula/UnityNativeGallery

        //Debug.Log(string.Format("Took screenshot to: {0}", fileName));

        byte[] imagemAngulo = toTexture2D(rtVistaAngulo, 1920, 1200).EncodeToPNG(); //transforma a renderTexture em texture 2d
        string fileNameAng = ScreenShotName(sceneName, "angulo"); //define o nome do arquivo
        NativeGallery.SaveImageToGallery(imagemAngulo, album, fileNameAng, callback);
    }

    Texture2D toTexture2D(RenderTexture rTex, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        Destroy(tex);//prevents memory leak
        return tex;
    }
}