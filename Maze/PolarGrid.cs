using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class PolarGrid : IGraph<PolarPosition>, IEnterable<PolarPosition>, ISVGDrawable
{
	public IReadOnlyList<IReadOnlyList<PolarCell>> Grid { get; }

	public bool this[PolarPosition a, PolarPosition b]
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

	public IEnumerable<PolarPosition> Neighbours(PolarPosition current)
	{
		if (current.Layer != 0)
		{
			yield return GetInwardPosition();
		}
		if (current.Layer != 0)
		{
			yield return GetCounterClockwisePosition();
		}
		if (current.Layer != 0)
		{
			yield return GetClockwisePosition();
		}
		if (current.Layer + 1 < Grid.Count)
		{
			var outwardCount = Grid[(int)current.Layer][(int)current.Cell].Outward.Length;
			for (uint index = 0; index < outwardCount; index++)
			{
				yield return GetOutwardPosition(index);
			}
		}

		PolarPosition GetInwardPosition() =>
			new(current.Layer - 1,
				current.Cell / CalculateOutwardCountRatio(current.Layer - 1));

		PolarPosition GetCounterClockwisePosition() =>
			new(current.Layer,
				(current.Cell - 1 + (uint)Grid[(int)current.Layer].Count) % (uint)Grid[(int)current.Layer].Count);

		PolarPosition GetClockwisePosition() =>
			new(current.Layer,
				(current.Cell + 1) % (uint)Grid[(int)current.Layer].Count);

		PolarPosition GetOutwardPosition(uint outwardIndex) =>
			new(current.Layer + 1,
				current.Cell * CalculateOutwardCountRatio(current.Layer) + outwardIndex);

		uint CalculateOutwardCountRatio(uint layer) =>
			(uint)(Grid[(int)layer + 1].Count / Grid[(int)layer].Count);
	}

	private ref bool GetWallReference(PolarPosition a, PolarPosition b)
	{
		(a, b) = OrderPositions(a, b);

		if (a == b)
		{
			throw new ArgumentException("Positions are equal.");
		}
		else if (a.Layer == b.Layer)
		{
			return ref GetVerticalWallReference(a, b);
		}
		else if (a.Layer + 1 == b.Layer)
		{
			return ref GetHorizontalWallReference(a, b);
		}
		else
		{
			throw new ArgumentException("Positions are not neighbours.");
		}

		static (PolarPosition, PolarPosition) OrderPositions(PolarPosition a, PolarPosition b) =>
			a.Layer <= b.Layer && a.Cell <= b.Cell ?
				(a, b) :
				(b, a);

		ref bool GetVerticalWallReference(PolarPosition a, PolarPosition b)
		{
			if (a.Cell + 1 == b.Cell)
			{
				return ref Grid[(int)a.Layer][(int)a.Cell].clockwise;
			}
			else if (a.Cell == 0 && b.Cell == Grid[(int)a.Layer].Count - 1)
			{
				return ref Grid[(int)a.Layer][^1].clockwise;
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}

		ref bool GetHorizontalWallReference(PolarPosition a, PolarPosition b)
		{
			var aOutwardCount = Grid[(int)a.Layer][(int)a.Cell].Outward.Length;
			if (a.Cell == b.Cell / aOutwardCount)
			{
				return ref Grid[(int)a.Layer][(int)a.Cell].Outward[b.Cell % aOutwardCount];
			}
			else
			{
				throw new ArgumentException("Positions are not neighbours.");
			}
		}
	}

	public IEnumerable<PolarPosition> GenerateEntries(uint entryCount)
	{
		if (entryCount == 0 || entryCount > Grid[^1].Count)
		{
			throw new ArgumentOutOfRangeException(paramName: nameof(entryCount));
		}

		var spacing = Grid[^1].Count / entryCount;
		return Enumerable.Range(0, (int)entryCount)
			.Select(i => new PolarPosition((uint)Grid.Count - 1, (uint)(i * spacing)));
	}

	public void OpenEntries(IEnumerable<PolarPosition> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(PolarPosition entry)
		{
			Grid[(int)entry.Layer][(int)entry.Cell].Outward = new[] { false };
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
/// A position in a <see cref="PolarGrid"/> reprecented by <paramref name="Layer"/> and <paramref name="Cell"/>.
/// </summary>
public record PolarPosition(uint Layer, uint Cell);

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
