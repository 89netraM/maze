using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public abstract class HexGrid : IGraph<HexCoordinate>, ISVGDrawable
{
	private delegate void HexEdgeVisitor(ref bool a, ref bool b);

	public IReadOnlyDictionary<HexCoordinate, HexNode> Map { get; }

	public bool this[HexCoordinate a, HexCoordinate b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	protected HexGrid(IReadOnlyDictionary<HexCoordinate, HexNode> map) =>
		(Map) = (map);

	public IEnumerable<HexCoordinate> Neighbours(HexCoordinate current) =>
		HexCoordinate.Directions
			.Select(direction => current + direction)
			.Where(Map.ContainsKey);

	private ref bool GetWallReference(HexCoordinate a, HexCoordinate b)
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

		static (HexCoordinate, HexCoordinate) OrderCoordinates(HexCoordinate a, HexCoordinate b)
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

		(HexCoordinate min, HexCoordinate max) FindMinMax()
		{
			HexCoordinate min, max;
			min = max = Map.Keys.First();
			foreach (var coordinate in Map.Keys)
			{
				min = min.MinPart(coordinate);
				max = max.MaxPart(coordinate);
			}
			return (min, max);
		}

		static SKCanvas CreateCanvas(Stream outputStream, HexCoordinate min, HexCoordinate max) =>
			SKSvgCanvas.Create(
				SKRect.Create(CalculateTotalWidth(min, max), CalculateTotalHeight(min, max)),
				outputStream);

		static float CalculateTotalWidth(HexCoordinate min, HexCoordinate max) =>
			(max.X - min.X) * HORIZONTAL_SPACING * 2.0f + HORIZONTAL_SPACING * 2.0f;

		static float CalculateTotalHeight(HexCoordinate min, HexCoordinate max) =>
			(max.Y - min.Y) * VERTICAL_SPACING + SIZE * 2.0f;

		void DrawHex(HexCoordinate coord, HexNode node)
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
			LineOrMoveTo(node.NorthWest, north);
			LineOrMoveTo(node.NorthEast, northEast);
			LineOrMoveTo(node.East, southEast);
			LineOrMoveTo(node.SouthEast, south);
			LineOrMoveTo(node.SouthWest, southWest);
			LineOrMoveTo(node.West, northWest);

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

		static SKPoint HexToPixelCoordinates(HexCoordinate coord) =>
			new(2.0f * HORIZONTAL_SPACING * coord.X + HORIZONTAL_SPACING * coord.Y, VERTICAL_SPACING * coord.Y);
	}
}

public record HexCoordinate(int X, int Y)
{
	public static IReadOnlyList<HexCoordinate> Directions { get; } = new HexCoordinate[]
	{
		new(1, 0),
		new(1, -1),
		new(0, -1),
		new(-1, 0),
		new(-1, 1),
		new(0, 1),
	};

	public HexCoordinate MinPart(HexCoordinate other) =>
		new(Math.Min(X, other.X), Math.Min(Y, other.Y));

	public HexCoordinate MaxPart(HexCoordinate other) =>
		new(Math.Max(X, other.X), Math.Max(Y, other.Y));

	public static HexCoordinate operator +(HexCoordinate a, HexCoordinate b) =>
		new(a.X + b.X, a.Y + b.Y);

	public static HexCoordinate operator -(HexCoordinate coord) =>
		new(-coord.X, -coord.Y);

	public static HexCoordinate operator -(HexCoordinate a, HexCoordinate b) =>
		a + (-b);

	public static HexCoordinate operator *(HexCoordinate coord, int facor) =>
		new(coord.X * facor, coord.Y * facor);
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
