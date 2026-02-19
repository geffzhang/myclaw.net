using MyClaw.MCP;

Console.WriteLine("MyClaw MCP 服务器启动中...");
Console.WriteLine("版本: 1.0.0");
Console.WriteLine("协议: MCP over HTTP/SSE");
Console.WriteLine();

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 2334;
var server = new McpServer(port);

await server.StartAsync();

Console.WriteLine($"服务器监听 http://localhost:{port}");
Console.WriteLine("按 Ctrl+C 停止。");

await Task.Delay(-1);
