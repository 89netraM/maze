using System;
using System.Collections.Generic;
using System.Linq;

namespace Maze;

public static class MazeGenerators
{
	/// <summary>
	/// Removes walls from the <paramref name="graph"/> by tracing a random deapth first search path from each of the
	/// <paramref name="starts"/>. Any node in the graph will be reachable from a one, and only one, of the
	/// <paramref name="starts"/>.
	/// </summary>
	/// <param name="graph">A pre generated graph from which the maze is carved.</param>
	/// <param name="starts">The starting points for the generation.</param>
	public static void DeapthFirstSearch<TGraph, TNode>(TGraph graph, IEnumerable<TNode> starts, Random random)
		where TGraph : IGraph<TNode>
	{
		var visited = new HashSet<TNode>(starts);
		var toVisits = visited.Select(SingletonStack).ToArray();

		while (toVisits.Any(toVisit => toVisit.Count > 0))
		{
			foreach (var toVisit in toVisits)
			{
				if (toVisit.TryPop(out TNode? current) && current is not null)
				{
					var neighbours = graph.Neighbours(current).Where(n => !visited.Contains(n)).ToArray();
					if (neighbours.Length > 1)
					{
						toVisit.Push(current);
					}
					if (neighbours.Length > 0)
					{
						var next = neighbours[random.Next(neighbours.Length)];
						graph[current, next] = false;
						visited.Add(next);
						toVisit.Push(next);
					}
				}
			}
		}
	}

	private static Stack<T> SingletonStack<T>(T element)
	{
		var stack = new Stack<T>();
		stack.Push(element);
		return stack;
	}
}
