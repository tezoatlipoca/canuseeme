
using System.Text.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
public class RemoteSiteDto {
    public string host { get; set; }
    public string port { get; set; }
    public string path { get; set; }

    public string url { get; set; }

    public bool HTTP { get; set; }
    public bool HTTPS { get; set; }

    public IPHostEntry resolvedIPs { get; set; }

    public override string ToString() {
        return JsonSerializer.Serialize(this);
     
    }

    public bool hostLookup() {

        string fn = "hostLookup"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(host)) {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        DBg.d(LogLevel.Trace, $"host: {host}");
        // lets do a DNS resolve on the hostname
        try {
            resolvedIPs = Dns.GetHostEntry(host);
            DBg.d(LogLevel.Trace, $"hostEntry.HostName: {resolvedIPs.HostName}");
            DBg.d(LogLevel.Trace, $"hostEntry.AddressList: {resolvedIPs.AddressList}");
            // if we have an address list, lets try and get the first one
            if (resolvedIPs.AddressList.Length > 0) {
                IPAddress ip = resolvedIPs.AddressList[0];
                DBg.d(LogLevel.Trace, $"ip: {ip}");
                return true;
            }
            else {
                DBg.d(LogLevel.Error, "hostEntry.AddressList.Length == 0");
                return false;
            }
        }
        catch (Exception e) {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }

    }

    public bool hostPing() {
        string fn = "hostPing"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(host)) {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        // lets try and ping the host
        try {
            Ping ping = new Ping();
            PingReply reply = ping.Send(host);
            if (reply.Status == IPStatus.Success) {
                DBg.d(LogLevel.Trace, $"reply.Status: {reply.Status}");
                return true;
            }
            else {
                DBg.d(LogLevel.Error, $"reply.Status: {reply.Status}");
                return false;
            }
        }
        catch (Exception e) {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }
    }

    public bool portCheck(int timeout = 1000) {
        string fn = "portCheck"; DBg.d(LogLevel.Trace, fn);
        // if there aren't any resolved addresses, skip this
        if ((resolvedIPs == null) || (resolvedIPs.AddressList.Length == 0)) {
            DBg.d(LogLevel.Error, "resolvedIPs is null");
            return false;
        }
        // port should not be null and must be an integer
        if (string.IsNullOrEmpty(port) || !int.TryParse(port, out int portInt)) {
            DBg.d(LogLevel.Error, "port is null or not an integer");
            return false;
        }

        try
        {


            IPAddress host = resolvedIPs.AddressList[0];
            DBg.d(LogLevel.Trace, $"trying host: {host}");
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, portInt, null, null);
                DBg.d(LogLevel.Trace, $"result: {result}");
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
                if (!success)
                {
                    return false;
                }

                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    
    }

    public bool hostCurl() {
        string fn = "hostCurl"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(host)) {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        // lets try and curl the host
        try {
            
            DBg.d(LogLevel.Trace, $"url: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            DBg.d(LogLevel.Trace, $"response.StatusCode: {response.StatusCode}");
            return true;
        }
        catch (Exception e) {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }
    }

}