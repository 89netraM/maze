using Maze.Web.Models;
using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public void GenerateMazeSVG(Stream outputStream, MazeKind mazeKind, MazeConfig config)
	{
		var random = config.Seed is int s ? new Random(s) : Random.Shared;
		var maze = mazeKind switch
		{
			MazeKind.Polar => GenerateMaze<PolarGrid, Vector2D<uint>>(config, random),
			MazeKind.HexHex => GenerateMaze<HexHexGrid, Vector2D<int>>(config, random),
			MazeKind.TriHex => GenerateMaze<TriHexGrid, Vector2D<int>>(config, random),
			MazeKind.Rect => GenerateMaze<RectGrid, Vector2D<uint>>(config, random),
			MazeKind.Tri => GenerateMaze<TriGrid, Vector2D<int>>(config, random),
			MazeKind.Irregular => GenerateMaze<IrregularGrid, IrregularGrid.Square>(config, random),
			_ => throw new ArgumentException(),
		};
		maze.DrawSVG(outputStream);
	}

	private ISVGDrawable GenerateMaze<TMaze, TNode>(MazeConfig config, Random random)
		where TMaze : IGraph<TNode>, IGraphCreator<TMaze>, IEnterable<TNode>, ISVGDrawable
	{
		var maze = TMaze.Create(config.Size, random);
		GenerateMaze<TMaze, TNode>(maze, config, random);
		return maze;
	}

	private void GenerateMaze<TMaze, TNode>(TMaze maze, MazeConfig config, Random random)
		where TMaze : IGraph<TNode>, IEnterable<TNode>
	{
		var entries = maze.GenerateEntries(config.EntryCount);
		maze.DeapthFirstSearch(entries, random);
		maze.OpenEntries(entries);
	}
}
