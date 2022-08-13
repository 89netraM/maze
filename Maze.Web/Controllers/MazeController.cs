using Maze.Web.Models;
using Maze.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
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

	/// <summary>
	/// Generates a random maze.
	/// </summary>
	/// <param name="mazeKind">The shape of the maze.</param>
	/// <param name="size">The size of the maze.</param>
	/// <param name="entryCount">The number of entry points to the maze.</param>
	/// <response code="200">Returns an SVG image of the maze.</response>
	/// <response code="400">The provided input is invalid.</response>
	[HttpGet("{mazeKind?}")]
	[Produces("image/svg+xml")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public Task Get(
		[FromRoute]
			MazeKind mazeKind = MazeKind.Polar,
		[FromQuery, Range(1, int.MaxValue)]
			uint size = 9,
		[FromQuery, Range(1, int.MaxValue)]
			uint entryCount = 3)
	{
		using var mazeSvgStream = mazeService.GenerateMazeSVG(size, entryCount, mazeKind);
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
