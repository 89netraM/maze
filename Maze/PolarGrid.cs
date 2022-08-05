using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze;

public class PolarGrid : IGraph<PolarPosition>
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

			int previousCount = grid[^1].Count;
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

			foreach (var innerCell in grid[^2])
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
		if (current.Layer < Grid.Count)
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
