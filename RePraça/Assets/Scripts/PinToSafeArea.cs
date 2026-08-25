using UnityEngine;
/// Resizes a UI element with a RectTransform to respect the safe areas of the current device.
/// This is particularly useful on an iPhone X, where we have to avoid the notch and the screen
/// corners.
/// 
/// code: mix de https://www.geeksforgeeks.org/c-sharp/responsive-ui-design-in-unity/
/// e https://gist.github.com/SeanMcTex/c28f6e56b803cdda8ed7acb1b0db6f82?permalink_comment_id=4863421
/// 
public class PinToSafeArea : MonoBehaviour {

    private Rect lastSafeArea;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();

            //Debug.Log("Safe area mudou");
        }
    }


    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        lastSafeArea = Screen.safeArea;
    }
}
