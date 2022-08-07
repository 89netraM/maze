using Maze.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Maze.Web.Controllers;

[ApiController]
[Route("")]
public class MazeController : ControllerBase
{
	private readonly MazeService mazeService;

	public MazeController(MazeService mazeService)
	{
		this.mazeService = mazeService ?? throw new ArgumentNullException(nameof(mazeService));
	}

	[HttpGet]
	[Produces("image/svg+xml")]
	public Task Get()
	{
		using var mazeSvgStream = mazeService.GenerateMazeSVG();
		return WriteSVGStream(mazeSvgStream);
	}

	private Task WriteSVGStream(Stream svgStream)
	{
		Response.StatusCode = StatusCodes.Status200OK;
		Response.ContentType = "image/svg+xml";
		Response.ContentLength = svgStream.Length;
		return svgStream.CopyToAsync(Response.Body);
	}
}
