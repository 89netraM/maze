using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public abstract class HexGrid : IGraph<Vector2D<int>>, ISVGDrawable
{
	private delegate void HexEdgeVisitor(ref bool a, ref bool b);

	protected static IReadOnlyList<Vector2D<int>> Directions { get; } = new Vector2D<int>[]
	{
		new(1, 0),
		new(1, -1),
		new(0, -1),
		new(-1, 0),
		new(-1, 1),
		new(0, 1),
	};

	public IReadOnlyDictionary<Vector2D<int>, HexNode> Map { get; }

	public bool this[Vector2D<int> a, Vector2D<int> b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	protected HexGrid(IReadOnlyDictionary<Vector2D<int>, HexNode> map) =>
		(Map) = (map);

	public IEnumerable<Vector2D<int>> Neighbours(Vector2D<int> current) =>
		Directions
			.Select(direction => current + direction)
			.Where(Map.ContainsKey);

	private ref bool GetWallReference(Vector2D<int> a, Vector2D<int> b)
	{
		if (a == b)
		{
			throw new ArgumentException("Coordinates are equal.");
		}

		(a, b) = OrderCoordinates(a, b);

		switch (b - a)
		{
			case (1, 0):
				return ref Map[a].East;
			case (1, -1):
				return ref Map[a].NorthEast;
			case (0, -1):
				return ref Map[a].NorthWest;
			default:
				throw new ArgumentException("Positions are not neighbours.");
		}

		static (Vector2D<int>, Vector2D<int>) OrderCoordinates(Vector2D<int> a, Vector2D<int> b)
		{
			if (a.X < b.X || (a.X == b.X && a.Y > b.Y))
			{
				return (a, b);
			}
			else
			{
				return (b, a);
			}
		}
	}

	public void DrawSVG(Stream outputStream)
	{
		const float SIZE = 30.0f;
		const float HORIZONTAL_SPACING = SIZE * 0.866025f;
		const float VERTICAL_SPACING = SIZE * 1.5f;

		var (min, max) = FindMinMax();

		using var canvas = CreateCanvas(outputStream, min, max);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true, StrokeCap = SKStrokeCap.Round, };

		var canvasOffset = new SKPoint(
			HORIZONTAL_SPACING + 2.0f * HORIZONTAL_SPACING * -min.X,
			VERTICAL_SPACING * -min.Y);

		foreach (var kvp in Map)
		{
			DrawHex(kvp.Key, kvp.Value);
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
			(max.X - min.X) * HORIZONTAL_SPACING * 2.0f + HORIZONTAL_SPACING * 2.0f;

		static float CalculateTotalHeight(Vector2D<int> min, Vector2D<int> max) =>
			(max.Y - min.Y) * VERTICAL_SPACING + SIZE * 2.0f;

		void DrawHex(Vector2D<int> coord, HexNode node)
		{
			var hexPixelOffset = new SKPoint(0.0f, SIZE);
			var pixelCenter = HexToPixelCoordinates(coord) + hexPixelOffset + canvasOffset;

			var northWest = pixelCenter + new SKPoint(-HORIZONTAL_SPACING, -SIZE * 0.5f);
			var north = pixelCenter + new SKPoint(0.0f, -SIZE);
			var northEast = pixelCenter + new SKPoint(HORIZONTAL_SPACING, -SIZE * 0.5f);
			var southEast = pixelCenter + new SKPoint(HORIZONTAL_SPACING, SIZE * 0.5f);
			var south = pixelCenter + new SKPoint(0.0f, SIZE);
			var southWest = pixelCenter + new SKPoint(-HORIZONTAL_SPACING, SIZE * 0.5f);

			using var path = new SKPath();

			path.MoveTo(northWest);
			path.LineOrMoveTo(node.NorthWest, north);
			path.LineOrMoveTo(node.NorthEast, northEast);
			path.LineOrMoveTo(node.East, southEast);
			path.LineOrMoveTo(node.SouthEast, south);
			path.LineOrMoveTo(node.SouthWest, southWest);
			path.LineOrMoveTo(node.West, northWest);

			canvas.DrawPath(path, paint);
		}

		static SKPoint HexToPixelCoordinates(Vector2D<int> coord) =>
			new(2.0f * HORIZONTAL_SPACING * coord.X + HORIZONTAL_SPACING * coord.Y, VERTICAL_SPACING * coord.Y);
	}
}

public class HexNode
{
	public static HexNode WithSouthEast() =>
		new() { SouthEast = true };

	public static HexNode WithWest() =>
		new() { West = true };

	public static HexNode WithSouthEastSouthWest() =>
		new() { SouthEast = true, SouthWest = true };

	public static HexNode WithSouthWestWest() =>
		new() { SouthWest = true, West = true };

	public static HexNode WithSouthEastSouthWestWest() =>
		new() { SouthEast = true, SouthWest = true, West = true };

	public bool NorthWest = true;
	public bool NorthEast = true;
	public bool East = true;
	public bool SouthEast = false;
	public bool SouthWest = false;
	public bool West = false;
}
