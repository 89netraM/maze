using System.Collections.Generic;

namespace Maze;

public interface IEnterable<TNode>
{
	/// <summary>
	/// Returns <paramref name="entryCount"/> number of entry nodes.
	/// </summary>
	IEnumerable<TNode> GenerateEntries(uint entryCount);

	/// <summary>
	/// Opens the outside walls of the given <paramref name="entries"/>.
	/// </summary>
	void OpenEntries(IEnumerable<TNode> entries);
}
