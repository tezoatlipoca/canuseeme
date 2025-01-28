
using System.Text.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;




[Serializable]
public class RemoteSiteDto
{
    public string host { get; set; }
    public string port { get; set; }
    public string path { get; set; }

    public string url { get; set; }

    public bool HTTP { get; set; }
    public bool HTTPS { get; set; }

    public string portType { get; set; }


    [JsonIgnore]
    public IPHostEntry resolvedIPs { get; set; }

    public string pingResponse { get; set; }
    public string portResponse { get; set; }
    public string curlResponse { get; set; }

    public string certDebug { get; set; }   


    public RemoteSiteDto()
    {
        host = null;
        port = null;
        path = null;
        url = null;
        HTTP = false;
        HTTPS = false;
        resolvedIPs = null;
        pingResponse = null;
        portResponse = null;
        curlResponse = null;
        certDebug = null;
    }

    protected RemoteSiteDto(SerializationInfo info, StreamingContext context)
    {
        host = info.GetString(nameof(host));
        port = info.GetString(nameof(port));
        path = info.GetString(nameof(path));
        url = info.GetString(nameof(url));
        HTTP = info.GetBoolean(nameof(HTTP));
        HTTPS = info.GetBoolean(nameof(HTTPS));
        pingResponse = info.GetString(nameof(pingResponse));
        portResponse = info.GetString(nameof(portResponse));
        curlResponse = info.GetString(nameof(curlResponse));

        var serializedIPs = info.GetString(nameof(resolvedIPs));
        if (!string.IsNullOrEmpty(serializedIPs))
        {
            resolvedIPs = JsonSerializer.Deserialize<IPHostEntry>(serializedIPs);
        }
        certDebug = info.GetString(nameof(certDebug));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(host), host);
        info.AddValue(nameof(port), port);
        info.AddValue(nameof(path), path);
        info.AddValue(nameof(url), url);
        info.AddValue(nameof(HTTP), HTTP);
        info.AddValue(nameof(HTTPS), HTTPS);
        info.AddValue(nameof(pingResponse), pingResponse);

        info.AddValue(nameof(portType), portType);

        
        if (resolvedIPs != null)
        {
            var serializedIPs = JsonSerializer.Serialize(resolvedIPs);
            info.AddValue(nameof(resolvedIPs), serializedIPs);
        }
        info.AddValue(nameof(portResponse), portResponse);
        info.AddValue(nameof(curlResponse), curlResponse);
        info.AddValue(nameof(certDebug), certDebug);
    }




}