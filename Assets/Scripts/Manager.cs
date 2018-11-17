using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable InconsistentNaming

public class Manager : MonoBehaviour
{
    [SerializeField] public GameObject LabelPrefab;
    [SerializeField] public GameObject PacketPrefab;
    public static string ConsoleString = "output:";

    public static NetworkEntity
        user,
        localNs,
        tld,
        authNs,
        yahooWs,
        yahooMs,
        hostAMs,
        hostA;

    // GUI input 
    private static bool iterativeDnsSearch = false;
    private static int emailSize = 10000;

    /// which DNS has the WS 
    private static NetworkEntity nsWsIpIsCachedOn;

    public static float accumulatedDelay = 0;


    // GUI input
    private const string emailConent = @"In the first age, in the first battle, when the shadows first lengthened, one stood.
Burned by the embers of Armageddon, his soul blistered by the fires of Hell and tainted beyond ascension, he chose the path of perpetual torment.
In his ravenous hatred he found no peace;
and with boiling blood he scoured the Umbral Plains seeking vengeance against the dark lords who had wronged him.
He wore the crown of the Night Sentinels, and those that tasted the bite of his sword named him... the Doom Slayer.";


    public static Manager Instance
    {
        get { return FindObjectOfType<Manager>(); }
    }

    public static void Echo(string msg)
    {
        print(msg);
        ConsoleString = msg + "\n" + ConsoleString;
        const int MaxStringSize = 10000;
        if (ConsoleString.Length > MaxStringSize)
        {
            ConsoleString = ConsoleString.Substring(0, MaxStringSize - 1);
        }
    }

    struct Paths
    {
        public static readonly NetworkEntity[]
            user2tld = {user, localNs, yahooWs, tld},
            //    1)
            user2localNs = {user, localNs},
            localNs2user = {localNs, user},

            //    2)
            user2yahooWs = {user, localNs, yahooWs},
            yahooWs2user = {yahooWs, localNs, user},

            //    4)
            yahooWs2yahooMs = {yahooWs, yahooMs},
            yahooMs2yahooWs = {yahooMs, yahooWs},

            //6) Iterative:
            yahooMs2tld = {yahooMs, yahooWs, tld},
            tld2yahooMs = {tld, yahooWs, yahooMs},
            yahooMs2authNs = {yahooMs, yahooWs, authNs},
            authNs2yahooMs = {authNs, yahooWs, yahooMs},

            //Recursive:
            tld2authNs = {tld, authNs},
            authNs2tld = tld2authNs.Reverse().ToArray(),

            //8)
            yahooMs2hostAMs = {yahooMs, hostAMs},

            //10)
            hostAMs2hostA = {hostAMs, hostA},
            hostA2hostAMs = {hostA, hostAMs};
    };

