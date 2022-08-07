using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public Stream GenerateMazeSVG()
	{
		var maze = GenerateMaze();
		return DrawMazeSVG(maze);
	}

	private ISVGDrawable GenerateMaze()
	{
		var entries = new[] { new PolarPosition(8, 0), new PolarPosition(8, 16), new PolarPosition(8, 32), };
		var maze = new PolarGrid(9);
		MazeGenerators.DeapthFirstSearch(maze, entries, Random.Shared);
		return maze;
	}

	private Stream DrawMazeSVG(ISVGDrawable svgDrawable)
	{
		var outputStream = new MemoryStream();
		svgDrawable.DrawSVG(outputStream);
		outputStream.Position = 0;
		return outputStream;
	}
}
