using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maze;

public class IrregularGrid : IGraph<IrregularGrid.Square>, IGraphCreator<IrregularGrid>, IEnterable<IrregularGrid.Square>, ISVGDrawable
{
	public static IrregularGrid Create(uint size, Random random) => new(size, random);

	private static IReadOnlyList<Vector2D<double>> Directions { get; } = new Vector2D<double>[]
	{
		new(1, 0),
		new(1, -1),
		new(0, -1),
		new(-1, 0),
		new(-1, 1),
		new(0, 1),
	};

	private IDictionary<Edge<Node>, ISet<Square>> edges;
	private IReadOnlyList<Square> edgeSquares;

	public bool this[Square a, Square b]
	{
		get => edges.ContainsKey(GetEdgeBetween(a, b));
		set
		{
			if (value)
			{
				throw new ArgumentException("Can not create edges.");
			}
			edges.Remove(GetEdgeBetween(a, b));
		}
	}

	public IrregularGrid(uint size, Random random)
	{
		if (size > 25)
		{
			throw new ArgumentException("Too large.", nameof(size));
		}
		var nodes = new Dictionary<Vector2D<double>, Node>();
		var squares = new List<Square>();
		edges = new Dictionary<Edge<Node>, ISet<Square>>();
		var graph = new Graph();
		AddNodes();
		AddEdges();
		RemoveRandomEdges();
		AddSquareEdges();
		edgeSquares = FindEdgeSquares();
		for (int i = 0; i < 50; i++)
		{
			StepRelaxation();
		}

		void AddNodes()
		{
			AddNode(new(0, 0));

			for (uint layer = 1; layer < size; layer++)
			{
				AddLayer(layer);
			}

			void AddLayer(uint layer)
			{
				foreach (var point in PointsOfHexLayer(layer))
				{
					AddNode(point);
				}
			}
		}

		void AddEdges()
		{
			var neighbours = PointsOfHexLayer(1).ToArray();

			foreach (var node in graph.Nodes)
			{
				foreach (var neighbour in neighbours)
				{
					var neighbourNode = neighbour + node;
					if (graph.ContainsNode(neighbourNode))
					{
						graph.AddEdge(node, neighbourNode);
					}
				}
			}
		}

		void RemoveRandomEdges()
		{
			var nodes = graph.Nodes.ToList();

			var outerNodes = PointsOfHexLayer(size - 1).ToArray();
			var outerEdges = outerNodes.Zip(outerNodes.Skip(1).Concat(outerNodes))
				.Select(pair => new Edge<Vector2D<double>>(pair.First, pair.Second))
				.ToHashSet();

			var avaliableEdges = graph.Nodes.SelectMany(n => graph.NeighboursOf(n).Select(nn => new Edge<Vector2D<double>>(n, nn))).ToHashSet();
			avaliableEdges.ExceptWith(outerEdges);

			while (avaliableEdges.Count > 0)
			{
				var node = nodes[random.Next(nodes.Count)];
				var neighbours = graph.NeighboursOf(node).Where(n => avaliableEdges.Contains(new(node, n))).ToArray();
				if (neighbours.Length == 0)
				{
					continue;
				}
				var neighbour = neighbours[random.Next(neighbours.Length)];

				graph.RemoveEdge(node, neighbour);
				avaliableEdges.Remove(new(node, neighbour));

				var commonNeighbours = graph.NeighboursOf(neighbour).ToHashSet();
				commonNeighbours.IntersectWith(graph.NeighboursOf(node));

				foreach (var commonNeighbour in commonNeighbours)
				{
					avaliableEdges.Remove(new(node, commonNeighbour));
					avaliableEdges.Remove(new(neighbour, commonNeighbour));
				}
			}
		}

		static IEnumerable<Vector2D<double>> PointsOfHexLayer(uint layer)
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

		void AddSquareEdges()
		{
			foreach (var node in graph.Nodes.ToArray())
			{
				var nodeSouthEast = node + Directions[0];
				var nodeNorthEast = node + Directions[1];
				var nodeNorth = node + Directions[2];
				var nodeNorthWest = node + Directions[3];
				if (graph.ContainsNode(nodeSouthEast) && graph.ContainsNode(nodeNorthEast) && !graph.ContainsEdge(nodeSouthEast, nodeNorthEast))
				{
					var center = node + (Directions[0] + Directions[1]) / 2.0;
					AddNode(center);

					var southEast = center + Directions[0] / 2.0;
					AddNode(southEast);
					graph.AddEdge(center, southEast);

					var northEast = center + Directions[1] / 2.0;
					AddNode(northEast);
					graph.AddEdge(center, northEast);

					var northWest = center + Directions[3] / 2.0;
					AddNode(northWest);
					graph.AddEdge(center, northWest);

					var southWest = center + Directions[4] / 2.0;
					AddNode(southWest);
					graph.AddEdge(center, southWest);

					AddSquare(node, southWest, center, northWest);
					AddSquare(nodeSouthEast, southEast, center, southWest);
					AddSquare(nodeSouthEast + Directions[1], northEast, center, southEast);
					AddSquare(nodeNorthEast, northWest, center, northEast);
				}
				if (graph.ContainsNode(nodeNorthEast) && graph.ContainsNode(nodeNorth) && !graph.ContainsEdge(nodeNorthEast, nodeNorth))
				{
					var center = node + (Directions[1] + Directions[2]) / 2.0;
					AddNode(center);

					var northEast = center + Directions[1] / 2.0;
					AddNode(northEast);
					graph.AddEdge(center, northEast);

					var north = center + Directions[2] / 2.0;
					AddNode(north);
					graph.AddEdge(center, north);

					var southWest = center + Directions[4] / 2.0;
					AddNode(southWest);
					graph.AddEdge(center, southWest);

					var south = center + Directions[5] / 2.0;
					AddNode(south);
					graph.AddEdge(center, south);

					AddSquare(node, south, center, southWest);
					AddSquare(south, nodeNorthEast, northEast, center);
					AddSquare(center, northEast, nodeNorthEast + Directions[2], north);
					AddSquare(southWest, center, north, nodeNorth);
				}
				if (graph.ContainsNode(nodeNorth) && graph.ContainsNode(nodeNorthWest) && !graph.ContainsEdge(nodeNorth, nodeNorthWest))
				{
					var center = node + (Directions[2] + Directions[3]) / 2.0;
					AddNode(center);

					var north = center + Directions[2] / 2.0;
					AddNode(north);
					graph.AddEdge(center, north);

					var northWest = center + Directions[3] / 2.0;
					AddNode(northWest);
					graph.AddEdge(center, northWest);

					var south = center + Directions[5] / 2.0;
					AddNode(south);
					graph.AddEdge(center, south);

					var southEast = center + Directions[0] / 2.0;
					AddNode(southEast);
					graph.AddEdge(center, southEast);

					AddSquare(node, southEast, center, south);
					AddSquare(southEast, nodeNorth, north, center);
					AddSquare(center, north, nodeNorth + Directions[3], northWest);
					AddSquare(south, center, northWest, nodeNorthWest);
				}
				if (graph.ContainsEdge(node, nodeSouthEast) && graph.ContainsEdge(nodeSouthEast, nodeNorthEast) && graph.ContainsEdge(nodeNorthEast, node))
				{
					var center = node + Directions[0] * (2.0 / 3.0) + Directions[2] * (1.0 / 3.0);
					AddNode(center);

					var southEast = node + Directions[0] / 2.0;
					AddNode(southEast);
					graph.AddEdge(center, southEast);

					var east = node + Directions[0] + Directions[2] / 2.0;
					AddNode(east);
					graph.AddEdge(center, east);

					var northEast = node + Directions[1] / 2.0;
					AddNode(northEast);
					graph.AddEdge(center, northEast);

					AddSquare(node, southEast, center, northEast);
					AddSquare(southEast, nodeSouthEast, east, center);
					AddSquare(northEast, center, east, nodeNorthEast);
				}
				if (graph.ContainsEdge(node, nodeNorthEast) && graph.ContainsEdge(nodeNorthEast, nodeNorth) && graph.ContainsEdge(nodeNorth, node))
				{
					var center = node + Directions[1] * (2.0 / 3.0) + Directions[3] * (1.0 / 3.0);
					AddNode(center);

					var northEast = node + Directions[1] / 2.0;
					AddNode(northEast);
					graph.AddEdge(center, northEast);

					var northNorthEast = node + Directions[1] + Directions[3] / 2.0;
					AddNode(northNorthEast);
					graph.AddEdge(center, northNorthEast);

					var north = node + Directions[2] / 2.0;
					AddNode(north);
					graph.AddEdge(center, north);

					AddSquare(node, northEast, center, north);
					AddSquare(northEast, nodeNorthEast, northNorthEast, center);
					AddSquare(north, center, northNorthEast, nodeNorth);
				}
			}
		}

		void AddNode(Vector2D<double> position)
		{
			if (!nodes.ContainsKey(position))
			{
				graph.AddNode(position);
				nodes.Add(position, new(position));
			}
		}

		void AddSquare(Vector2D<double> a, Vector2D<double> b, Vector2D<double> c, Vector2D<double> d)
		{
			var aNode = nodes[a];
			var bNode = nodes[b];
			var cNode = nodes[c];
			var dNode = nodes[d];
			var square = new Square(aNode, bNode, cNode, dNode);
			squares.Add(square);
			AddEdge(aNode, bNode, square);
			AddEdge(bNode, cNode, square);
			AddEdge(cNode, dNode, square);
			AddEdge(dNode, aNode, square);
		}

		void AddEdge(Node a, Node b, Square square)
		{
			var edge = new Edge<Node>(a, b);
			if (!edges.TryGetValue(edge, out ISet<Square>? edgeSqaures))
			{
				edgeSqaures = new HashSet<Square>();
				edges.Add(edge, edgeSqaures);
			}
			edgeSqaures.Add(square);
		}

		IReadOnlyList<Square> FindEdgeSquares()
		{
			var edgeSquares = new HashSet<Square>();
			foreach (var squares in edges.Values)
			{
				if (squares.Count == 1)
				{
					edgeSquares.Add(squares.First());
				}
			}
			return new List<Square>(edgeSquares);
		}

		void StepRelaxation()
		{
			foreach (var square in squares)
			{
				square.Step();
			}
			foreach (var node in nodes.Values)
			{
				node.Step();
			}
		}
	}

