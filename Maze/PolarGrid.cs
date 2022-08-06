using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class PolarGrid : IGraph<PolarPosition>, ISVGDrawable
{
	public IReadOnlyList<IReadOnlyList<PolarCell>> Grid { get; }

	public bool this[PolarPosition a, PolarPosition b]
	{
		get => WallReference(a, b);
		set => WallReference(a, b) = value;
	}

	public PolarGrid(uint layers)
	{
		var grid = new IReadOnlyList<PolarCell>[layers];
		grid[0] = new[] { new PolarCell(false) };

		for (uint l = 1; l < layers; l++)
		{
			double radius = (double)l / layers;
			double circumference = 2.0d * Math.PI * radius;

			int previousCount = grid[l - 1].Count;
			double estimatedCellWidth = circumference / previousCount;
			double ratio = Math.Round(estimatedCellWidth / (1.0d / layers));

			int count = (int)(previousCount * ratio);
			grid[l] = Enumerable.Range(0, count)
				.Select(_ =>
				{
					var cell = new PolarCell(true);
					if (l + 1 == layers)
					{
						cell.Outward = new[] { true };
					}
					return cell;
				})
				.ToArray();

			foreach (var innerCell in grid[l - 1])
			{
				innerCell.Outward = Enumerable.Range(0, count / previousCount).Select(_ => true).ToArray();
			}
		}

		Grid = grid;
	}

	public IEnumerable<PolarPosition> Neighbours(PolarPosition current)
	{
		if (current.Layer != 0)
		{
			yield return new PolarPosition(
				current.Layer - 1,
				current.Cell / (uint)(Grid[(int)current.Layer].Count / Grid[(int)current.Layer - 1].Count));
		}
		if (current.Layer != 0)
		{
			yield return new PolarPosition(
				current.Layer,
				(current.Cell - 1 + (uint)Grid[(int)current.Layer].Count) % (uint)Grid[(int)current.Layer].Count);
		}
		if (current.Layer != 0)
		{
			yield return new PolarPosition(current.Layer, (current.Cell + 1) % (uint)Grid[(int)current.Layer].Count);
		}
		if (current.Layer + 1 < Grid.Count)
		{
			var aOutwardCount = Grid[(int)current.Layer][(int)current.Cell].Outward.Length;
			for (uint i = 0; i < aOutwardCount; i++)
			{
				yield return new PolarPosition(
					current.Layer + 1,
					current.Cell * ((uint)Grid[(int)current.Layer + 1].Count / (uint)Grid[(int)current.Layer].Count) + i);
			}
		}
	}

	private ref bool WallReference(PolarPosition a, PolarPosition b)
	{
		if (a.Layer > b.Layer || a.Cell > b.Cell)
		{
			(a, b) = (b, a);
		}

		if (a == b)
		{
			throw new ArgumentException("Positions are equal.");
		}
		else if (a.Layer == b.Layer)
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
		else if (a.Layer + 1 == b.Layer)
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
		else
		{
			throw new ArgumentException("Positions are not neighbours.");
		}
	}

	public void DrawSVG(Stream outputStream)
	{
		const float LAYER_SIZE = 100.0f;

		var canvasRect = SKRect.Create(Grid.Count * LAYER_SIZE, Grid.Count * LAYER_SIZE);
		using var canvas = SKSvgCanvas.Create(canvasRect, outputStream);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true };

		for (int layer = 0; layer < Grid.Count; layer++)
		{
			int cellCount = Grid[layer].Count;
			float cellWidth = 360.0f / cellCount;

			var ovalSize = new SKSize((layer + 1) * LAYER_SIZE, (layer + 1) * LAYER_SIZE);
			var ovalTopLeft = new SKPoint((canvasRect.Width - ovalSize.Width) / 2.0f, (canvasRect.Height - ovalSize.Height) / 2.0f);
			var oval = SKRect.Create(ovalTopLeft, ovalSize);
			for (int cell = 0; cell < cellCount; cell++)
			{
				using var line = new SKPath();

				int wallCount = Grid[layer][cell].Outward.Length;
				float wallWidth = cellWidth / wallCount;
				for (int wall = 0; wall < wallCount; wall++)
				{
					if (Grid[layer][cell].Outward[wall])
					{
						line.ArcTo(oval, cellWidth * cell + wallWidth * wall, wallWidth, true);
					}
				}

				if (Grid[layer][cell].Clockwise)
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

				canvas.DrawPath(line, paint);
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
	public bool[] Outward { get; set; } = Array.Empty<bool>();

	public PolarCell(bool clockwise) =>
		(Clockwise) = (clockwise);
}