    void Awake()
    {
        // this sets up all the connections in the network
        user = GameObject.Find("User").GetComponent<NetworkEntity>();
        localNs = GameObject.Find("Local NS").GetComponent<NetworkEntity>();
        tld = GameObject.Find("TLD").GetComponent<NetworkEntity>();
        authNs = GameObject.Find("Auth NS").GetComponent<NetworkEntity>();
        yahooWs = GameObject.Find("Yahoo WS").GetComponent<NetworkEntity>();
        yahooMs = GameObject.Find("Yahoo MS").GetComponent<NetworkEntity>();
        hostAMs = GameObject.Find("Host-A MS").GetComponent<NetworkEntity>();
        hostA = GameObject.Find("Host-A").GetComponent<NetworkEntity>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private static void COE344M1()
    {
        accumulatedDelay = 0;
//        Assumptions: 
//        Local DNS server has the IP address of the web server
//            TLD server doesn't have the IP address of the receiver IP address but has the IP address for the authoritative server

//            Store and forward formula: (Check)
//            Total delay = N(Ttran + Tprop) + ( Packets - 1)*Ttran

//                              (DNS can be iterative or recursive)
//        (Different propagation delays and transmission rates)
//        (Different email sizes)


//            Considered delay is only propagation delay 
        user.SendData(0, Paths.user2localNs, "", delegate
        {
            localNs.SendData(0, Paths.localNs2user, "", delegate
            {
                Echo("1) DNS lookup for web server address");
                //        2) TCP establishment with web server for HTTP
                //            Considered delay is only propagation delay 
                user.EstablishTcp(Paths.user2yahooWs, delegate
                {
                    //        3) Sending email to web server using HTTP (packets)
                    //        Considered delay: propagation delay + transmission rate, use store and forward
                    Echo("2) TCP establishment with web server for HTTP");
                    user.SendData(emailSize, Paths.user2yahooWs, emailConent, delegate
                    {
                        Echo("3 Email reached web server");
                        //        4) SMTP TCP establishment between web server and mail server Considered delay is only propagation delay 
                        yahooWs.EstablishTcp(Paths.yahooWs2yahooMs, delegate
                        {
                            Echo("4) ws and ms TCP established");
                            yahooWs.SendData(emailSize, Paths.yahooWs2yahooMs, emailConent, RequestFromDNS);
                        });
                    });
                });
            });
        });
//        6) DNS lookup for receiver IP address by mail server (Can be iterative or recursive)
//        Considered delay is only propagation delay 
//            (Assumption: IP address is stored in Authoritative server, so go from mail server to TLD, receive 2 RR of type: NS and A for Authoritative, then go back to mail server, and go again to Authoritative server, receive 1 (or 2): A (and MX) 

//        7) TCP establishment between sender mail server and receiver mail server for SMTP
//            Considered delay is only propagation delay 

//        8) Push email from send to receiver as a single message
//        Considered delay: propagation delay + transmission rate

//        9) Receiver establishes TCP connection with its mail server for HTTP
//            Considered delay is only propagation delay 

//        10) User requests the message and receives it (packets)
//        Considered delay: propagation delay + transmission rate
    }
    private static void RequestFromDNS()
    {
//        5) Push email from web server to mail server using SMTP (Single message)
//        Considered delay: propagation delay + transmission rate
        Echo("Email reached mail server");
        yahooMs.SendData(0, Paths.yahooMs2tld, "I want the IP address of the mail server", delegate
        {
            if (iterativeDnsSearch) // iterative search
            {
                tld.SendData(0, Paths.tld2yahooMs,
                    "RR(" + authNs.name + ", NS)\nRR(" + authNs.IpAddress + ", A)", delegate
                    {
                        yahooMs.SendData(0, Paths.yahooMs2authNs, "RR(,, A)", delegate
                        {
                            authNs.SendData(0, Paths.authNs2yahooMs,
                                "RR(MX)\nRR(" + hostAMs.IpAddress + ", A)", SendMail);
                        });
                    });
            }
            else // recursive search
            {
                tld.SendData(0, Paths.tld2authNs,
                    string.Format("RR({0}, NS)", authNs.IpAddress), delegate
                    {
                        authNs.SendData(0, Paths.authNs2tld,
                            "RR(" + hostAMs.IpAddress + ", A)", delegate
                            {
                                tld.SendData(0, Paths.tld2yahooMs, "RR(, , MX)\nRR(" + hostAMs.IpAddress + ")",
                                    SendMail);
                            });
                    });
            }
        });
    }
    private static void SendMail()
    {
        Echo("Yahoo mail server has the IP address");

        yahooMs.EstablishTcp(Paths.yahooMs2hostAMs, delegate
        {
            yahooMs.SendData(emailSize, Paths.yahooMs2hostAMs, emailConent, delegate
            {
                Echo("HostA mail server recieved the email");
                hostA.EstablishTcp(Paths.hostA2hostAMs, delegate
                {
                    hostA.SendData(0, Paths.hostA2hostAMs, "HTTP: I want my email", delegate
                    {
                        hostAMs.SendData(emailSize,
                            Paths.hostAMs2hostA, emailConent, delegate
                            {
                                Echo("Host A has received the email!!!!");
                                Echo(string.Format("Delay of operation: {0}seconds", accumulatedDelay));
                            });
                    });
                });
            });
        });
    }


    private void OnGUI()
    {
//        if (GUI.Button(new Rect(x: 40, y: 40, width: 80, height: 40), "SendNudes"))
//        {
//            yahooMs.SendData(1000, Paths.yahooMs2hostAMs, "", delegate { print("the entire message was received"); });
//        }
        GUI.TextField(new Rect(x: 40, y: 40, width: 80, height: 40), "Accumulated delay:    " + accumulatedDelay + "s",
            new GUIStyle());

        if (GUI.Button(new Rect(x: 40, y: 80, width: 80, height: 40), "Go!"))
        {
            COE344M1();
        }

//        emailConent = GUI.TextArea(new Rect(x: 40, y: 120, width: 120, height: 80), emailConent);
        try
        {
            emailSize = int.Parse(GUI.TextArea(new Rect(x: 40, y: 120, width: 120, height: 80), emailSize.ToString()));
        }
        catch (Exception)
        {
            // ignored
        }

        iterativeDnsSearch = GUI.Toggle(new Rect(x: 40, y: 200, width: 200, height: 20), iterativeDnsSearch,
            "Iterative DNS search");
        // draw console
        const int height = 160;
        ConsoleString = GUI.TextArea(new Rect(0, Screen.height - height, Screen.width, height), ConsoleString);


        // draw the d_prop/DataRate textfields, also update values uppon editing 
        foreach (Datalink dl in Datalink.DatalinksAll)
        {
            Vector2 size = new Vector2(100, 40);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(dl.MidPoint);
            Vector2 guiPosition = new Vector2(screenPos.x - size.x, Screen.height - screenPos.y);

            string delayText = string.Format("{0}s/{1}bps", dl.PropDelay, dl.TransmissionSpeed);
            string inputDelayText = GUI.TextField(new Rect(guiPosition, size), delayText);

            if (!inputDelayText.Equals(delayText))
            {
                try
                {
                    string[] split = inputDelayText.Split('/');
                    Regex rx = new Regex(@"[\deE\+.\\-]+", RegexOptions.Multiline);

                    float parseResult;

                    string matchValue1 = rx.Match(split[0]).Groups[0].Value;
                    parseResult = float.TryParse(matchValue1, out parseResult) ? parseResult : dl.PropDelay;
                    dl.PropDelay = parseResult;

                    string matchValue2 = rx.Match(split[1]).Groups[0].Value;
                    parseResult = float.TryParse(matchValue2, out parseResult) ? parseResult : dl.TransmissionSpeed;
                    dl.TransmissionSpeed = parseResult;
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception: " + e);
                }
            }
        }
    }
}