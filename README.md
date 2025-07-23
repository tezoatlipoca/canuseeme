# CanUSeeMe
## A self-hosted auto-responder for remote self-testing local endpoints/websites.

When setting up self-hosted programs or developing web services, or troubleshooting firewall or proxy/reverse-proxy things, sometimes its nice to be able to look _outside_ your network and query it from beyond, like how someone else would see it. You can do this now with a webproxy, but that only works for HTTP/S. Even better is if we could do so programmatically - like if our program listens on a port, or at a particular hostname have an external agent try reaching us at that hostname:port but then report back so we know we're reachable -if not we can bail or log warnings appropriately. 

All `CanUSeeMe` does (for now) is expose a single endpoint `/lb` (for `lookback`) which given a URL, breaks it down and tells you: 
* what external IP you're talking from
* what hostname.domain you're asking me to check
* what IP that actually resolves to
* can I ping that ip?
* can I ping that hostname?
* if you gave me a port, can I reach it? if not, can I reach 80 or 443 (HTTPS)? if its a common port like FTP or SSH or SMTP do I get an expected response? 
* if its HTTP/S, what do I get when I `curl` it?
* do I get valid HTTPS certificate?

Basically, enough information for a running network service to be able to determine _itself_ if its exposed to, and reachable from the internet. 
`CanUSeeMe` was written as a goofy little service for FOSS platform maintainers to run so that user instances have somewhere to _phone home_ to; one to help check that you've set your DNS and firewall up so your _thing_ is accessible if you're self-hosting. Future work might be to report instance creation and "up"ness to a "master" instance... or to load share in a P2P manner.  

## Sample Output
I have a sample instance running and used it to query my self-hosted blog: 
```
https://canuseeme.tezoatlipoca.com/lb/?url=https://awadwatt.com/tezoatlipoca
```
which results in:
```
{
  "host": "awadwatt.com",
  "port": "443",
  "path": "/",
  "url": "https://awadwatt.com",
  "http": false,
  "https": true,
  "portType": "HTTPS",
  "pingResponse": "Ping reply: Success",
  "portResponse": "Sent command: GET / HTTP/1.1\r\nHost: awadwatt.com\r\nConnection: closeReceived response: HTTP/1.1 302 FoundHTTP 302 Found. Redirect to: /?landing=1",
  "curlResponse": "querying url: GET https://awadwatt.com\nresponseString: \u003C!DOCTYPE HTML\u003E\n\u003Chtml\u003E\n\t\u003Chead\u003E\n\t\t\n\u003Ctitle\u003Eawadwatt.com\u003C/title\u003E\n\n\u003Cstyle type=\"text/css\"\u003E\nh2 {\n\tfont-weight: normal;\n}\n#pricing.content-container div.form-container #payment-form {\n\tdisplay: block !impor",
  "certDebug": "Certificate validation passed.\nCertificate Subject: CN=awadwatt.com\nCertificate Issuer: CN=E6, O=Let's Encrypt, C=US\nCertificate Effective Date: 2/28/2025 11:17:30 PM\nCertificate Expiration Date: 5/30/2025 12:17:29 AM\nChain Element Subject: CN=awadwatt.com\nChain Element Issuer: CN=E6, O=Let's Encrypt, C=US\nChain Element Subject: CN=E6, O=Let's Encrypt, C=US\nChain Element Issuer: CN=ISRG Root X1, O=Internet Security Research Group, C=US\nChain Element Subject: CN=ISRG Root X1, O=Internet Security Research Group, C=US\nChain Element Issuer: CN=ISRG Root X1, O=Internet Security Research Group, C=US"
}
```
.. which isn't pretty but its functional. This is output that has been pretty-printed by my browser, so that may be adding some unintended relish. 
**If you want nice HTML formatted output** add `&html` to the end of the web request to `canuseeme`... so that last example would be:
```
https://canuseeme.tezoatlipoca.com/lb/?url=https://awadwatt.com/tezoatlipoca&html
```
which gives... [well you can see here](https://canuseeme.tezoatlipoca.com/lb/?url=https://awadwatt.com/tezoatlipoca&html). 

## Command Line
```
Usage: ./canuseeme(.exe) -- [options]
Options:
--port=PORT			  Port to listen on. Default is 5000
--bind=IP			    IP address to bind to. Default is *       
--hostname=URL		URL to use in links. Default is http://localhost 
--runlevel=LEVEL	Log level. Default is Information (Trace -> Debug -> Information -> Warn -> Fatal)
--sitecss=URL			PATH to the site stylesheet. Default is null       
--sitepng=URL			PATH to the site favicon.ico (jpg and png). Default is null 
--help            This
```
## Using: 
The `/lb` endpoint only returns JSON (maybe we'll add an HTML mode later) - Once you have `canuseeme` running and the port is exposed if you try and hit the `/lb` endpoint with no querystring you'll get 
```
"No URL provided."
```
If you give it a valid `url` parameter it will do its best to query that url for the info above. 
If you provide the parameter `html` (e.g. `lb?url=<url>&html`) the results will be encapsulated in some nice HTML for readability in a browser.. instead of the default which is `json`.
If you specify `portType` it will try and use a little intelligence when it queries the port (if one is given as part of the URL), but only so far as what language to speak to that port _with_, like using `EHLO` to talk to an SMTP service. 
If you try and talk FTP to an IMAP service you're going to get weird results (but `canuseeme` will still _try_). Here are the ports and what `canuseeme` tries to interrogate it with:

```
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
```
Note that `canuseeme` doesn't _evaluate_ the response from that port, it just passes it along in the results and closes the connection. (unless its HTTP/s then it does a webget)
The default ports above (e.g. TELNET - 23) were for my reference - `canuseeme` makes no assumption about what port anything is running on - so if you setup your SSH service running on port 6978, you'd use  
`/lb?url=yourdomain:6978&portType=SSH`
Consider the non-HTTP/S port interrogation to be _experimental_ for now. 
Here's how I query my email server: 
```
https://canuseeme.tezoatlipoca.com/lb?url=smtp.awadwatt.com:25&portType=SMTP
```
gets me
```
{
  "host": "smtp.awadwatt.com",
  "port": "25",
  "path": "/",
  "url": "smtp.awadwatt.com:25",
  "http": false,
  "https": false,
  "portType": "SMTP",
  "pingResponse": "Ping reply: Success",
  "portResponse": "Sent command: EHLO smtp.awadwatt.comReceived response: 554 5.5.0 Error: SMTP protocol synchronization",
  "curlResponse": "querying url: GET smtp.awadwatt.com:25\nException: The URI prefix is not recognized. \n\nInnerException:  \n",
  "certDebug": null
}
```
I get _AN_ SMTP error just not the one I was expecting (more work is needed.)

## Future Work

* improve non HTTP port interrogation (thats still experimental)
* for non HTTP ports don't bother doing a webget/curl or the cert.
* rate limit requests by IP address to prevent abuse/spam flooding or use as a ddos relay
* add the ability to require some form of user credentials or some secret key to prevent public use
* HTML form output so the request could be made by a browser, or stuffed into an iframe. 
* webadmin pages?
* /about page? 
* have endpoints to facilitate master/sibling relationships 

## Try it before you buy it
https://canuseeme.tezoatlipoca.com/lb?url=<your url here>&portType=[HTTP|HTTPS|SMTP|FTP|SSH|.. etc]
