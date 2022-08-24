using SkiaSharp;

namespace Maze;

internal static class SKHelpers
{
	public static void LineOrMoveTo(this SKPath path, bool line, SKPoint point)
	{
		if (line)
		{
			path.LineTo(point);
		}
		else
		{
			path.MoveTo(point);
		}
	}
}
