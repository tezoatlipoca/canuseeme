using System.Text.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;




[Serializable]
public class RemoteSiteDto
{
    public string callerID { get; set; } // REQUESTORS IP ADDRESS
    public string host { get; set; }
    public string port { get; set; }
    public string path { get; set; }

    public string url { get; set; }

    public bool HTTP { get; set; }
    public bool HTTPS { get; set; }

    public string portType { get; set; }



    public string[] ipAddresses { get; set; }
    public ExceptionInfoDto ExceptionInfo { get; set; }

    public string pingResponse { get; set; }
    public string portResponse { get; set; }
    public string curlResponse { get; set; }

    public string certDebug { get; set; }


    public RemoteSiteDto()
    {
        host = null;
        port = null;
        portType = "HTTP"; // default to HTTP
        path = null;
        url = null;
        HTTP = false;
        HTTPS = false;
        ipAddresses = null;
        ExceptionInfo = null;
        pingResponse = null;
        portResponse = null;
        curlResponse = null;
        certDebug = null;
        callerID = null; // this is the requestors IP address
    }


    public void resolveIPs(IPHostEntry entry)
    {
        if (entry == null) return;
        // copy the addresses out of the entry and into this.ipAddresses[]
        ipAddresses = entry.AddressList?.Select(ip => ip.ToString()).ToArray();
        if (ipAddresses == null || ipAddresses.Length == 0)
        {
            ipAddresses = new string[] { "No IP addresses resolved." };
        }
    }

}