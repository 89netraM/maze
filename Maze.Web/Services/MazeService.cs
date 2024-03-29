﻿using Maze.Web.Models;
using System;
using System.IO;

namespace Maze.Web.Services;

public class MazeService
{
	public void GenerateMazeSVG(Stream outputStream, MazeKind mazeKind, MazeConfig config)
	{
		var maze = mazeKind switch
		{
			MazeKind.Polar => GenerateMaze<PolarGrid, Vector2D<uint>>(config),
			MazeKind.HexHex => GenerateMaze<HexHexGrid, Vector2D<int>>(config),
			MazeKind.TriHex => GenerateMaze<TriHexGrid, Vector2D<int>>(config),
			MazeKind.Rect => GenerateMaze<RectGrid, Vector2D<uint>>(config),
			MazeKind.Tri => GenerateMaze<TriGrid, Vector2D<int>>(config),
			_ => throw new ArgumentException(),
		};
		maze.DrawSVG(outputStream);
	}

	private ISVGDrawable GenerateMaze<TMaze, TNode>(MazeConfig config)
		where TMaze : IGraph<TNode>, IGraphCreator<TMaze>, IEnterable<TNode>, ISVGDrawable
	{
		var maze = TMaze.Create(config.Size);
		GenerateMaze<TMaze, TNode>(maze, config);
		return maze;
	}

	private void GenerateMaze<TMaze, TNode>(TMaze maze, MazeConfig config)
		where TMaze : IGraph<TNode>, IEnterable<TNode>
	{
		var random = config.Seed is int s ? new Random(s) : Random.Shared;
		var entries = maze.GenerateEntries(config.EntryCount);
		maze.DeapthFirstSearch(entries, random);
		maze.OpenEntries(entries);
	}
}
