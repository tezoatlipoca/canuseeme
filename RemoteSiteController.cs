using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Components.Endpoints;

public class RemoteSiteController
{
    public RemoteSiteDto rsd { get; set; }

    // constructor that takes a RemoteSiteDto
    public RemoteSiteController(RemoteSiteDto foo)
    {
        rsd = foo;
    }


    public bool hostDNSLookup()
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

    public async Task<bool> hostPing()
    {
        string fn = "hostPing"; DBg.d(LogLevel.Trace, fn);
        string msg = null;
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
            msg = $"Ping reply: {reply.Status}";
            DBg.d(LogLevel.Trace, msg);
            rsd.pingResponse = msg;
            if (reply.Status == IPStatus.Success)
            {

                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            DBg.d(LogLevel.Error, e.Message);
            rsd.pingResponse = e.Message;
            return false;
        }
    }

    public async Task<bool> portCheck(int timeout = 1000)
    {
        string fn = "portCheck"; DBg.d(LogLevel.Trace, fn);
        string msg = null;
        // if there aren't any resolved addresses, skip this
        if ((rsd.resolvedIPs == null) || (rsd.resolvedIPs.AddressList.Length == 0))
        {
            msg = "No DNS resolved addresses. Cannot check port.";
            DBg.d(LogLevel.Error, msg);
            rsd.portResponse = msg;
            return false;
        }
        // port should not be null and must be an integer
        if (string.IsNullOrEmpty(rsd.port) || !int.TryParse(rsd.port, out int portInt))
        {
            msg = $"port {rsd.port}  is null or not an integer";
            DBg.d(LogLevel.Error, msg);
            rsd.portResponse = msg;
            
            return false;
        }

        try
        {



            DBg.d(LogLevel.Trace, $"trying host: {rsd.resolvedIPs.AddressList[0]}:{portInt} with PROTOCOL:  {rsd.portType}");
            using (var client = new TcpClient())
            {
                var address = rsd.resolvedIPs.AddressList[0];
                if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    msg = $"Unsupported address family: {address.AddressFamily}";
                    DBg.d(LogLevel.Error, msg);
                    rsd.portResponse = msg;
                    return false;
                }


                var connectTask = client.ConnectAsync(address, portInt);

                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                {
                    DBg.d(LogLevel.Trace, "Connected OK!");
                    await connectTask; // Ensure any exceptions are observed
                    using (var networkStream = client.GetStream())
                    {
                        Stream stream = networkStream;
                        // If the port is 443 (HTTPS), wrap the stream in an SslStream
                        if (portInt == 443)
                        {
                            var sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate));
                            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                            {
                                TargetHost = rsd.host,
                                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                            });
                            stream = sslStream;
                        }
                        using (var reader = new StreamReader(stream))
                        using (var writer = new StreamWriter(stream) { AutoFlush = true })
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
                                    command = $"EHLO {rsd.host}\r\n";
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
                                msg += $"Sent command: {command.Trim()}";
                                DBg.d(LogLevel.Trace, $"Sent command: {command.Trim()}");
                            }

                            // Read response
                            var response = await reader.ReadLineAsync();
                            msg += $"Received response: {response}";
                            DBg.d(LogLevel.Trace, $"Received response: {response}");

