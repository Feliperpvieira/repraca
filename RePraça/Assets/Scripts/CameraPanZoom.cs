using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraPanZoom : MonoBehaviour
{
    //Esse código: https://kylewbanks.com/blog/unity3d-panning-and-pinch-to-zoom-camera-with-touch-and-mouse-input
    //Outra opção, com rotação: https://forum.unity.com/threads/mobile-touch-to-orbit-pan-and-zoom-camera-without-fix-target-in-one-script.522607/

    float offsetX;
    float offsetY;
    Vector3 move;

    private static readonly float PanSpeed = 20f;
    private static readonly float ZoomSpeedTouch = 0.1f;
    private static readonly float ZoomSpeedMouse = 0.5f;

    private static readonly float[] BoundsX = new float[] { -10f, 11f };
    private static readonly float[] BoundsZ = new float[] { -23f, 9f };
    private static readonly float[] ZoomBounds = new float[] { 10f, 85f };

    private Camera cam;

    private Vector3 lastPanPosition;
    private int panFingerId; // Touch mode only

    private bool wasZoomingLastFrame; // Touch mode only
    private Vector2[] lastZoomPositions; // Touch mode only


    private BuildingManager buildingManager;
    private GameObject cameraExport;
    public Toggle toogleAnguloCamera; //toggle entre angulos da camera
    float lerpDuration = 1f; //duração da animação entre angulos da camera no toggle

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        //coloca o objeto building manager da scene na variavel do codigo
        //vai servir pra pegar coisas do codigo do building manager pra cá
        buildingManager = GameObject.Find("BuildingManager").GetComponent<BuildingManager>();
    }

    void Update()
    {
        
        if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }
       
        
    }

    void HandleTouch()
    {
        switch (Input.touchCount)
        {

            case 1: // Panning
                wasZoomingLastFrame = false;

                // If the touch began, capture its position and its finger ID.
                // Otherwise, if the finger ID of the touch doesn't match, skip it.
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    lastPanPosition = touch.position; 
                    panFingerId = touch.fingerId;
                }
                else if (touch.fingerId == panFingerId) //REMOVIDO: && touch.phase == TouchPhase.Moved
                {
                    //if (Input.GetTouch(0).phase == TouchPhase.Moved) //checa se o toque esta batendo em um botao
                    //{
                    //    PanCamera(touch.position);
                    //}

                    //interfaceTopoSistemas são os botões brancos no topo da tela
                    //ela só é true na tela de edição da praça, sempre que abre outra tela ela vira falsa. então a movimentação da camera só funciona quando esses botoes estao ativos
                    if(buildingManager.interfaceTopoSistema.activeInHierarchy == true && Input.GetTouch(0).phase == TouchPhase.Moved && buildingManager.pendingObject == null)
                    {
                            PanCamera(touch.position);
                    }
                    //se estiver movendo algum objeto, move a câmera pelo lado da tela que o dedo estiver 
                    else if (!EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId) && Input.GetTouch(0).phase != TouchPhase.Ended && buildingManager.pendingObject != null)
                    {
                        PanCameraObject(touch.position);
                    }

                }
                break;

            case 2: // Zooming
                Vector2[] newPositions = new Vector2[] { Input.GetTouch(0).position, Input.GetTouch(1).position };
                if (!wasZoomingLastFrame)
                {
                    lastZoomPositions = newPositions;
                    wasZoomingLastFrame = true;
                }
                else
                {
                    // Zoom based on the distance between the new positions compared to the 
                    // distance between the previous positions.
                    float newDistance = Vector2.Distance(newPositions[0], newPositions[1]);
                    float oldDistance = Vector2.Distance(lastZoomPositions[0], lastZoomPositions[1]);
                    float offset = newDistance - oldDistance;

                    ZoomCamera(offset, ZoomSpeedTouch);

                    lastZoomPositions = newPositions;
                }
                break;

            default:
                wasZoomingLastFrame = false;
                break;
        }
    }

    void HandleMouse()
    {
        // On mouse down, capture it's position.
        // Otherwise, if the mouse is still down, pan the camera.
        if (Input.GetMouseButtonDown(0))
        {
            lastPanPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            PanCamera(Input.mousePosition);
        }

        // Check for scrolling to zoom the camera
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        ZoomCamera(scroll, ZoomSpeedMouse);
    }

    void PanCamera(Vector3 newPanPosition)
    {
        // Determine how much to move the camera
        Vector3 offset = cam.ScreenToViewportPoint(lastPanPosition - newPanPosition);
        move = new Vector3(offset.x * PanSpeed, 0, offset.y * PanSpeed);

        // Perform the movement
        transform.Translate(move, Space.World);

        // Ensure the camera remains within bounds.
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(transform.position.x, BoundsX[0], BoundsX[1]);
        pos.z = Mathf.Clamp(transform.position.z, BoundsZ[0], BoundsZ[1]);
        transform.position = pos;

        // Cache the position
        lastPanPosition = newPanPosition;
    }

    void ZoomCamera(float offset, float speed)
    {
        if (offset == 0)
        {
            return;
        }

        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - (offset * speed), ZoomBounds[0], ZoomBounds[1]);
    }

    void PanCameraObject(Vector3 newPanPosition)
    {
        //reseta pra 0 a movimentação de cada axis a cada frame
        offsetX = 0;
        offsetY = 0;

        //Direita
        if (newPanPosition.x > Screen.width * 0.9)
        {
            offsetX = 4f * Time.deltaTime;
        }
        else if (newPanPosition.x > Screen.width * 0.8)
        {
            offsetX = 2f * Time.deltaTime;
        }
        //Esquerda
        if (newPanPosition.x < Screen.width * 0.1)
        {
            offsetX = -4f * Time.deltaTime;
        }
        else if (newPanPosition.x < Screen.width * 0.2)
        {
            offsetX = -2f * Time.deltaTime;
        }
        //Cima
        if (newPanPosition.y > Screen.height * 0.9)
        {
            offsetY = 4f * Time.deltaTime;
        }
        else if (newPanPosition.y > Screen.height * 0.75)
        {
            offsetY = 2f * Time.deltaTime;
        }
        //Baixo
        if (newPanPosition.y < Screen.height * 0.1)
        {
            offsetY = -4f * Time.deltaTime;
        }
        else if (newPanPosition.y < Screen.height * 0.25)
        {
            offsetY = -2f * Time.deltaTime;
        }

        move = new Vector3(offsetX, 0, offsetY);


        // Perform the movement
        transform.Translate(move, Space.World);

        // Ensure the camera remains within bounds.
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(transform.position.x, BoundsX[0], BoundsX[1]);
        pos.z = Mathf.Clamp(transform.position.z, BoundsZ[0], BoundsZ[1]);
        transform.position = pos;

        // Cache the position
        lastPanPosition = newPanPosition;
    }

    //Toggle para alternar entre 90 e 45 graus na câmera
    public void ToggleCamera()
    {
        if (toogleAnguloCamera.isOn)
        {
            StartCoroutine(RotateCam(90f, 5, 12));
        }
        else
        {
            StartCoroutine(RotateCam(40f, -5, -12));
        }
    }
    //Coroutine que muda entre os ângulos de forma fluida - https://gamedevbeginner.com/how-to-rotate-in-unity-complete-beginners-guide/#rotate_over_time
    IEnumerator RotateCam(float angle, float deslocY, float deslocZ)
    {
        toogleAnguloCamera.interactable = false; //impede que toque no toggle enquanto a animacao roda
        float timeElapsed = 0;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(angle, 0, 0);

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + new Vector3(0, deslocY, deslocZ);

        while (timeElapsed < lerpDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / lerpDuration);

            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / lerpDuration);

            timeElapsed += Time.deltaTime;
            yield return null;
        }
        //transform.rotation = targetRotation;

        toogleAnguloCamera.interactable = true; //retoma o toggle
    }
}
