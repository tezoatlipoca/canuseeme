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

    public async Task<string> portCheck(int timeout = 1000)
    {
        string fn = "portCheck"; DBg.d(LogLevel.Trace, fn);
        string msg=null;
        // if there aren't any resolved addresses, skip this
        if ((rsd.resolvedIPs == null) || (rsd.resolvedIPs.AddressList.Length == 0))
        {
            msg = "No DNS resolved addresses. Cannot check port.";
            DBg.d(LogLevel.Error, msg);
            return msg;
        }
        // port should not be null and must be an integer
        if (string.IsNullOrEmpty(rsd.port) || !int.TryParse(rsd.port, out int portInt))
        {
            msg = $"port {rsd.port}  is null or not an integer";
            DBg.d(LogLevel.Error, msg);
            return msg;
        }

        try
        {



            DBg.d(LogLevel.Trace, $"trying host: {rsd.resolvedIPs.AddressList[0]}:{portInt}");
            using (var client = new TcpClient())
            {
                var address = rsd.resolvedIPs.AddressList[0];
                if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    msg = $"Unsupported address family: {address.AddressFamily}";
                    DBg.d(LogLevel.Error, msg );
                    return msg;
                }


                var connectTask = client.ConnectAsync(address, portInt);

                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                {
                    DBg.d(LogLevel.Trace, "Connected OK!");
                    await connectTask; // Ensure any exceptions are observed

                    using (var networkStream = client.GetStream())
                    using (var reader = new StreamReader(networkStream))
                    using (var writer = new StreamWriter(networkStream) { AutoFlush = true })
                    {
                        // switch based on "expected" port type and send the appropriate command
                        string command = null;

                        switch (rsd.portType)
                        {
                            case null:
                                break;
                            case "HTTP": // HTTP
                            case "HTTPS": // HTTPS
                                command = $"GET / HTTP/1.1\r\nHost: {rsd.host}\r\nConnection: close\r\n\r\n";
                                break;
                            case "SMTP": // SMTP 25
                                command = "EHLO localhost\r\n";
                                //expectedResponse = "250";
                                break;
                            case "FTP": // FTP 21
                                command = "USER anonymous\r\n";
                                //expectedResponse = "331";
                                break;
                            case "POP3": // POP3 110
                                command = "USER test\r\n";
                                //expectedResponse = "+OK";
                                break;
                            case "IMAP": // IMAP 143
                                command = "a001 LOGIN test test\r\n";
                                //expectedResponse = "a001 OK";
                                break;
                            case "TELNET": // Telnet 23
                                           // No specific command, just establish a connection
                                           //expectedResponse = "Connected to";
                                break;
                            case "SSH": // SSH 22
                                        // No specific command, just establish a connection
                                        //expectedResponse = "SSH-2.0";
                                break;
                            case "MySQL": // MySQL 3306
                                          // No specific command, just establish a connection
                                          //expectedResponse = "\x00\x00\x00\x0a";
                                break;
                            case "Redis": // Redis - 6379
                                command = "PING\r\n";
                                //expectedResponse = "+PONG";
                                break;

                        }

                        if (command != null)
                        {
                            await writer.WriteLineAsync(command);
                            DBg.d(LogLevel.Trace, $"Sent command: {command.Trim()}");
                        }

                        // Read response
                        var response = await reader.ReadLineAsync();
                        msg = $"Received response: {response}";
                        DBg.d(LogLevel.Trace, msg);

                        return msg;

                    }
                }
                else
                    {
                        msg = "Connection timed out.";
                        DBg.d(LogLevel.Error, msg);
                        return msg; // Timed out
                    }
                }
            }
        catch (SocketException ex)
        {
            DBg.d(LogLevel.Error, $"SocketException: {ex.Message}");
            DBg.d(LogLevel.Error, $"Stack Trace: {ex.StackTrace}");
            return ex.Message;
        }
        catch (Exception ex)
        {
            DBg.d(LogLevel.Error, $"Exception: {ex.Message}");
            DBg.d(LogLevel.Error, $"Stack Trace: {ex.StackTrace}");
            return ex.Message;
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