                            // Check if the response is a 302 redirect
                            if (!string.IsNullOrEmpty(response) && response.Contains("HTTP/1.1 302 Found"))
                            {
                                // Read the Location header to get the new URI
                                string locationHeader = null;
                                while (!reader.EndOfStream)
                                {
                                    var headerLine = await reader.ReadLineAsync();
                                    if (headerLine.StartsWith("Location:"))
                                    {
                                        locationHeader = headerLine.Substring("Location:".Length).Trim();
                                        break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(locationHeader))
                                {
                                    DBg.d(LogLevel.Trace, $"Redirecting to: {locationHeader}");
                                    // Handle the redirection (e.g., make a new request to the new URI)
                                    // For simplicity, returning the new location
                                    msg += $"HTTP 302 Found. Redirect to: {locationHeader}";
                                }
                                else
                                {
                                    msg += "HTTP 302 Found but no Location header found";
                                    
                                }
                                rsd.portResponse = msg;
            
                                return true;
                            }
                            else if (!string.IsNullOrEmpty(response) && response.Contains("HTTP/1.1 200 OK"))
                            {
                                msg += "HTTP GET request successful";
                                rsd.portResponse = msg;
            
                                return true;
                            }
                            else
                            {
                                DBg.d(LogLevel.Information, msg);
                                rsd.portResponse = msg;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    msg = "Connection timed out.";
                    DBg.d(LogLevel.Error, msg);
                    rsd.portResponse = msg;
            
                    return false; // Timed out
                }
            }
        }
        catch (SocketException ex)
        {
            var fullMessage = $"SOCKET EXCEPTION ----- {ex.Message}\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                fullMessage += $"\nInner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }
            DBg.d(LogLevel.Error, fullMessage);
            rsd.portResponse = fullMessage;
            
            return false;
        }
        catch (IOException ioEx)
        {


            var fullMessage = $"IOException ----- {ioEx.Message}\n{ioEx.StackTrace}";
            if (ioEx.InnerException != null)
            {
                fullMessage += $"\nInner Exception: {ioEx.InnerException.Message}\n{ioEx.InnerException.StackTrace}";
            }
            DBg.d(LogLevel.Error, fullMessage);
            rsd.portResponse = fullMessage;
            
            return false;
        }
        catch (Exception ex)
        {

            var fullMessage = $"Exception ------- {ex.Message}\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                fullMessage += $"\nInner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }
            DBg.d(LogLevel.Error, fullMessage);
            rsd.portResponse = fullMessage;
            
            return false;
        }

    }

    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        string fn = "ValidateServerCertificate"; DBg.d(LogLevel.Trace, fn);
        string msg = null;
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            msg = "Certificate validation passed.";
            DBg.d(LogLevel.Trace, msg);
        }
        else
        {
            msg = $"Certificate validation failed: {sslPolicyErrors}";
            DBg.d(LogLevel.Error, msg);
        }

        // Log certificate details
        msg += $"\nCertificate Subject: {certificate.Subject}";
        DBg.d(LogLevel.Trace, $"Certificate Subject: {certificate.Subject}");
        msg += $"\nCertificate Issuer: {certificate.Issuer}";
        DBg.d(LogLevel.Trace, $"Certificate Issuer: {certificate.Issuer}");
        msg += $"\nCertificate Effective Date: {certificate.GetEffectiveDateString()}";
        DBg.d(LogLevel.Trace, $"Certificate Effective Date: {certificate.GetEffectiveDateString()}");
        msg += $"\nCertificate Expiration Date: {certificate.GetExpirationDateString()}";
        DBg.d(LogLevel.Trace, $"Certificate Expiration Date: {certificate.GetExpirationDateString()}");

        // Log the entire certificate chain
        foreach (var element in chain.ChainElements)
        {
            DBg.d(LogLevel.Trace, $"Chain Element Subject: {element.Certificate.Subject}");
            msg += $"\nChain Element Subject: {element.Certificate.Subject}";
            DBg.d(LogLevel.Trace, $"Chain Element Issuer: {element.Certificate.Issuer}");
            msg += $"\nChain Element Issuer: {element.Certificate.Issuer}";
            foreach (var status in element.ChainElementStatus)
            {
                DBg.d(LogLevel.Trace, $"Chain Element Status: {status.StatusInformation}");
                msg += $"\nChain Element Status: {status.StatusInformation}";
            }
        }
        rsd.certDebug = msg;
        return sslPolicyErrors == SslPolicyErrors.None;
    }

    public bool hostCurl()
    {
        string fn = "hostCurl"; DBg.d(LogLevel.Trace, fn);
        string msg = null; 
        // host should not be null
        if (string.IsNullOrEmpty(rsd.host))
        {
            msg = "Host is null, cannot GET via curl..";
            DBg.d(LogLevel.Error, msg);
            return false;
        }
        // lets try and curl the host
        try
        {

            DBg.d(LogLevel.Trace, $"querying url: {rsd.url}");
            msg = $"querying url: GET {rsd.url}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rsd.url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            DBg.d(LogLevel.Trace, $"response.StatusCode: {response.StatusCode}");
            // get the first 200 characters of the response
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string responseString = reader.ReadToEnd();
                DBg.d(LogLevel.Trace, $"responseString: {responseString.Substring(0, 200)}");
                msg += $"\nresponseString: {responseString.Substring(0, 200)}";
            }
            rsd.curlResponse = msg;
            return true;
        }
        catch (System.Net.WebException e)
        {
            msg += $"\nException: {e.Message} \n";
            msg += $"\nStatusCode: {e.HResult} \n";
            msg += $"\nResponse: {e.Response} \n";
            msg += $"\nInnerException: {e.InnerException} \n";
            rsd.curlResponse = msg;
            DBg.d(LogLevel.Error, msg);
            return false;
        }
        catch (Exception e)
        {
            msg += $"\nException: {e.Message} \n";
            msg += $"\nInnerException: {e.InnerException} \n";
            rsd.curlResponse = msg;
            DBg.d(LogLevel.Error, msg);
            return false;
        }
    }



}