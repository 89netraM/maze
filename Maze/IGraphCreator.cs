using System;

namespace Maze;

public interface IGraphCreator<out TGraph>
{
	static abstract TGraph Create(uint size, Random random);
}