	private Edge<Node> GetEdgeBetween(Square a, Square b)
	{
		var commonNodes = a.Nodes.ToHashSet();
		commonNodes.IntersectWith(b.Nodes);
		if (commonNodes.Count == 2)
		{
			return new(commonNodes.First(), commonNodes.Last());
		}
		else
		{
			throw new ArgumentException("Positions not neighbours.");
		}
	}

	public IEnumerable<Square> Neighbours(Square current)
	{
		foreach (var (a, b) in current.Edges)
		{
			if (edges.TryGetValue(new(a, b), out var neighbours))
			{
				foreach (var square in neighbours)
				{
					if (square != current)
					{
						yield return square;
					}
				}
			}
		}
	}

	public IEnumerable<Square> GenerateEntries(uint entryCount)
	{
		int spacing = edgeSquares.Count / (int)entryCount;
		for (int i = 0; i < entryCount; i++)
		{
			yield return edgeSquares[i * spacing];
		}
	}

	public void OpenEntries(IEnumerable<Square> entries)
	{
		foreach (var entry in entries)
		{
			OpenEntry(entry);
		}
	}

	private void OpenEntry(Square entry)
	{
		foreach (var (a, b) in entry.Edges)
		{
			Edge<Node> edge = new(a, b);
			if (edges.TryGetValue(edge, out var squares) && squares.Count == 1)
			{
				edges.Remove(edge);
				break;
			}
		}
	}

