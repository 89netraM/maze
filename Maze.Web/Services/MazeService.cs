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
		var maze = new PolarGrid(9);
		var entries = maze.GenerateEntries(3);
		maze.DeapthFirstSearch(entries, Random.Shared);
		maze.OpenEntries(entries);
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
