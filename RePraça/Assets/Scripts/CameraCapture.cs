using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static NativeGallery;

public class CameraCapture : MonoBehaviour
{
    public RenderTexture rt;

    string album = "rePraca";
    MediaSaveCallback callback = null;

    public static string ScreenShotName(int width, int height) //define o nome do arquivo
    {
        return string.Format("praca_{0}x{1}_{2}.png",
                               width, height,
                               System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")); //data e hora atual

        //return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
        //                     Application.persistentDataPath,
        //                     width, height,
        //                     System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    public void SaveTexture()
    {
        byte[] bytes = toTexture2D(rt).EncodeToPNG(); //transforma a renderTexture em texture 2d
        string fileName = ScreenShotName(1080, 1080); //define o nome do arquivo
        //System.IO.File.WriteAllBytes(fileName, bytes);
        NativeGallery.SaveImageToGallery(bytes, album, fileName, callback); //plugin native gallery https://github.com/yasirkula/UnityNativeGallery

        //Debug.Log(string.Format("Took screenshot to: {0}", fileName));
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(1080, 1080, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        Destroy(tex);//prevents memory leak
        return tex;
    }
}