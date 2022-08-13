using Maze.Web.Models;
using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public Stream GenerateMazeSVG(uint size, uint entryCount, MazeKind mazeKind)
	{
		var maze = mazeKind switch
		{
			MazeKind.Polar => GeneratePolarMaze(size, entryCount),
			MazeKind.HexHex => GenerateHexHexMaze(size, entryCount),
			_ => throw new ArgumentException(),
		};
		return DrawMazeSVG(maze);
	}

	private ISVGDrawable GeneratePolarMaze(uint size, uint entryCount)
	{
		var maze = new PolarGrid(size);
		GenerateMaze<PolarGrid, PolarPosition>(maze, entryCount);
		return maze;
	}

	private ISVGDrawable GenerateHexHexMaze(uint size, uint entryCount)
	{
		var maze = new HexHexGrid(size);
		GenerateMaze<HexHexGrid, HexCoordinate>(maze, entryCount);
		return maze;
	}

	private void GenerateMaze<TMaze, TNode>(TMaze maze, uint entryCount) where TMaze : IGraph<TNode>, IEnterable<TNode>, ISVGDrawable
	{
		var entries = maze.GenerateEntries(entryCount);
		maze.DeapthFirstSearch(entries, Random.Shared);
		maze.OpenEntries(entries);
	}

	private Stream DrawMazeSVG(ISVGDrawable svgDrawable)
	{
		var outputStream = new MemoryStream();
		svgDrawable.DrawSVG(outputStream);
		outputStream.Position = 0;
		return outputStream;
	}
}
