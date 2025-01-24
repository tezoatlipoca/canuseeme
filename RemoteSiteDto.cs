
using System.Text.Json;
using System.Net;

public class RemoteSiteDto {
    public string host { get; set; }
    public string port { get; set; }
    public string path { get; set; }

    public string url { get; set; }

    public bool HTTP { get; set; }
    public bool HTTPS { get; set; }

    public IPHostEntry resolvedIPs { get; set; }

    

    

}