using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze;

public class HexHexGrid : HexGrid, IEnterable<HexCoordinate>
{
	private static IReadOnlyDictionary<HexCoordinate, HexNode> GenerateHexGrid(uint size)
	{
		var map = new Dictionary<HexCoordinate, HexNode>();

		AddCenter(map);
		for (uint layer = 1; layer < size - 1; layer++)
		{
			AddHexLayer(map, layer);
		}
		AddLastHexLayer(map, size - 1);

		return map;

		static void AddCenter(IDictionary<HexCoordinate, HexNode> map) =>
			map.Add(new(0, 0), new());

		static void AddHexLayer(IDictionary<HexCoordinate, HexNode> map, uint layer)
		{
			foreach (var coord in CoordinatesOfHexLayer(layer))
			{
				map.Add(coord, new());
			}
		}

		static void AddLastHexLayer(IDictionary<HexCoordinate, HexNode> map, uint layer)
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

			void AddOnNorthWestSide(HexCoordinate coord) =>
				map.Add(coord, HexNode.WithWest());

			void AddOnEastNorthEastSide(HexCoordinate coord)
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

			void AddOnSouthEastSide(HexCoordinate coord)
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

			void AddOnSouthWestSide(HexCoordinate coord)
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

			void AddOnWestSide(HexCoordinate coord) =>
				map.Add(coord, HexNode.WithSouthWestWest());
		}
	}

	private static IEnumerable<HexCoordinate> CoordinatesOfHexLayer(uint layer)
	{
		var coord = HexCoordinate.Directions[4] * (int)layer;
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < layer; j++)
			{
				yield return coord;
				coord += HexCoordinate.Directions[i];
			}
		}
	}

	private static void VisitEdgeSideOfCoordinate(uint size, HexCoordinate coord, HexSideVisitor visitor)
	{
		if (coord.Y > 0 || (coord.Y == 0 && coord.X < 0))
		{
			VisitSouthSideOfCoordinate(coord, visitor);
		}
		else
		{
			VisitNorthSideOfCoordinate(coord, visitor);
		}

		void VisitSouthSideOfCoordinate(HexCoordinate coord, HexSideVisitor visitor)
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

		void VisitNorthSideOfCoordinate(HexCoordinate coord, HexSideVisitor visitor)
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

	public IEnumerable<HexCoordinate> GenerateEntries(uint entryCount)
	{
		var edgeLayerCount = HexLayerCount(size - 1);
		return CoordinatesOfHexLayer(size - 1)
			.Select((coord, index) => (coord, index))
			.GroupBy(EntryWindow)
			.Select(FirstCoordinateOfGroup);

		static uint HexLayerCount(uint layer) =>
			layer * 6;

		long EntryWindow((HexCoordinate, int index) pair) =>
			pair.index / (edgeLayerCount / entryCount);

		static HexCoordinate FirstCoordinateOfGroup(IGrouping<long, (HexCoordinate coord, int i)> group) =>
			group.First().coord;
	}

	public void OpenEntries(IEnumerable<HexCoordinate> entries)
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

		void OpenEntry(HexCoordinate entry)
		{
			VisitEdgeSideOfCoordinate(size, entry, hexSideOpener);
		}

		void OpenNorthWestSide(HexCoordinate entry) =>
			Map[entry].NorthWest = false;

		void OpenNorthEastSide(HexCoordinate entry) =>
			Map[entry].NorthEast = false;

		void OpenEastSide(HexCoordinate entry) =>
			Map[entry].East = false;

		void OpenSouthEastSide(HexCoordinate entry) =>
			Map[entry].SouthEast = false;

		void OpenSouthWestSide(HexCoordinate entry) =>
			Map[entry].SouthWest = false;

		void OpenWestSide(HexCoordinate entry) =>
			Map[entry].West = false;
	}

	private record HexSideVisitor(
		Action<HexCoordinate> VisitNorthWestSide,
		Action<HexCoordinate> VisitNorthEastSide,
		Action<HexCoordinate> VisitEastSide,
		Action<HexCoordinate> VisitSouthEastSide,
		Action<HexCoordinate> VisitSouthWestSide,
		Action<HexCoordinate> VisitWestSide);
}