	public void DrawSVG(Stream outputStream)
	{
		const float SIZE = 30.0f;
		const float HORIZONTAL_SPACING = SIZE * 1.5f;
		const float VERTICAL_SPACING = SIZE * 0.866025f;

		var (min, max) = FindMinMax();

		using var canvas = CreateCanvas(outputStream, min, max);
		using var paint = new SKPaint { Color = SKColors.Black, IsStroke = true };

		var canvasOffset = new SKPoint(
			SIZE + HORIZONTAL_SPACING * -(float)min.X,
			VERTICAL_SPACING + VERTICAL_SPACING * 2.0f * -(float)min.Y);

		foreach (var edge in edges.Keys)
		{
			DrawEdges(edge);
		}

		(Vector2D<double> min, Vector2D<double> max) FindMinMax()
		{
			Vector2D<double> min, max;
			min = max = edges.Keys.First().A.Position;
			foreach (var edge in edges.Keys)
			{
				min = min.MinPart(edge.A.Position).MinPart(edge.B.Position);
				max = max.MaxPart(edge.A.Position).MaxPart(edge.B.Position);
			}
			return (min, max);
		}

		static SKCanvas CreateCanvas(Stream outputStream, Vector2D<double> min, Vector2D<double> max) =>
			SKSvgCanvas.Create(
				SKRect.Create(CalculateTotalWidth(min, max), CalculateTotalHeight(min, max)),
				outputStream);

		static float CalculateTotalWidth(Vector2D<double> min, Vector2D<double> max) =>
			(float)(max.X - min.X) * HORIZONTAL_SPACING + SIZE * 2.0f;

		static float CalculateTotalHeight(Vector2D<double> min, Vector2D<double> max) =>
			(float)(max.Y - min.Y) * VERTICAL_SPACING * 2.0f + VERTICAL_SPACING * 2.0f;

		void DrawEdges(Edge<Node> edge)
		{
			DrawEdge(edge.A.Position, edge.B.Position);
		}

		void DrawEdge(Vector2D<double> a, Vector2D<double> b)
		{
			var aPixelCoordinate = HexToPixelCoordinates(a) + canvasOffset;
			var bPixelCoordinate = HexToPixelCoordinates(b) + canvasOffset;

			canvas.DrawLine(aPixelCoordinate, bPixelCoordinate, paint);
		}

		static SKPoint HexToPixelCoordinates(Vector2D<double> point) =>
			new(HORIZONTAL_SPACING * (float)point.X,
				2.0f * VERTICAL_SPACING * (float)point.Y + VERTICAL_SPACING * (float)point.X);
	}

