using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

public class RemoteSiteController
{
    RemoteSiteDto rsd { get; set; }

    // constructor that takes a RemoteSiteDto
    public RemoteSiteController(RemoteSiteDto foo)
    {
        rsd = foo;
    }


    public bool hostLookup()
    {

        string fn = "hostLookup"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(rsd.host))
        {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        DBg.d(LogLevel.Trace, $"host: {rsd.host}");
        // lets do a DNS resolve on the hostname
        try
        {
            rsd.resolvedIPs = Dns.GetHostEntry(rsd.host);
            DBg.d(LogLevel.Trace, $"hostEntry.HostName: {rsd.resolvedIPs.HostName}");
            DBg.d(LogLevel.Trace, $"hostEntry.AddressList: {rsd.resolvedIPs.AddressList}");
            // if we have an address list, lets try and get the first one
            if (rsd.resolvedIPs.AddressList.Length > 0)
            {
                IPAddress ip = rsd.resolvedIPs.AddressList[0];
                DBg.d(LogLevel.Trace, $"ip: {ip}");
                return true;
            }
            else
            {
                DBg.d(LogLevel.Error, "hostEntry.AddressList.Length == 0");
                return false;
            }
        }
        catch (Exception e)
        {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }

    }

    public bool hostPing()
    {
        string fn = "hostPing"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(rsd.host))
        {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        // lets try and ping the host
        try
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(rsd.host);
            if (reply.Status == IPStatus.Success)
            {
                DBg.d(LogLevel.Trace, $"reply.Status: {reply.Status}");
                return true;
            }
            else
            {
                DBg.d(LogLevel.Error, $"reply.Status: {reply.Status}");
                return false;
            }
        }
        catch (Exception e)
        {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }
    }

    public async Task<bool> portCheck(int timeout = 1000)
    {
        string fn = "portCheck"; DBg.d(LogLevel.Trace, fn);
        // if there aren't any resolved addresses, skip this
        if ((rsd.resolvedIPs == null) || (rsd.resolvedIPs.AddressList.Length == 0))
        {
            DBg.d(LogLevel.Error, "resolvedIPs is null");
            return false;
        }
        // port should not be null and must be an integer
        if (string.IsNullOrEmpty(rsd.port) || !int.TryParse(rsd.port, out int portInt))
        {
            DBg.d(LogLevel.Error, "port is null or not an integer");
            return false;
        }

        try
        {



            DBg.d(LogLevel.Trace, $"trying host: {rsd.resolvedIPs.AddressList[0]}:{portInt}");
            using (var client = new TcpClient())
            {
                var address = rsd.resolvedIPs.AddressList[0];
                if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    DBg.d(LogLevel.Error, $"Unsupported address family: {address.AddressFamily}");
                    return false;
                }


                var connectTask = client.ConnectAsync(address, portInt);

                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                {
                    DBg.d(LogLevel.Trace, "Connected");
                    await connectTask; // Ensure any exceptions are observed
                    DBg.d(LogLevel.Trace, $"about to return true");
                    return true;
                }
                else
                {
                    DBg.d(LogLevel.Error, "Timed out");
                    return false; // Timed out
                }
            }
        }
        catch (SocketException ex)
        {
            DBg.d(LogLevel.Error, $"SocketException: {ex.Message}");
            DBg.d(LogLevel.Error, $"Stack Trace: {ex.StackTrace}");
            return false;
        }
        catch (Exception ex)
        {
            DBg.d(LogLevel.Error, $"Exception: {ex.Message}");
            DBg.d(LogLevel.Error, $"Stack Trace: {ex.StackTrace}");
            return false;
        }
        catch
        {
            return false;
        }

    }

    public bool hostCurl()
    {
        string fn = "hostCurl"; DBg.d(LogLevel.Trace, fn);
        // host should not be null
        if (string.IsNullOrEmpty(rsd.host))
        {
            DBg.d(LogLevel.Error, "host is null");
            return false;
        }
        // lets try and curl the host
        try
        {

            DBg.d(LogLevel.Trace, $"url: {rsd.url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rsd.url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            DBg.d(LogLevel.Trace, $"response.StatusCode: {response.StatusCode}");
            return true;
        }
        catch (Exception e)
        {
            DBg.d(LogLevel.Error, e.Message);
            return false;
        }
    }



}