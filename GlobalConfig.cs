using System.Reflection;


public static class GlobalConfig
{
    // define get and set methods for port, bind, hostname, and hostport
    // the difference between Bind+Port and Hostname is that Bind+Port is the address that the server listens on, 
    //while Hostname is the address (and port) that the server tells clients to connect to
    // to handle reverse proxies, nats etc. redirects blah blah
    // its the latter that gets written to HTML and RSS and Federation elements; i.e. the valid back reference to this instance.
    public static int Port { get; set; }
    public static string? Bind { get; set; }
    public static string? Hostname { get; set; }

    public static bool isSecure { get; set; } = false;

    public static LogLevel CURRENT_LEVEL { get; set; }

    public static string? bldVersion { get; set; }


    public static string? sitecss { get; set; } = null;
    public static string? sitepng { get; set; } = null;



    // parses the command line arguments
    public static bool CommandLineParse(string[] args)
    {
        DBg.d(LogLevel.Debug, "Startup");
        for (int i = 0; i < args.Length; i++)
        {
            DBg.d(LogLevel.Trace, $"Startup command line argument {i} is {args[i]}");
        }

        // arguments are in the form of --key=value
        // we'll split on the = and then switch on the key
        foreach (var arg in args)
        {
            var splitArg = arg.Split('=');
            switch (splitArg[0])
            {
                case "--port":
                    Port = int.Parse(splitArg[1]);

                    break;
                case "--bind":
                    Bind = splitArg[1];

                    break;
                case "--hostname":
                    Hostname = splitArg[1];

                    break;
                case "--runlevel":
                    CURRENT_LEVEL = castRunLevel(splitArg[1]);
                    break;
                case "--sitecss":
                    sitecss = splitArg[1];
                    break;
                case "--sitepng":
                    sitepng = splitArg[1];
                    break;
                case "--help":
                    Console.WriteLine("Usage: ./canuseeme(.exe) -- [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("--port=PORT\t\t\tPort to listen on. Default is 5000");
                    Console.WriteLine("--bind=IP\t\t\tIP address to bind to. Default is *");
                    Console.WriteLine("--hostname=URL\t\t\tURL to use in links. Default is http://localhost");
                    Console.WriteLine("--runlevel=LEVEL\t\t\tLog level. Default is Information");
                    Console.WriteLine("--sitecss=URL\t\t\tURL to the site stylesheet. Default is null");
                    Console.WriteLine("--sitepng=URL\t\t\tURL to the site favicon.ico. Default is null");
                    Environment.Exit(0);
                    break;
                default:
                    DBg.d(LogLevel.Warning, $"Unexpected command line argument: {splitArg[0]}");
                    break;
            }
        }
        if (Port == 0) Port = 5000;
        DBg.d(LogLevel.Information, $"Port: {Port}");
        if (Bind == null) Bind = "*";
        DBg.d(LogLevel.Information, $"Bind: {Bind}");
        if (Hostname == null) Hostname = "http://localhost";

        // parse hostname. if it starts with https://, then we're secure
        if (Hostname.StartsWith("https://"))
        {
            isSecure = true;
        }
        DBg.d(LogLevel.Information, $"Hostname: {Hostname}");

        
        DBg.d(LogLevel.Information, $"Admin page stylesheet: {sitecss}");
        DBg.d(LogLevel.Information, $"Admin page favicon.ico: {sitepng}");
        

        // lastly get the AssemblyInformationalVersion attribute from the assembly and store it in a static variable
        var bldVersionAttribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        // convert it to a string and store it in a static variable
        bldVersion = bldVersionAttribute?.InformationalVersion;

        return true;

    }

    public static LogLevel castRunLevel(string level)
    {
        var returnLevel = LogLevel.None;
        switch (level)
        {
            case "trace":
                returnLevel = LogLevel.Trace;
                break;
            case "debug":
                returnLevel = LogLevel.Debug;
                break;
            case "info":
                returnLevel = LogLevel.Debug;
                break;
            case "warn":
                returnLevel = LogLevel.Warning;
                break;
            case "error":
                returnLevel = LogLevel.Error;
                break;
            case "critical":
                returnLevel = LogLevel.Critical;
                break;
            default:
                DBg.d(LogLevel.Critical, $"Unexpected value for runlevel: {level}");
                break;

        }
        return returnLevel;


    }
}

