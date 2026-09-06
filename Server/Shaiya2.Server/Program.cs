using Shaiya2.Server.Networking;

Console.Title = "Shaiya2 Server";

Console.WriteLine("========================================");
Console.WriteLine("            SHAIYA2 SERVER");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Server version: 0.0.1");
Console.WriteLine("Environment: Development");
Console.WriteLine();

var server = new GameServer();

if (!server.Start(7777))
{
    Console.WriteLine("Unable to start server.");
    return;
}

Console.WriteLine();
Console.WriteLine("Waiting for connections...");
Console.WriteLine();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;

    server.Stop();

    Environment.Exit(0);
};

server.Run();