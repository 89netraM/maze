using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public Stream GenerateMazeSVG(uint size, uint entryCount)
	{
		var maze = GenerateMaze(size, entryCount);
		return DrawMazeSVG(maze);
	}

	private ISVGDrawable GenerateMaze(uint size, uint entryCount)
	{
		var maze = new PolarGrid(size);
		var entries = maze.GenerateEntries(entryCount);
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
