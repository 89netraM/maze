using System.Collections.Generic;

namespace Maze;

public interface IEnterable<TNode>
{
	/// <summary>
	/// Opens the outside walls of the given <paramref name="entries"/>.
	/// </summary>
	void OpenEntries(IEnumerable<TNode> entries);
}
