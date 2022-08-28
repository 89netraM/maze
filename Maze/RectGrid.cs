using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class RectGrid : IGraph<Vector2D<uint>>, IGraphCreator<RectGrid>, IEnterable<Vector2D<uint>>, ISVGDrawable
{
	public static RectGrid Create(uint size, Random random) => new(size);

	public IReadOnlyList<IReadOnlyList<RectNode>> Grid { get; }

	public bool this[Vector2D<uint> a, Vector2D<uint> b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	private RectNode this[Vector2D<uint> coordinate] =>
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

	public IEnumerable<Vector2D<uint>> Neighbours(Vector2D<uint> current) =>
		PossibleNeighbours(current)
			.Where(IsOnGrid);

	public static IEnumerable<Vector2D<uint>> PossibleNeighbours(Vector2D<uint> coord)
	{
		if (coord.Y > 0)
		{
			yield return new(coord.X, coord.Y - 1);
		}
		if (coord.X > 0)
		{
			yield return new(coord.X - 1, coord.Y);
		}
		yield return new(coord.X + 1, coord.Y);
		yield return new(coord.X, coord.Y + 1);
	}

	private bool IsOnGrid(Vector2D<uint> coord) =>
		coord.Y < Grid.Count && coord.X < Grid[(int)coord.Y].Count;

	private ref bool GetWallReference(Vector2D<uint> a, Vector2D<uint> b)
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

		static (Vector2D<uint>, Vector2D<uint>) OrderPositions(Vector2D<uint> a, Vector2D<uint> b) =>
			a.Y <= b.Y && a.X <= b.X ?
				(a, b) :
				(b, a);

		ref bool GetVerticalWallReference(Vector2D<uint> a, Vector2D<uint> b)
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

		ref bool GetHorizontalWallReference(Vector2D<uint> a, Vector2D<uint> b)
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

	public IEnumerable<Vector2D<uint>> GenerateEntries(uint entryCount) =>
		GenerateEntryIndices(entryCount)
			.Select(EntryIndexToCoordinate);

	private IEnumerable<uint> GenerateEntryIndices(uint entryCount)
	{
		const int CORNER_EDGE_COUNT = 4;
		int edgeNodeCount = Grid.Count * 2 + Grid[0].Count + Grid[^1].Count - CORNER_EDGE_COUNT;
		int spacing = edgeNodeCount / (int)entryCount;
		return Enumerable.Range(0, (int)entryCount).Select(i => (uint)(i * spacing));
	}

	private Vector2D<uint> EntryIndexToCoordinate(uint index)
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

	public void OpenEntries(IEnumerable<Vector2D<uint>> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(Vector2D<uint> entry)
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
			path.LineOrMoveTo(node.North, northWestCorner + northEastOffset);
			path.LineOrMoveTo(node.East, northWestCorner + southEastOffset);
			path.LineOrMoveTo(node.South, northWestCorner + southWestOffset);
			path.LineOrMoveTo(node.West, northWestCorner);

			canvas.DrawPath(path, paint);
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
