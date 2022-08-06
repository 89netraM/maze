using System.IO;

namespace Maze;

public interface ISVGDrawable
{
	void DrawSVG(Stream outputStream);
}
