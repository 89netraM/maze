using Maze.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MazeService>();

builder.Services.Configure<KestrelServerOptions>(ConfigureKestrel);
builder.Services.AddControllers()
	.AddJsonOptions(JsonOptionsSetup);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerGenSetup);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

static void ConfigureKestrel(KestrelServerOptions options)
{
	options.AllowSynchronousIO = true;
}

static void JsonOptionsSetup(JsonOptions options)
{
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}

static void SwaggerGenSetup(SwaggerGenOptions options)
{
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
}
