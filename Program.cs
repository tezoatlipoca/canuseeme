
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using System.Security.Policy;




bool cliOK = GlobalConfig.CommandLineParse(args);

var builder = WebApplication.CreateBuilder(args);
if (!cliOK)
{
    DBg.d(LogLevel.Critical, "Command line parsing failed. Exiting.");
    Environment.Exit(1);
}


DBg.d(LogLevel.Information, $"canuseeme:{GlobalConfig.bldVersion}");


builder.WebHost.UseUrls($"http://{GlobalConfig.Bind}:{GlobalConfig.Port}");

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
    StringBuilder sb = new StringBuilder();
    await GlobalStatic.GenerateHTMLHead(sb, "About");
    sb.AppendLine("<p>A simple auto-responding web-endpoint written in C# using ASP.NET Core.</p>");
    await GlobalStatic.GeneratePageFooter(sb);
    return Results.Text(sb.ToString(), "text/html");
});




//redirect pathless requests to to the about page
app.MapGet("/", async (HttpContext httpContext) =>
{
    string fn = "/"; DBg.d(LogLevel.Trace, fn);
    return Results.Redirect("/lb");
});

app.MapGet("/lb", async (HttpContext httpContext) =>
{
    string fn = "/lb"; DBg.d(LogLevel.Trace, fn);

    // Pull elements of a RemoteSiteDto out of the query string
    //string url = httpContext.Request.Query["url"];
    string url = "http://awadwatt.com:80/path1/path2";
    //string url = "8.8.8.8";
    RemoteSiteDto rsd = new RemoteSiteDto();
    string msg = null;
    if(string.IsNullOrEmpty(url))
    {
        msg = "No URL provided.";
        return Results.BadRequest(msg);
    }
    else {
        rsd.url = url;
        DBg.d(LogLevel.Trace, $"url: {url}");
        var parsUrl = url;
        // ok now try and split url into host, port and path components
        // is there an http:// or https:// at the start? 
        if (url.StartsWith("http://")) {
            rsd.HTTP = true;
            parsUrl = url.Substring(7);
        }
        else if (url.StartsWith("https://")) {
            rsd.HTTPS = true;
            parsUrl = url.Substring(8);
        }
        else {
            parsUrl = url;
        }
        // see if there's a port number
        string[] urlParts = parsUrl.Split(":");
        // if two parts, we have host and port[+path]
        if (urlParts.Length == 2)
        {
            rsd.host = urlParts[0];
            // strip off any trailing /
            rsd.host = rsd.host.TrimEnd('/');
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
            rsd.host = urlParts[0];
            // strip off any trailing /
            rsd.host = rsd.host.TrimEnd('/');
            rsd.port = "80";
        }
        else
        {
            msg = $"URL parsing failed: {url}.";
            return Results.BadRequest(msg);
        }
        // see if there's a path - anything after the first / not counting any http:// or https:// (which we took off above)
        
        urlParts = parsUrl.Split("/");
        // path is everything after the first /, if there is one
        if (urlParts.Length > 1)
        {
            rsd.path = "/" + string.Join("/", urlParts.Skip(1));
        }
        else
        {
            rsd.path = "/";
        }

        // now the fun stuff. 
        RemoteSiteController rc = new RemoteSiteController(rsd);
        
        rc.hostLookup();
        rc.hostPing();
        
        await rc.portCheck();
        rc.hostCurl(); 

        // lets serialize rsd to json
        string jsonString = JsonSerializer.Serialize(rsd);


        return Results.Ok(jsonString);

    }

}).AllowAnonymous();




// Mutex to ensure only one of us is running

bool createdNew;
using (var mutex = new Mutex(true, GlobalStatic.applicationName, out createdNew))
{
    if (createdNew)
    {
        app.Run();
    }
    else
    {
        Console.WriteLine("Another instance of the application is already running.");
    }
}







