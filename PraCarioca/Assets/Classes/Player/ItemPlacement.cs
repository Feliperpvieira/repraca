using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPlacement : MonoBehaviour
{

    [SerializeField] private Camera PlayerCamera;

    private GameObject CurrentPlacingTower;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(CurrentPlacingTower != null)
        {
            Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(camray, out RaycastHit hitInfo, 100f))
            {
                CurrentPlacingTower.transform.position = hitInfo.point;
            }

            if (Input.GetMouseButtonDown(0))
            {
                CurrentPlacingTower = null;
            }
        }
    }

    public void SetTowerToPlace(GameObject tower)
    {
        CurrentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);
    }
}
