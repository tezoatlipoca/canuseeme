using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using System.Security.Policy;
using Microsoft.AspNetCore.Http.Json;




bool cliOK = GlobalConfig.CommandLineParse(args);

var builder = WebApplication.CreateBuilder(args);
if (!cliOK)
{
    DBg.d(LogLevel.Critical, "Command line parsing failed. Exiting.");
    Environment.Exit(1);
}


DBg.d(LogLevel.Information, $"canuseeme:{GlobalConfig.bldVersion}");


builder.WebHost.UseUrls($"http://{GlobalConfig.Bind}:{GlobalConfig.Port}");

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(AppJsonContext.Default);
});

var app = builder.Build();
// this configures the middleware to respect the X-Forwarded-For and X-Forwarded-Proto headers
// that are set by any reverse proxy server (nginx, apache, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();







app.MapGet("/about", async (HttpContext httpContext) =>
{
    string fn = "/about"; DBg.d(LogLevel.Trace, fn);

    return Results.Text(GlobalStatic.staticAboutPage, "text/html");
});




//redirect pathless requests to to the about page
app.MapGet("/",  (HttpContext httpContext) =>
{
    string fn = "/"; DBg.d(LogLevel.Trace, fn);
    return Results.Redirect("/about");
});

app.MapGet("/lb", async (HttpContext httpContext) =>
{
    string fn = "/lb"; DBg.d(LogLevel.Trace, fn);

    // Pull elements of a RemoteSiteDto out of the query string
    string url = httpContext.Request.Query["url"];
    //string url = "http://awadwatt.com:443/path1/path2";
    //string portType = "HTTPS";
    string portType = httpContext.Request.Query["portType"];
    //string url = "8.8.8.8";
    bool html = httpContext.Request.Query.ContainsKey("html");
    // if "html" is in the query string convert it to boolean true; false if otherwise
    DBg.d(LogLevel.Trace, $"html output requested: {(html ? "yes" : "no")}");
    
    RemoteSiteDto rsd = new RemoteSiteDto();
    rsd.callerID = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"; // get the requestors IP address
    string msg = null;
    if (string.IsNullOrEmpty(url))
    {
        msg = "No URL provided.";
        return Results.BadRequest(msg);
    }
    else
    {
        rsd.url = url;
        DBg.d(LogLevel.Trace, $"url: {url}");
        var parsUrl = url;
        // ok now try and split url into host, port and path components
        // is there an http:// or https:// at the start? 
        if (url.StartsWith("http://"))
        {
            rsd.HTTP = true;
            parsUrl = url.Substring(7);
        }
        else if (url.StartsWith("https://"))
        {
            rsd.HTTPS = true;
            parsUrl = url.Substring(8);
        }
        else
        {
            parsUrl = url;
        }
        DBg.d(LogLevel.Trace, $"parsUrl - after protocol strip : {parsUrl}");
        // see if there's a port number
        string[] urlParts = parsUrl.Split(":");
        // if two parts, we have host and port[+path]
        if (urlParts.Length == 2)
        {
            DBg.d(LogLevel.Trace, $"urlParts[0]: {urlParts[0]}");
            DBg.d(LogLevel.Trace, $"urlParts[1]: {urlParts[1]}");
            rsd.host = urlParts[0];

            rsd.port = urlParts[1].Split('/')[0]; // strip off anything after the port number
            // if port isn't an integer, we have a problem
            if (!int.TryParse(rsd.port, out int port))
            {
                msg = $"Port parsing failed: {url}.";
                return Results.BadRequest(msg);
            }

        }
        else if (urlParts.Length == 1)
        {
            DBg.d(LogLevel.Trace, $"urlParts[0]: {urlParts[0]}");
            rsd.host = urlParts[0];


            if (rsd.HTTPS)
            {
                rsd.port = "443";
                rsd.portType = "HTTPS";
            }
            else
            {
                rsd.port = "80";
                rsd.portType = "HTTP";
            }

        }
        else
        {
            DBg.d(LogLevel.Trace, $"urlParts[0]: {urlParts[0]}");
            DBg.d(LogLevel.Trace, $"urlParts[1]: {urlParts[1]}");
            DBg.d(LogLevel.Trace, $"urlParts[2]: {urlParts[2]}");
            msg = $"URL parsing failed: {url}.";
            return Results.BadRequest(msg);
        }
        // see if there's a path - anything after the first / not counting any http:// or https:// (which we took off above)

        urlParts = parsUrl.Split("/");
        // path is everything after the first /, if there is one
        if (urlParts.Length > 1)
        {
            for (int i = 0; i < urlParts.Length; i++)
            {
                DBg.d(LogLevel.Trace, $"urlParts[{i}]: {urlParts[i]}");
            }
            rsd.path = "/" + string.Join("/", urlParts.Skip(1));
        }
        else
        {
            rsd.path = "/";
        }
        // in the case there is no port in the URL, we still need to remove the path from the host
        if (rsd.host.Contains("/"))
        {
            string[] hostParts = rsd.host.Split("/");
            rsd.host = hostParts[0];
        }



        DBg.d(LogLevel.Trace, $"rsd.path: {rsd.path}");
        if (string.IsNullOrEmpty(portType))
        {
            portType = "HTTP";
        }
        rsd.portType = portType;
        // now the fun stuff. 
        RemoteSiteController rc = new RemoteSiteController(rsd);

        DBg.d(LogLevel.Trace, $"host: {rsd.host}");
        DBg.d(LogLevel.Trace, $"port: {rsd.port}");
        DBg.d(LogLevel.Trace, $"path: {rsd.path}");


        if (await rc.hostDNSLookup())
        {
            if (await rc.hostPing())
            {
                if (await rc.portCheck())
                {
                    // only do curl if port is http/https
                    if (rsd.portType == "HTTP" || rsd.portType == "HTTPS")
                    {
                        rc.hostCurl();
                    }
                }
            }
        }

        if (!html)
            {
                return Results.Ok(rc.rsd);
            }
            else
            {
                StringBuilder sb = GlobalStatic.HTMLOutput(rc.rsd);
                return Results.Text(sb.ToString(), "text/html");
            }

    }

}).AllowAnonymous();




// Mutex to ensure only one of us is running

bool createdNew;
using (var mutex = new Mutex(true, GlobalStatic.applicationName, out createdNew))
{
    if (createdNew)
    {
        app.UseStatusCodePages(async context =>
        {
            if (context.HttpContext.Response.StatusCode == 404)
            {
                context.HttpContext.Response.ContentType = "text/html";
                var path = context.HttpContext.Request.Path;
                var html = GlobalStatic.Generate404Page(path);
                await context.HttpContext.Response.WriteAsync(html.ToString());
            }
        });

        app.Run();
    }
    else
    {
        Console.WriteLine("Another instance of the application is already running.");
    }
}







