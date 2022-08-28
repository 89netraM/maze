using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class PolarGrid : IGraph<Vector2D<uint>>, IGraphCreator<PolarGrid>, IEnterable<Vector2D<uint>>, ISVGDrawable
{
	public static PolarGrid Create(uint size, Random random) => new(size);

	public IReadOnlyList<IReadOnlyList<PolarCell>> Grid { get; }

	public bool this[Vector2D<uint> a, Vector2D<uint> b]
	{
		get => GetWallReference(a, b);
		set => GetWallReference(a, b) = value;
	}

	public PolarGrid(uint layerCount)
	{
		var grid = new IReadOnlyList<PolarCell>[layerCount];
		grid[0] = BuildFirstLayer();

		for (int layer = 1; layer < layerCount; layer++)
		{
			int previousCellCount = grid[layer - 1].Count;
			int cellCount = CalculateCellCountOfLayer(layer);

			grid[layer] = layer + 1 == layerCount ? BuildLastLayer(cellCount) : BuildLayer(cellCount);
			ConnectToPreviousLayer(layer);
		}

		Grid = grid;

		int CalculateCellCountOfLayer(int layer)
		{
			int previousCellCount = grid[layer - 1].Count;

			double radius = (double)layer / layerCount;
			double circumference = 2.0d * Math.PI * radius;

			double estimatedCellWidth = circumference / previousCellCount;
			double ratio = Math.Round(estimatedCellWidth * layerCount);

			return (int)(previousCellCount * ratio);
		}

		static IReadOnlyList<PolarCell> BuildFirstLayer() =>
			new[] { new PolarCell(false) };

		static IReadOnlyList<PolarCell> BuildLayer(int cellCount) =>
			Enumerable.Range(0, cellCount)
				.Select(_ => new PolarCell(true))
				.ToArray();

		static IReadOnlyList<PolarCell> BuildLastLayer(int cellCount) =>
			Enumerable.Range(0, cellCount)
				.Select(_ => new PolarCell(true, new[] { true }))
				.ToArray();

		void ConnectToPreviousLayer(int layer)
		{
			int cellCount = grid[layer].Count;
			int previousCellCount = grid[layer - 1].Count;
			foreach (var innerCell in grid[layer - 1])
			{
				innerCell.Outward = Enumerable.Range(0, cellCount / previousCellCount).Select(_ => true).ToArray();
			}
		}
	}

	public IEnumerable<Vector2D<uint>> Neighbours(Vector2D<uint> current)
	{
		if (current.Y != 0)
		{
			yield return GetInwardPosition();
		}
		if (current.Y != 0)
		{
			yield return GetCounterClockwisePosition();
		}
		if (current.Y != 0)
		{
			yield return GetClockwisePosition();
		}
		if (current.Y + 1 < Grid.Count)
		{
			var outwardCount = Grid[(int)current.Y][(int)current.X].Outward.Length;
			for (uint index = 0; index < outwardCount; index++)
			{
				yield return GetOutwardPosition(index);
			}
		}

		Vector2D<uint> GetInwardPosition() =>
			new(current.X / CalculateOutwardCountRatio(current.Y - 1),
				current.Y - 1);

		Vector2D<uint> GetCounterClockwisePosition() =>
			new((current.X - 1 + (uint)Grid[(int)current.Y].Count) % (uint)Grid[(int)current.Y].Count,
				current.Y);

		Vector2D<uint> GetClockwisePosition() =>
			new((current.X + 1) % (uint)Grid[(int)current.Y].Count,
				current.Y);

		Vector2D<uint> GetOutwardPosition(uint outwardIndex) =>
			new(current.X * CalculateOutwardCountRatio(current.Y) + outwardIndex,
				current.Y + 1);

		uint CalculateOutwardCountRatio(uint layer) =>
			(uint)(Grid[(int)layer + 1].Count / Grid[(int)layer].Count);
	}

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
		else if (a.Y + 1 == b.Y)
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
				return ref Grid[(int)a.Y][(int)a.X].clockwise;
			}
			else if (a.X == 0 && b.X == Grid[(int)a.Y].Count - 1)
			{
				return ref Grid[(int)a.Y][^1].clockwise;
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}

		ref bool GetHorizontalWallReference(Vector2D<uint> a, Vector2D<uint> b)
		{
			var aOutwardCount = Grid[(int)a.Y][(int)a.X].Outward.Length;
			if (a.X == b.X / aOutwardCount)
			{
				return ref Grid[(int)a.Y][(int)a.X].Outward[b.X % aOutwardCount];
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}
	}

	public IEnumerable<Vector2D<uint>> GenerateEntries(uint entryCount)
	{
		if (entryCount == 0 || entryCount > Grid[^1].Count)
		{
			throw new ArgumentOutOfRangeException(paramName: nameof(entryCount));
		}

		var spacing = Grid[^1].Count / entryCount;
		return Enumerable.Range(0, (int)entryCount)
			.Select(i => new Vector2D<uint>((uint)(i * spacing), (uint)Grid.Count - 1));
	}

	public void OpenEntries(IEnumerable<Vector2D<uint>> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(Vector2D<uint> entry)
		{
			Grid[(int)entry.Y][(int)entry.X].Outward = new[] { false };
		}
	}

	public void DrawSVG(Stream outputStream)
	{
		const float LAYER_SIZE = 100.0f;

		var canvasRect = SKRect.Create(Grid.Count * LAYER_SIZE, Grid.Count * LAYER_SIZE);
		using var canvas = SKSvgCanvas.Create(canvasRect, outputStream);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true, StrokeCap = SKStrokeCap.Round, };

		for (int layer = 0; layer < Grid.Count; layer++)
		{
			DrawLayer(layer);
		}

		void DrawLayer(int layer)
		{
			int cellCount = Grid[layer].Count;
			float cellWidth = 360.0f / cellCount;

			var ovalSize = new SKSize((layer + 1) * LAYER_SIZE, (layer + 1) * LAYER_SIZE);
			var ovalTopLeft = new SKPoint((canvasRect.Width - ovalSize.Width) / 2.0f, (canvasRect.Height - ovalSize.Height) / 2.0f);
			var oval = SKRect.Create(ovalTopLeft, ovalSize);

			for (int cell = 0; cell < cellCount; cell++)
			{
				DrawCell(cell);
			}

			void DrawCell(int cell)
			{
				using var line = new SKPath();

				int wallCount = Grid[layer][cell].Outward.Length;
				float wallWidth = cellWidth / wallCount;
				for (int wall = 0; wall < wallCount; wall++)
				{
					DrawOutwardWall(wall);
				}

				if (Grid[layer][cell].Clockwise)
				{
					DrawClockwiseWall();
				}

				canvas.DrawPath(line, paint);

				void DrawOutwardWall(int wall)
				{
					if (Grid[layer][cell].Outward[wall])
					{
						line.ArcTo(oval, cellWidth * cell + wallWidth * wall, wallWidth, true);
					}
				}

				void DrawClockwiseWall()
				{
					float radius = LAYER_SIZE * layer / 2.0f;
					float angle = cellWidth * (cell + 1);
					line.MoveTo(
						canvasRect.Width / 2.0f + (radius + LAYER_SIZE / 2.0f) * MathF.Cos(angle * MathF.PI / 180.0f),
						canvasRect.Height / 2.0f + (radius + LAYER_SIZE / 2.0f) * MathF.Sin(angle * MathF.PI / 180.0f));
					line.LineTo(
						canvasRect.Width / 2.0f + radius * MathF.Cos(angle * MathF.PI / 180.0f),
						canvasRect.Height / 2.0f + radius * MathF.Sin(angle * MathF.PI / 180.0f));
				}
			}
		}
	}
}

/// <summary>
/// A single cell in a <see cref="PolarGrid"/> containing the clockwise and outward walls.
/// </summary>
public class PolarCell
{
	internal bool clockwise;
	public bool Clockwise
	{
		get => clockwise;
		set => clockwise = value;
	}
	public bool[] Outward { get; set; }

	public PolarCell(bool clockwise) : this(clockwise, Array.Empty<bool>()) { }

	public PolarCell(bool clockwise, bool[] outward) =>
		(Clockwise, Outward) = (clockwise, outward);
}
