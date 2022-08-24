using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class TriGrid : IGraph<Vector2D<int>>, IGraphCreator<TriGrid>, IEnterable<Vector2D<int>>, ISVGDrawable
{
	public static TriGrid Create(uint size) => new(size);

	public IReadOnlyDictionary<Vector2D<int>, TriNode> Map { get; }

	private readonly uint size;

	public bool this[Vector2D<int> a, Vector2D<int> b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	public TriGrid(uint size)
	{
		this.size = size;
		Map = GenerateMap(size);

		static IReadOnlyDictionary<Vector2D<int>, TriNode> GenerateMap(uint size)
		{
			var map = new Dictionary<Vector2D<int>, TriNode>();

			for (int diagonal = 0; diagonal < size; diagonal++)
			{
				int startX = diagonal * 2;
				for (int i = 0; i <= diagonal; i++)
				{
					map[new(startX - i, -i)] = new();
				}
			}

			return map;
		}
	}

	public IEnumerable<Vector2D<int>> Neighbours(Vector2D<int> current) =>
		IsCoordinateEven(current) ?
			EvenNeighbours(current) :
			OddNeighbours(current);

	private IEnumerable<Vector2D<int>> EvenNeighbours(Vector2D<int> current)
	{
		if (Map.ContainsKey(new(current.X - 2, current.Y)))
		{
			yield return new(current.X - 1, current.Y);
		}
		if (Map.ContainsKey(new(current.X + 2, current.Y)))
		{
			yield return new(current.X + 1, current.Y);
		}
		if (Map.ContainsKey(new(current.X - 1, current.Y + 1)))
		{
			yield return new(current.X, current.Y + 1);
		}
	}

	private IEnumerable<Vector2D<int>> OddNeighbours(Vector2D<int> current) =>
		PossibleOddNeighbours(current)
			.Where(Map.ContainsKey);

	private IEnumerable<Vector2D<int>> PossibleOddNeighbours(Vector2D<int> current)
	{
		yield return new(current.X - 1, current.Y);
		yield return new(current.X + 1, current.Y);
		yield return new(current.X, current.Y - 1);
	}

	private ref bool GetWallReference(Vector2D<int> a, Vector2D<int> b)
	{
		if (a == b)
		{
			throw new ArgumentException("Positions are equal.");
		}

		(a, b) = OrderCoordinates(a, b);

		switch (b - a)
		{
			case (-1, 0):
				return ref Map[a].Left;
			case (1, 0):
				return ref Map[a].Right;
			case (0, 1):
				return ref Map[a].Bottom;
			default:
				throw new ArgumentException("Positions are not neighbours.");
		}

		static (Vector2D<int>, Vector2D<int>) OrderCoordinates(Vector2D<int> a, Vector2D<int> b) =>
			IsCoordinateEven(a) ?
				(a, b) :
				(b, a);
	}

	public IEnumerable<Vector2D<int>> GenerateEntries(uint entryCount) =>
		GenerateEntryIndices(entryCount)
			.Select(EntryIndexToCoordinate);

	private IEnumerable<int> GenerateEntryIndices(uint entryCount)
	{
		int edgeNodeCount = (int)size * 3 - 3;
		int spacing = edgeNodeCount / (int)entryCount;
		return Enumerable.Range(0, (int)entryCount).Select(i => i * spacing);
	}

	private Vector2D<int> EntryIndexToCoordinate(int index)
	{
		if (size == 1)
		{
			return new(0, 0);
		}
		if (index < size - 1)
		{
			return new(index, -index);
		}
		index -= (int)size - 1;
		if (index < size - 1)
		{
			return new((int)size - 1 + index, -((int)size - 1 - index));
		}
		index -= (int)size - 1;
		if (index < size - 1)
		{
			return new(((int)size - 1 - index) * 2, 0);
		}
		throw new ArgumentOutOfRangeException();
	}

	public void OpenEntries(IEnumerable<Vector2D<int>> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(Vector2D<int> entry)
		{
			if (entry.X == 0 && entry.Y == 0)
			{
				Map[entry].Left = false;
			}
			else if (entry.Y == 0)
			{
				Map[entry].Bottom = false;
			}
			else if (entry.X - entry.Y == (size - 1) * 2)
			{
				Map[entry].Right = false;
			}
			else if (entry.X + entry.Y == 0)
			{
				Map[entry].Left = false;
			}
		}
	}

	private static bool IsCoordinateEven(Vector2D<int> coord) =>
		(coord.X + coord.Y) % 2 == 0;

	public void DrawSVG(Stream outputStream)
	{
		const float SIDE = 50.0f;
		const float INRADIUS = SIDE * 0.28868f;
		const float CIRCUMRADIUS = SIDE * 0.57735f;
		const float VERTICAL_SPACING = INRADIUS + CIRCUMRADIUS;

		var (min, max) = FindMinMax();

		using var canvas = CreateCanvas(outputStream, min, max);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true, StrokeCap = SKStrokeCap.Round, };

		var canvasOffset = new SKPoint(
			SIDE / 2.0f,
			VERTICAL_SPACING * -min.Y + CIRCUMRADIUS);

		foreach (var kvp in Map)
		{
			DrawTri(kvp.Key, kvp.Value);
		}

		(Vector2D<int> min, Vector2D<int> max) FindMinMax()
		{
			Vector2D<int> min, max;
			min = max = Map.Keys.First();
			foreach (var coordinate in Map.Keys)
			{
				min = min.MinPart(coordinate);
				max = max.MaxPart(coordinate);
			}
			return (min, max);
		}

		static SKCanvas CreateCanvas(Stream outputStream, Vector2D<int> min, Vector2D<int> max) =>
			SKSvgCanvas.Create(
				SKRect.Create(CalculateTotalWidth(min, max), CalculateTotalHeight(min, max)),
				outputStream);

		static float CalculateTotalWidth(Vector2D<int> min, Vector2D<int> max) =>
			(max.X - min.X + 2) / 2.0f * SIDE;

		static float CalculateTotalHeight(Vector2D<int> min, Vector2D<int> max) =>
			(max.Y - min.Y + 1) * VERTICAL_SPACING;

		void DrawTri(Vector2D<int> coord, TriNode node)
		{
			var pixelCenter = TriToPixelCoordinates(coord) + canvasOffset;

			var left = pixelCenter + new SKPoint(-SIDE / 2.0f, INRADIUS);
			var upper = pixelCenter + new SKPoint(0.0f, -CIRCUMRADIUS);
			var right = pixelCenter + new SKPoint(SIDE / 2.0f, INRADIUS);

			using var path = new SKPath();

			path.MoveTo(left);
			path.LineOrMoveTo(node.Left, upper);
			path.LineOrMoveTo(node.Right, right);
			path.LineOrMoveTo(node.Bottom, left);

			canvas.DrawPath(path, paint);
		}

		static SKPoint TriToPixelCoordinates(Vector2D<int> coord) =>
			new(SIDE / 2.0f * coord.X, VERTICAL_SPACING * coord.Y);
	}
}

public class TriNode
{
	public bool Left = true;
	public bool Right = true;
	public bool Bottom = true;
}
