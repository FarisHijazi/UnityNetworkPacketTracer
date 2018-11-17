using System;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class TraverseNodes : MonoBehaviour
{
    public float Speed = 10;

    public NetworkEntity[] PathNodes;

    /// when a node is passed
    public Action OnNodePass;

    private float t = 0,
        z;

    private const float ClosingReachDistance = 0.01f;

    /// <summary>
    /// the current node that the object is chasing 
    /// </summary>
    protected int Current = 0;

    protected bool Sleeping;


    void Start()
    {
        z = transform.position.z;
    }

    private void LateUpdate()
    {
        if (Sleeping)
            return;
        if (PathNodes.Length <= 0)
            return;
        // move
//        transform.position = Vector3.Lerp(
//            transform.position,
//            PathNodes[Current].transform.position - Vector3.forward * z,
//            t * Speed * Time.deltaTime
//        );
        transform.position = Vector3.MoveTowards(
            transform.position,
            PathNodes[Current].transform.position - Vector3.forward * z,
            Speed * Time.deltaTime * 0.1f
        );

        float distance2D = Vector2.Distance(transform.position, PathNodes[Current].transform.position);

        t += Time.deltaTime;

        if (distance2D <= ClosingReachDistance)
        {
            t = 0;
            IncrementTarget();
        }
    }

    private void IncrementTarget()
    {
        if (Current >= PathNodes.Length - 1)
        {
            ReachedEnd();
        }
        else
        {
            Current++;
            PassNode();
            if (OnNodePass != null)
                OnNodePass();
        }
    }

    protected virtual void ReachedEnd()
    {
        Destroy(gameObject);
    }

    protected virtual void PassNode()
    {
    }

    private void OnDrawGizmos()
    {
        // draw a point on each node
        foreach (var node in PathNodes)
            Gizmos.DrawSphere(node.transform.position, radius: 0.1f);

        // connect the dots with lines
        for (int i = 0; i < PathNodes.Length - 1; i++)
            Gizmos.DrawLine(PathNodes[i].transform.position, PathNodes[i + 1].transform.position);
    }
}