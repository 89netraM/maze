using System;
using System.Collections.Generic;

namespace Maze;

public interface IGraph<INode>
{
	/// <summary>
	/// Indicates if two are connected.
	/// </summary>
	/// <exception cref="ArgumentException">If the two nodes are not neighbours.</exception>
	public bool this[INode a, INode b] { get; set; }

	/// <summary>
	/// Lists all neighbours of the <paramref name="current"/> node.
	/// </summary>
	public IEnumerable<INode> Neighbours(INode current);
}
