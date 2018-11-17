using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// does stuff like setup the labels and draw lines between nodes
[ExecuteInEditMode]
public class EditorScript : MonoBehaviour
{
    void Update()
    {
        foreach (NetworkEntity networkEntity in GameObject.FindObjectsOfType<NetworkEntity>())
        {
            // draw debug lines for nodes
            foreach (NetworkEntity node in networkEntity.ConnectedNodes)
            {
                Debug.DrawLine(networkEntity.transform.position, node.transform.position);
            }
        }
    }
}