using MyClaw.MCP;

Console.WriteLine("MyClaw MCP Server Starting...");
Console.WriteLine("Version: 1.0.0");
Console.WriteLine("Protocol: MCP over HTTP/SSE");
Console.WriteLine();

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 2334;
var server = new McpServer(port);

await server.StartAsync();

Console.WriteLine($"Server listening on http://localhost:{port}");
Console.WriteLine("Press Ctrl+C to stop.");

await Task.Delay(-1);
