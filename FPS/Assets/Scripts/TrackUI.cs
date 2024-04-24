using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackUI : MonoBehaviour
{
    [SerializeField] public Camera playerCamera;
    [SerializeField] public Transform Subject;
    [SerializeField] public RectTransform mCanvas;
    public RectTransform uiRT;


    void Update()
    {
        if (Subject)
        {

            // convert screen coords
            Vector2 adjustedPosition = playerCamera.WorldToScreenPoint(Subject.position);

            adjustedPosition.x *= mCanvas.rect.width / (float)playerCamera.pixelWidth;
            adjustedPosition.y *= mCanvas.rect.height / (float)playerCamera.pixelHeight;

            // set it
            uiRT.anchoredPosition = adjustedPosition - mCanvas.sizeDelta / 2f;


            //transform.position = playerCamera.WorldToScreenPoint(Subject.position);


        }
    }
}
