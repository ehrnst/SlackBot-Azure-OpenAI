using Azure.Identity;
using Microsoft.Extensions.Azure;
using SlackNet.AspNetCore;
using SlackNet.Events;
using azopenAiChatApi.Handlers;
using azopenAiChatApi.Models;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration["Azure:KeyVault"]), new DefaultAzureCredential());

// Register secrets as configuration values
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Slack"));
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Azure:OpenAI"));
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Azure"));

builder.Services.AddAzureClients(clientBuilder =>
{
    // Use DefaultAzureCredential by default
    clientBuilder.AddOpenAIClient(new Uri(builder.Configuration["Azure:OpenAI:Endpoint"]));
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

builder.Services.AddSlackNet(c => c
    // Configure the tokens used to authenticate with Slack
    .UseApiToken(builder.Configuration["Slack:ApiToken"]) // This gets used by the API client
    .UseAppLevelToken(builder.Configuration["Slack:AppLevelToken"]) // (Optional) used for socket mode
    
    // Register your Slack handlers here
    .RegisterEventHandler<MessageEvent, AzOpenAIHandler>()
);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UseSlackNet(c => c.UseSocketMode(true));

app.MapControllers();

app.Run();