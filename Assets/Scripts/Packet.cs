/**
 * Link packet
 */

using System;
using System.Collections;
using UnityEngine;

public class Packet : TraverseNodes
{
    /// <summary> /// And event that will be called when the packet reaches its destination /// </summary>
    public Action OnDestinationReached;

    ///  called when the packet is sent the first time, only called once
    public Action OnDeployment;

    public string Payload, Headers;
    public int PayloadLength, HeadersLength;
    public const int DefaultSegmentLength = 1000;//942;
    public const int DefaultHeaderLength = 0;//58;

    /**
     * @return returns the total length in bits (including all headers)
     */
    public int Size
    {
        get { return PayloadSize + this.HeadersSize; }
    }

    public int PayloadSize
    {
        get { return PayloadLength == 0 ? GetStringBytes(Payload) : PayloadLength; }
        private set { PayloadLength = value; }
    }

    public int HeadersSize
    {
        get { return Headers.Length == 0 ? HeadersLength : GetStringBytes(Headers); }
        private set { HeadersLength = value; }
    }

    /// is a control packet? (ACK, REQ, ..)
    public bool IsControl
    {
        set { GetComponent<SpriteRenderer>().color = value ? Color.red : Color.yellow; }
    }

    public static Packet[] PacketizeData(string data, int segmentLength, int dataLength = -1,
        int headersLength = Packet.DefaultHeaderLength)
    {
        if (dataLength == -1) dataLength = GetStringBytes(data);
        int numPackets = (int) Math.Ceiling(dataLength / (float) segmentLength);
        if (numPackets <= 0) numPackets = 1;

        bool isControl = false;

        //todo: this is just for debugging, fix later
        if (dataLength == 0)
        {
            data = "[control]";
            isControl = true;
        }
        
        Packet[] packets = new Packet[numPackets];


        if (packets.Length <= 0)
            return packets;

        
        for (int i = 0; i < numPackets - 1; i++)
        {
            packets[i] = Instantiate(Manager.Instance.PacketPrefab).GetComponent<Packet>();
            packets[i].PayloadSize = segmentLength;
            packets[i].Payload = data;
            packets[i].HeadersSize = headersLength;
            packets[i].transform.position = Vector3.up * 10000;
            packets[i].IsControl = isControl;
        }

        int payloadSizeOfLastPacket = dataLength - (numPackets - 1) * segmentLength;

        packets[numPackets - 1] = Instantiate(Manager.Instance.PacketPrefab).GetComponent<Packet>();
        packets[numPackets - 1].PayloadSize = payloadSizeOfLastPacket;
        packets[numPackets - 1].HeadersSize = headersLength;
        packets[numPackets - 1].transform.position = Vector3.up * 10000;
        packets[numPackets - 1].IsControl = isControl;

        return packets;
    }


    protected override void ReachedEnd()
    {
        NetworkEntity lastNode = PathNodes[PathNodes.Length - 1];
        if (lastNode.OnRecievedData != null)
            lastNode.OnRecievedData(this);

        if (OnDestinationReached != null)
            OnDestinationReached();
        Destroy(gameObject);
    }

    /// when the packet passes through a node
    protected override void PassNode()
    {
        base.PassNode();
        Debug.Log("PassNode()");

        // sleep for a period of dTrans
        if (Current != PathNodes.Length - 1)
        {
            Datalink link = Datalink.GetLinkBetween(
                PathNodes[Current],
                PathNodes[Current + 1]
            );
            StartCoroutine(Sleep(link.CalculateTransmissionDelay(this.Size)));
        }

        UpdateSpeed();
    }

    IEnumerator Sleep(float seconds)
    {
        Sleeping = true;
        yield return new WaitForSeconds(seconds);
        Sleeping = false;
    }

    // todo: doesn't work
    /// update movement speed based on the next link
    public void UpdateSpeed()
    {
        if (Current == PathNodes.Length - 1) return;
        Datalink link = Datalink.GetLinkBetween(
            PathNodes[Current],
            PathNodes[Current + 1]
        );

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (link.PropDelay == 0)
            this.Speed = 10000; // max speed
        else
        {
//            Debug.Log(string.Format("Updating speed: {0}. {1}/{2}={3}", link, link.Distance, link.PropDelay,
//                link.Distance / link.PropDelay));
            this.Speed = link.Distance / link.PropDelay;
        }
    }

    void OnGUI()
    {
        Utils.DrawLabel(
            theTransform: transform,
            text: this.Payload.Substring(0, Mathf.Min(Payload.Length, 10000)),
            offset: Vector3.right,
            size: new Vector2(120, 80)
        );
    }

    public static int GetStringBytes(string str)
    {
        return str.Length * 8;
    }
}