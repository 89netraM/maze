using System;
using System.Collections.Generic;

namespace Maze;

public interface IGraph<TNode>
{
	/// <summary>
	/// Indicates if two are connected.
	/// </summary>
	/// <exception cref="ArgumentException">If the two nodes are not neighbours.</exception>
	public bool this[TNode a, TNode b] { get; set; }

	/// <summary>
	/// Lists all neighbours of the <paramref name="current"/> node.
	/// </summary>
	public IEnumerable<TNode> Neighbours(TNode current);
}
