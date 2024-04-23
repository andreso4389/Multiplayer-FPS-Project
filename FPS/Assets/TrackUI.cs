using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackUI : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform Subject;


    void Update()
    {
        if (Subject)
        {
            transform.position = playerCamera.WorldToScreenPoint(Subject.position);
        }
    }
}
