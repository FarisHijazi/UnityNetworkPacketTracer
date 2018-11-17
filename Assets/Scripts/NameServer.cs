using System.Collections;
using UnityScript.Steps;

public class NameServer : NetworkEntity
{
    private Hashtable m_entries = new Hashtable();
    
    public void Query()
    {
        
    }

    // resource record class
    public class RR
    {
        public RR()
        {
            
        }
        public static RR Parse(string rr)
        {
            throw new System.NotImplementedException();
        }
    }
}