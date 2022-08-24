using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze;

public class HexHexGrid : HexGrid, IGraphCreator<HexHexGrid>, IEnterable<Vector2D<int>>
{
	public static HexHexGrid Create(uint size) => new(size);

	private static IReadOnlyDictionary<Vector2D<int>, HexNode> GenerateHexGrid(uint size)
	{
		var map = new Dictionary<Vector2D<int>, HexNode>();

		AddCenter(map);
		for (uint layer = 1; layer < size - 1; layer++)
		{
			AddHexLayer(map, layer);
		}
		AddLastHexLayer(map, size - 1);

		return map;

		static void AddCenter(IDictionary<Vector2D<int>, HexNode> map) =>
			map.Add(new(0, 0), new());

		static void AddHexLayer(IDictionary<Vector2D<int>, HexNode> map, uint layer)
		{
			foreach (var coord in CoordinatesOfHexLayer(layer))
			{
				map.Add(coord, new());
			}
		}

		static void AddLastHexLayer(IDictionary<Vector2D<int>, HexNode> map, uint layer)
		{
			var hexAdder = new HexSideVisitor(
				AddOnNorthWestSide,
				AddOnEastNorthEastSide,
				AddOnEastNorthEastSide,
				AddOnSouthEastSide,
				AddOnSouthWestSide,
				AddOnWestSide);

			foreach (var coord in CoordinatesOfHexLayer(layer))
			{
				VisitEdgeSideOfCoordinate(layer + 1, coord, hexAdder);
			}

			void AddOnNorthWestSide(Vector2D<int> coord) =>
				map.Add(coord, HexNode.WithWest());

			void AddOnEastNorthEastSide(Vector2D<int> coord)
			{
				if (coord.Y == 0)
				{
					map.Add(coord, HexNode.WithSouthEast());
				}
				else
				{
					map.Add(coord, new());
				}
			}

			void AddOnSouthEastSide(Vector2D<int> coord)
			{
				if (coord.X == 0)
				{
					map.Add(coord, HexNode.WithSouthEastSouthWest());
				}
				else
				{
					map.Add(coord, HexNode.WithSouthEast());
				}
			}

			void AddOnSouthWestSide(Vector2D<int> coord)
			{
				if (-coord.X == layer)
				{
					map.Add(coord, HexNode.WithSouthEastSouthWestWest());
				}
				else
				{
					map.Add(coord, HexNode.WithSouthEastSouthWest());
				}
			}

			void AddOnWestSide(Vector2D<int> coord) =>
				map.Add(coord, HexNode.WithSouthWestWest());
		}
	}

	private static IEnumerable<Vector2D<int>> CoordinatesOfHexLayer(uint layer)
	{
		var coord = Directions[4] * (int)layer;
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < layer; j++)
			{
				yield return coord;
				coord += Directions[i];
			}
		}
	}

	private static void VisitEdgeSideOfCoordinate(uint size, Vector2D<int> coord, HexSideVisitor visitor)
	{
		if (coord.Y > 0 || (coord.Y == 0 && coord.X < 0))
		{
			VisitSouthSideOfCoordinate(coord, visitor);
		}
		else
		{
			VisitNorthSideOfCoordinate(coord, visitor);
		}

		void VisitSouthSideOfCoordinate(Vector2D<int> coord, HexSideVisitor visitor)
		{
			if (coord.X >= 0)
			{
				visitor.VisitSouthEastSide(coord);
			}
			else if (coord.Y == size - 1)
			{
				visitor.VisitSouthWestSide(coord);
			}
			else
			{
				visitor.VisitWestSide(coord);
			}
		}

		void VisitNorthSideOfCoordinate(Vector2D<int> coord, HexSideVisitor visitor)
		{
			if (coord.X <= 0)
			{
				visitor.VisitNorthWestSide(coord);
			}
			else if (-coord.Y == size - 1)
			{
				visitor.VisitNorthEastSide(coord);
			}
			else
			{
				visitor.VisitEastSide(coord);
			}
		}
	}

	private readonly uint size;

	public HexHexGrid(uint size) : base(GenerateHexGrid(size)) =>
		(this.size) = (size);

	public IEnumerable<Vector2D<int>> GenerateEntries(uint entryCount)
	{
		var edgeLayerCount = HexLayerCount(size - 1);
		return CoordinatesOfHexLayer(size - 1)
			.Select((coord, index) => (coord, index))
			.GroupBy(EntryWindow)
			.Select(FirstCoordinateOfGroup);

		static uint HexLayerCount(uint layer) =>
			layer * 6;

		long EntryWindow((Vector2D<int>, int index) pair) =>
			pair.index / (edgeLayerCount / entryCount);

		static Vector2D<int> FirstCoordinateOfGroup(IGrouping<long, (Vector2D<int> coord, int i)> group) =>
			group.First().coord;
	}

	public void OpenEntries(IEnumerable<Vector2D<int>> entries)
	{
		var hexSideOpener = new HexSideVisitor(
			OpenNorthWestSide,
			OpenNorthEastSide,
			OpenEastSide,
			OpenSouthEastSide,
			OpenSouthWestSide,
			OpenWestSide);

		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}

		void OpenEntry(Vector2D<int> entry)
		{
			VisitEdgeSideOfCoordinate(size, entry, hexSideOpener);
		}

		void OpenNorthWestSide(Vector2D<int> entry) =>
			Map[entry].NorthWest = false;

		void OpenNorthEastSide(Vector2D<int> entry) =>
			Map[entry].NorthEast = false;

		void OpenEastSide(Vector2D<int> entry) =>
			Map[entry].East = false;

		void OpenSouthEastSide(Vector2D<int> entry) =>
			Map[entry].SouthEast = false;

		void OpenSouthWestSide(Vector2D<int> entry) =>
			Map[entry].SouthWest = false;

		void OpenWestSide(Vector2D<int> entry) =>
			Map[entry].West = false;
	}

	private record HexSideVisitor(
		Action<Vector2D<int>> VisitNorthWestSide,
		Action<Vector2D<int>> VisitNorthEastSide,
		Action<Vector2D<int>> VisitEastSide,
		Action<Vector2D<int>> VisitSouthEastSide,
		Action<Vector2D<int>> VisitSouthWestSide,
		Action<Vector2D<int>> VisitWestSide);
}
