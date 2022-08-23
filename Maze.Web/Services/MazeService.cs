using Maze.Web.Models;
using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public void GenerateMazeSVG(Stream outputStream, uint size, uint entryCount, MazeKind mazeKind, int? seed)
	{
		var maze = mazeKind switch
		{
			MazeKind.Polar => GeneratePolarMaze(size, entryCount, seed),
			MazeKind.HexHex => GenerateHexHexMaze(size, entryCount, seed),
			MazeKind.Rect => GenerateRectMaze(size, entryCount, seed),
			_ => throw new ArgumentException(),
		};
		maze.DrawSVG(outputStream);
	}

	private ISVGDrawable GeneratePolarMaze(uint size, uint entryCount, int? seed)
	{
		var maze = new PolarGrid(size);
		GenerateMaze<PolarGrid, PolarPosition>(maze, entryCount, seed);
		return maze;
	}

	private ISVGDrawable GenerateHexHexMaze(uint size, uint entryCount, int? seed)
	{
		var maze = new HexHexGrid(size);
		GenerateMaze<HexHexGrid, HexCoordinate>(maze, entryCount, seed);
		return maze;
	}

	private ISVGDrawable GenerateRectMaze(uint size, uint entryCount, int? seed)
	{
		var maze = new RectGrid(size);
		GenerateMaze<RectGrid, RectCoordinate>(maze, entryCount, seed);
		return maze;
	}

	private void GenerateMaze<TMaze, TNode>(TMaze maze, uint entryCount, int? seed) where TMaze : IGraph<TNode>, IEnterable<TNode>, ISVGDrawable
	{
		var random = seed is int s ? new Random(s) : Random.Shared;
		var entries = maze.GenerateEntries(entryCount);
		maze.DeapthFirstSearch(entries, random);
		maze.OpenEntries(entries);
	}
}
