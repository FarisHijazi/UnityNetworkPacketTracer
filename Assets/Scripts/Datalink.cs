using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A Link represents a direct DataLink wired/wireless connection between 2 NetworkEntities
/// </summary>
public class Datalink
{
    /// <summary>
    /// A set containing all of the instantiated DataLinks
    /// </summary>
    public static readonly HashSet<Datalink> DatalinksAll = new HashSet<Datalink>();

    private readonly NetworkEntity EntityA, EntityB;

    ///Propagation delay in (seconds)
    public float PropDelay;

    ///the data-rate R in (bits/second) 
    public float TransmissionSpeed;


    /// Creates a one-way DataLink link between 2 network entities and creates and connects sockets as well
    /// Use this to create the network
    /// <param name="entityA"></param>
    /// <param name="entityB"></param>
    /// <param name="propDelay"></param>
    /// <param name="transmissionSpeed"></param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static Datalink CreateDatalink(NetworkEntity entityA, NetworkEntity entityB, float propDelay,
        float transmissionSpeed)
    {
        Datalink existingDatalink = GetLinkBetween(entityA, entityB);
        if (existingDatalink == null)
            return new Datalink(entityA, entityB, propDelay, transmissionSpeed);

        // if a link already exists, just update the values
        existingDatalink.PropDelay = propDelay;
        existingDatalink.TransmissionSpeed = transmissionSpeed;
        return existingDatalink;
    }

    public static Datalink[] CreateFullduplexDatalink(NetworkEntity entityA, NetworkEntity entityB, float propDelay,
        float transmissionSpeed)
    {
        return new[]
        {
            CreateDatalink(entityB, entityA, propDelay, transmissionSpeed),
            CreateDatalink(entityA, entityB, propDelay, transmissionSpeed)
        };
    }

    private Datalink(NetworkEntity entityA, NetworkEntity entityB, float propDelay, float transmissionSpeed)
    {
        this.PropDelay = propDelay;
        this.TransmissionSpeed = transmissionSpeed;

        this.EntityA = entityA;
        this.EntityB = entityB;

        DatalinksAll.Add(this);
    }

    public Vector3 MidPoint
    {
        get { return (EntityA.transform.position + EntityB.transform.position) / 2; }
    }

    public float CalculateTransmissionDelay(float packetLength)
    {
        var result = packetLength / TransmissionSpeed;
        Debug.Log(string.Format("CalculateTransmissionDelay({0}) = {0}/{1} = {2}", packetLength, TransmissionSpeed,
            result));
        return result;
    }

    /**Assuming that all packets have the same size*/
    public static float CalculateTransmissionDelay(Packet[] packets, Datalink[] links)
    {
        // N*L/Rmin + sum(L/Ri) where i=0 up to k-1
        float minTransmissionRate = links.Select(link => link.TransmissionSpeed).Min();
        int packetSize = packets[0].Size;

        if (packets.Length == 1)
        {
            float transDelay = links.Select(link => link.CalculateTransmissionDelay(packetSize)).Sum();
            Debug.Log(
                "Only a single packet, calculating packet trans delay" +
                "\nLink delays: " + string.Join(", ", links.Select(link => link.CalculateTransmissionDelay(packetSize).ToString()).ToArray()) +
                "\ntotal: " + transDelay
            );
            return transDelay;
        }

        var sum = 0d;
        for (int i = 0; i < links.Length - 1; i++)
        {
            sum += packetSize / (double) links[i].TransmissionSpeed;
        }

        var msgTransDelay = packets.Length * packetSize / minTransmissionRate;
        Debug.Log("TransmissionDelay: N*L/Rmin+sum(L/Ri): sum=" + sum + ", d_msgTrans=" + msgTransDelay);
        /*
         * TransmissionDelay: N*L/Rmin+sum(L/Ri): sum=0.0001, d_msgTrans=0.001
         * Should be:                                                    0.012
         */

        return msgTransDelay + (float)sum;
    }

    /// the distance between the 2 networkNodes in Unity units
    public float Distance
    {
        get { return Vector2.Distance(EntityA.transform.position, EntityB.transform.position); }
    }

    /// returns true if the dataLinks connect between the same two networkEntities (Order does NOT matter)
    public bool Equals(Datalink other)
    {
        return other.EntityA == this.EntityA && other.EntityB == this.EntityB ||
               other.EntityB == this.EntityA && other.EntityA == this.EntityB;
    }

    public override string ToString()
    {
        Debug.Assert(EntityA != null);
        Debug.Assert(EntityB != null);
        return string.Format("[\"{0}\"---\"{1}\"]", EntityA.name, EntityB.name);
    }

    /// returns a dataLink that connects between the two networkEntities a and b (Order does NOT matter)
    public static Datalink GetLinkBetween(NetworkEntity a, NetworkEntity b)
    {
        foreach (Datalink datalink in DatalinksAll)
        {
            if (datalink == null)
            {
                Debug.LogWarning("datalink is null!");
                break;
            }

            if (datalink.EntityA == a && datalink.EntityB == b ||
                datalink.EntityA == b && datalink.EntityB == a)
            {
                return datalink;
            }
        }

        return null;
    }

    public static Datalink[] GetLinksInPath(NetworkEntity[] nodes)
    {
        Datalink[] datalinks = new Datalink[nodes.Length - 1];
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            datalinks[i] = Datalink.GetLinkBetween(nodes[i], nodes[i + 1]);
        }

//        Debug.Log("NodepathToDatalinks: " +
//                  string.Join(", ", datalinks.Select(datalink => datalink.ToString()).ToArray())
//        );

        return datalinks;
    }
}