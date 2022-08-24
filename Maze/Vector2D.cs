using System;

namespace Maze;

public record Vector2D<T>(T X, T Y) where T : INumber<T>
{
	public Vector2D<T> MinPart(Vector2D<T> other) =>
		new(T.Min(X, other.X), T.Min(Y, other.Y));

	public Vector2D<T> MaxPart(Vector2D<T> other) =>
		new(T.Max(X, other.X), T.Max(Y, other.Y));

	public static Vector2D<T> operator +(Vector2D<T> a, Vector2D<T> b) =>
		new(a.X + b.X, a.Y + b.Y);

	public static Vector2D<T> operator -(Vector2D<T> coord) =>
		new(-coord.X, -coord.Y);

	public static Vector2D<T> operator -(Vector2D<T> a, Vector2D<T> b) =>
		new(a.X - b.X, a.Y - b.Y);

	public static Vector2D<T> operator *(Vector2D<T> coord, T facor) =>
		new(coord.X * facor, coord.Y * facor);
}