	private class Graph
	{
		private readonly IDictionary<Vector2D<double>, ISet<Vector2D<double>>> nodes = new Dictionary<Vector2D<double>, ISet<Vector2D<double>>>();

		public IEnumerable<Vector2D<double>> Nodes => nodes.Keys;

		public void AddNode(Vector2D<double> node)
		{
			if (!nodes.ContainsKey(node))
			{
				nodes.Add(node, new HashSet<Vector2D<double>>());
			}
		}

		public void RemoveNode(Vector2D<double> node)
		{
			foreach (var neighbour in NeighboursOf(node))
			{
				nodes[neighbour].Remove(node);
			}
			nodes.Remove(node);
		}

		public bool ContainsNode(Vector2D<double> node) =>
			nodes.ContainsKey(node);

		public IEnumerable<Vector2D<double>> NeighboursOf(Vector2D<double> node) =>
			nodes[node];

		public void AddEdge(Vector2D<double> a, Vector2D<double> b)
		{
			nodes[a].Add(b);
			nodes[b].Add(a);
		}

		public void RemoveEdge(Vector2D<double> a, Vector2D<double> b)
		{
			nodes[a].Remove(b);
			nodes[b].Remove(a);
		}

		public bool ContainsEdge(Vector2D<double> a, Vector2D<double> b) =>
			nodes.ContainsKey(a) && nodes[a].Contains(b);
	}

	private class Edge<TNode> : IEquatable<Edge<TNode>>
	{
		public TNode A { get; }
		public TNode B { get; }

		public Edge(TNode a, TNode b) =>
			(A, B) = (a, b);

		public override int GetHashCode() =>
			A.GetHashCode() ^ B.GetHashCode();

		public override bool Equals(object? obj) =>
			obj is Edge<TNode> e && Equals(e);

		public bool Equals(Edge<TNode>? other) =>
			other is not null &&
			((A.Equals(other.A) && B.Equals(other.B)) ||
			(A.Equals(other.B) && B.Equals(other.A)));

		public static bool operator ==(Edge<TNode> left, Edge<TNode> right) =>
			left.Equals(right);
		public static bool operator !=(Edge<TNode> left, Edge<TNode> right) =>
			!(left == right);
	}

	public class Node
	{
		public Vector2D<double> Position { get; private set; }
		public Vector2D<double> Velocity { get; set; } = Vector2D<double>.AdditiveIdentity;

		public Node(Vector2D<double> position) =>
			(Position) = (position);

		public void Step()
		{
			var magnitude = Math.Sqrt(Math.Pow(Velocity.X, 2) + Math.Pow(Velocity.Y, 2));
			if (magnitude > 0)
			{
				Position += (Velocity / magnitude) * 0.025;
			}
			Velocity = Vector2D<double>.AdditiveIdentity;
		}
	}

	public class Square
	{
		public Node A { get; }
		public Node B { get; }
		public Node C { get; }
		public Node D { get; }

		public Vector2D<double> Center => (A.Position + B.Position + C.Position + D.Position) / 4;

		public IEnumerable<Node> Nodes => new[] { A, B, C, D };
		public IEnumerable<(Node, Node)> Edges => Nodes.Zip(Nodes.Skip(1).Concat(Nodes));

		public Square(Node a, Node b, Node c, Node d) =>
			(A, B, C, D) = (a, b, c, d);

		public void Step()
		{
			foreach (var (node, target) in Nodes.Zip(TargetCorners()))
			{
				node.Velocity += target - node.Position;
			}
		}

		private IEnumerable<Vector2D<double>> TargetCorners()
		{
			var doubleNodes = Nodes.Concat(Nodes);
			foreach (var (node, i) in Nodes.Select((n, i) => (n, i)))
			{
				yield return TargetCorner(node, doubleNodes.Skip(i + 1).Take(3));
			}

			Vector2D<double> TargetCorner(Node node, IEnumerable<Node> others) =>
				others.Select(MakeRelative)
					.Select(Rotate)
					.Aggregate(SumVectors) / 3 + Center;

			Vector2D<double> MakeRelative(Node node) =>
				node.Position - Center;

			Vector2D<double> Rotate(Vector2D<double> pos, int index)
			{
				var rotation = (index + 1) * Math.PI / 2.0;
				return new(pos.X * Math.Cos(rotation) - pos.Y * Math.Sin(rotation),
					pos.X * Math.Sin(rotation) + pos.Y * Math.Sin(rotation));
			}

			Vector2D<double> SumVectors(Vector2D<double> a, Vector2D<double> b) => a + b;
		}
	}
}
