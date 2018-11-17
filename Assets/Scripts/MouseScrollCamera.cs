using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class MouseScrollCamera : MonoBehaviour
{
    private Camera m_camera;

    /// the amount to zoom
    [SerializeField] [Range(1, 1000)] private float m_zoomAmount = 1f;

    [SerializeField] private bool m_invert = false;
    [SerializeField] [Range(0, 10)] private float m_followMouseAmount = 1f;
    

    void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // moving the camera toward the mouse
        var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var mouseOffset = (Vector2) Input.mousePosition - screenCenter;


        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Math.Abs(scrollInput) > 0.1f) // if (scroll != 0) 
        {
            float scrollAmount = scrollInput * m_zoomAmount * -(m_invert ? -1 : 1);

            if (m_camera.orthographic)
                m_camera.orthographicSize += scrollAmount;
            else
                m_camera.fieldOfView += scrollAmount;


            transform.position += new Vector3(mouseOffset.x / Screen.width, mouseOffset.y / Screen.height, 0) *
                                  m_followMouseAmount*-Mathf.Sign(scrollAmount);
        }
    }
}