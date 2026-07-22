using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tabic.Core.Models;
using Tabic.Mcp;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<TimelineData>();

builder.Services
    .AddMcpServer()
    .WithTools<TabTools>()
    .WithStdioServerTransport();

await builder.Build().RunAsync();