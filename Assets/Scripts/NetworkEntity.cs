using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

public class NetworkEntity : MonoBehaviour
{
    public IPAddress IpAddress = IPAddress.Any;
    public NetworkEntity[] ConnectedNodes = new NetworkEntity[1];
    public Action<Packet> OnRecievedData;


    private Dictionary<NetworkEntity, NetworkEntity[]> m_possibleConnections =
        new Dictionary<NetworkEntity, NetworkEntity[]>();


    void Start()
    {
        // instantiate a DataLink for each connection
        foreach (NetworkEntity connectedNode in ConnectedNodes)
        {
            AddConnection(connectedNode);
            Utils.DrawLine(transform.position, connectedNode.transform.position, Color.black);
        }
    }

    /// will packetize the data then send the resulting packets
    public Packet[] SendData(int dataLength, NetworkEntity[] pathNodes, string data = "", Action callback = null,
        int segmentSize = Packet.DefaultSegmentLength)
    {
        if (dataLength == -1)
            dataLength = Packet.GetStringBytes(data);

        Packet[] packets = Packet.PacketizeData(data: data, segmentLength: segmentSize, dataLength: dataLength);
        Datalink[] links = Datalink.GetLinksInPath(pathNodes);

        packets = packets.Where(packet => packet != null).ToArray();

        if (packets.Length == 0)
        {
            Debug.LogError("Packets.Length = 0!!!!");
            return null;
        }

        // print end2EndDelay
        float tripPropDelay = links.Select(link => link.PropDelay).Sum(); // sum prop delays
        var tripTransDelay = Datalink.CalculateTransmissionDelay(packets, Datalink.GetLinksInPath(pathNodes));

        var end2EndDelay = tripPropDelay + tripTransDelay;
        Manager.Echo(string.Format("The end-to-end delay of the message:    {0} + {1} = {2,30}s",
            tripPropDelay, tripTransDelay, end2EndDelay));
        Manager.accumulatedDelay += end2EndDelay;

        // set the deployment callback of each packet to call the next packet
        // (this makes them deploy sequentially with a delay between them rather than all together)
        for (int i = 0; i < packets.Length - 1; i++)
        {
            Packet nextPacket = packets[i + 1];

            packets[i].OnDeployment = delegate
            {
                float dTrans = links[0].CalculateTransmissionDelay(nextPacket.Size);
                StartCoroutine(SendPacketLater(nextPacket, pathNodes, null, dTrans * 2));
            };
        }

        // set the callback
        packets.Last().OnDestinationReached = callback;

        // send the first packet with a delay
        float dTransFirstPaket = links[0].CalculateTransmissionDelay(packets[0].Size);
        StartCoroutine(SendPacketLater(packets[0], pathNodes, null, dTransFirstPaket * 2));

        return packets;
    }

    public void EstablishTcp(NetworkEntity[] pathNodes, Action callback = null)
    {
        Packet[] reqPackets = SendData(0, pathNodes, "TCP req", delegate
        {
            NetworkEntity[] respPath = pathNodes.Reverse().ToArray();
            Packet[] respPackets = respPath[0].SendData(0, respPath, "TCP resp", callback);
            respPackets[0].IsControl = true;
        });
        reqPackets[0].IsControl = true;
    }

    
    private void AddConnection(NetworkEntity connectedNode)
    {
        Datalink.CreateFullduplexDatalink(entityA: this, entityB: connectedNode, propDelay: 0.05f,
            transmissionSpeed: 10E6f);
    }

    /// this is what makes the distance between the packets in the animation
    private IEnumerator SendPacketLater(Packet packet, NetworkEntity[] nodePath, Action callback, float delay)
    {
        yield return new WaitForSeconds(delay);
        SendPacket(packet, nodePath, callback);
    }

    private void SendPacket(Packet packet, NetworkEntity[] pathNodes, Action callback = null)
    {
//        string[] delayStrings = new string[pathNodes.Length - 1];
//        float[] delays = new float[pathNodes.Length - 1];
//        Datalink[] links = Datalink.GetLinksInPath(pathNodes);
//
//        // printing the delay of links
//        for (int i = 0; i < links.Length; i++)
//        {
//            if (links[i] == null) continue;
//            // calculate delay
//            float transDelay = links[i].CalculateTransmissionDelay(packet.Size);
//            delays[i] = transDelay + links[i].PropDelay;
//
//            // save delay string to print later
//            delayStrings[i] = string.Format("Delay for packet {0}bits = {1}s + {2}s = {3}sec over {4}",
//                packet.Size, transDelay, links[i].PropDelay, delays[i], links[i]
//            );
//        }

        
        // make sure that packet starts here
        packet.transform.position = this.transform.position;
        packet.PathNodes = pathNodes;
        packet.UpdateSpeed();
        if (packet.OnDeployment != null)
            packet.OnDeployment();
        if (callback != null)
            packet.OnDestinationReached = callback;
        packet.enabled = true;
    }

    void OnGUI()
    {
        Utils.DrawLabel(this.transform, this.name);
    }
}