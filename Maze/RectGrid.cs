using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class RectGrid : IGraph<RectCoordinate>, IGraphCreator<RectGrid>, IEnterable<RectCoordinate>, ISVGDrawable
{
	public static RectGrid Create(uint size) => new(size);

	public IReadOnlyList<IReadOnlyList<RectNode>> Grid { get; }

	public bool this[RectCoordinate a, RectCoordinate b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	private RectNode this[RectCoordinate coordinate] =>
		Grid[(int)coordinate.Y][(int)coordinate.X];

	public RectGrid(uint size)
	{
		Grid = Enumerable.Range(0, (int)size)
			.Select(y => Enumerable.Range(0, (int)size)
				.Select(x => ConstructDefaultNode(x, y))
				.ToArray())
			.ToArray();

		RectNode ConstructDefaultNode(int x, int y)
		{
			if (x == 0 && y == 0)
			{
				return RectNode.WithNorthhWest();
			}
			else if (x == 0)
			{
				return RectNode.WithWest();
			}
			else if (y == 0)
			{
				return RectNode.WithNorth();
			}
			else
			{
				return new RectNode();
			}
		}
	}

	public IEnumerable<RectCoordinate> Neighbours(RectCoordinate current) =>
		current.PossibleNeighbours()
			.Where(IsOnGrid);

	private bool IsOnGrid(RectCoordinate coord) =>
		coord.Y < Grid.Count && coord.X < Grid[(int)coord.Y].Count;

	private ref bool GetWallReference(RectCoordinate a, RectCoordinate b)
	{
		(a, b) = OrderPositions(a, b);

		if (a == b)
		{
			throw new ArgumentException("Positions are equal.");
		}
		else if (a.Y == b.Y)
		{
			return ref GetVerticalWallReference(a, b);
		}
		else if (a.X == b.X)
		{
			return ref GetHorizontalWallReference(a, b);
		}
		else
		{
			throw new ArgumentException("Positions are not neighbours.");
		}

		static (RectCoordinate, RectCoordinate) OrderPositions(RectCoordinate a, RectCoordinate b) =>
			a.Y <= b.Y && a.X <= b.X ?
				(a, b) :
				(b, a);

		ref bool GetVerticalWallReference(RectCoordinate a, RectCoordinate b)
		{
			if (a.X + 1 == b.X)
			{
				return ref this[a].East;
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}

		ref bool GetHorizontalWallReference(RectCoordinate a, RectCoordinate b)
		{
			if (a.Y + 1 == b.Y)
			{
				return ref this[a].South;
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}

	}

	public IEnumerable<RectCoordinate> GenerateEntries(uint entryCount) =>
		GenerateEntryIndices(entryCount)
			.Select(EntryIndexToCoordinate);

	private IEnumerable<uint> GenerateEntryIndices(uint entryCount)
	{
		const int CORNER_EDGE_COUNT = 4;
		int edgeNodeCount = Grid.Count * 2 + Grid[0].Count + Grid[^1].Count - CORNER_EDGE_COUNT;
		int spacing = edgeNodeCount / (int)entryCount;
		return Enumerable.Range(0, (int)entryCount).Select(i => (uint)(i * spacing));
	}

	private RectCoordinate EntryIndexToCoordinate(uint index)
	{
		if (index < Grid[0].Count - 1)
		{
			return new(index, 0);
		}
		index -= (uint)Grid[0].Count - 1;
		if (index < Grid.Count - 1)
		{
			return new((uint)Grid[0].Count - 1, index);
		}
		index -= (uint)Grid.Count - 1;
		if (index < Grid[0].Count)
		{
			return new((uint)Grid[^1].Count - 1 - index, (uint)Grid.Count - 1);
		}
		index -= (uint)Grid[^1].Count - 1;
		if (index < Grid.Count - 1)
		{
			return new(0, (uint)Grid.Count - 1 - index);
		}
		throw new ArgumentOutOfRangeException();
	}

	public void OpenEntries(IEnumerable<RectCoordinate> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(RectCoordinate entry)
		{
			if (entry.Y == 0)
			{
				this[entry].North = false;
			}
			else if (entry.X == Grid[(int)entry.Y].Count - 1)
			{
				this[entry].East = false;
			}
			else if (entry.X == 0)
			{
				this[entry].West = false;
			}
			else if (entry.Y == Grid.Count - 1)
			{
				this[entry].South = false;
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	public void DrawSVG(Stream outputStream)
	{
		const float NODE_SIZE = 50.0f;

		var northEastOffset = new SKPoint(NODE_SIZE, 0.0f);
		var southEastOffset = new SKPoint(NODE_SIZE, NODE_SIZE);
		var southWestOffset = new SKPoint(0.0f, NODE_SIZE);

		var canvasRect = SKRect.Create(Grid.Count * NODE_SIZE, Grid.Count * NODE_SIZE);
		using var canvas = SKSvgCanvas.Create(canvasRect, outputStream);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true, StrokeCap = SKStrokeCap.Round, };

		for (int y = 0; y < Grid.Count; y++)
		{
			for (int x = 0; x < Grid[y].Count; x++)
			{
				DrawNode(x, y);
			}
		}

		void DrawNode(int x, int y)
		{
			var node = Grid[y][x];
			var northWestCorner = new SKPoint(NODE_SIZE * x, NODE_SIZE * y);

			using var path = new SKPath();
			
			path.MoveTo(northWestCorner);
			LineOrMoveTo(node.North, northWestCorner + northEastOffset);
			LineOrMoveTo(node.East, northWestCorner + southEastOffset);
			LineOrMoveTo(node.South, northWestCorner + southWestOffset);
			LineOrMoveTo(node.West, northWestCorner);

			canvas.DrawPath(path, paint);

			void LineOrMoveTo(bool line, SKPoint point)
			{
				if (line)
				{
					path.LineTo(point);
				}
				else
				{
					path.MoveTo(point);
				}
			}
		}
	}
}

public record RectCoordinate(uint X, uint Y)
{
	public IEnumerable<RectCoordinate> PossibleNeighbours()
	{
		if (Y > 0)
		{
			yield return new(X, Y - 1);
		}
		if (X > 0)
		{
			yield return new(X - 1, Y);
		}
		yield return new(X + 1, Y);
		yield return new(X, Y + 1);
	}
}

public class RectNode
{
	public static RectNode WithWest() =>
		new() { West = true };

	public static RectNode WithNorth() =>
		new() { North = true };

	public static RectNode WithNorthhWest() =>
		new() { West = true, North = true };

	public bool North = false;
	public bool East = true;
	public bool West = false;
	public bool South = true;
}
