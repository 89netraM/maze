using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze;

public class TriHexGrid : HexGrid, IGraphCreator<TriHexGrid>, IEnterable<Vector2D<int>>
{
	public static TriHexGrid Create(uint size) => new(size);

	private static IReadOnlyDictionary<Vector2D<int>, HexNode> GenerateHexGrid(uint size)
	{
		var map = new Dictionary<Vector2D<int>, HexNode>();

		for (int i = 0; i < size; i++)
		{
			GenerateHexDiagonal(i);
		}

		return map;

		void GenerateHexDiagonal(int diagonal)
		{
			int diagonalCount = (int)size - diagonal;
			var coord = new Vector2D<int>(diagonal, 0);
			for (int i = 0; i < diagonalCount; i++)
			{
				GenerateHexNode(coord);
				coord += new Vector2D<int>(1, -1);
			}
		}

		void GenerateHexNode(Vector2D<int> coord)
		{
			var node = new HexNode();
			if (coord.X + coord.Y == 0)
			{
				node.West = true;
			}
			if (coord.Y == 0)
			{
				node.SouthWest = true;
				node.SouthEast = true;
			}
			map[coord] = node;
		}
	}

	private readonly uint size;

	public TriHexGrid(uint size) : base(GenerateHexGrid(size)) =>
		(this.size) = (size);

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
			return new((int)size - 1, -((int)size - 1 - index));
		}
		index -= (int)size - 1;
		if (index < size - 1)
		{
			return new((int)size - 1 - index, 0);
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
			if (entry.X == size - 1 && entry.Y == 0)
			{
				Map[entry].East = false;
			}
			else if (entry.Y == 0)
			{
				Map[entry].SouthWest = false;
			}
			else if (entry.X + entry.Y == 0)
			{
				Map[entry].NorthWest = false;
			}
			else if (entry.X == size - 1)
			{
				Map[entry].East = false;
			}
			else
			{
				throw new ArgumentException();
			}
		}
	}
}
