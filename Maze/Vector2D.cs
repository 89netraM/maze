using System;

namespace Maze;

public record Vector2D<T>(T X, T Y) :
		IAdditiveIdentity<Vector2D<T>, Vector2D<T>>,
		IAdditionOperators<Vector2D<T>, Vector2D<T>, Vector2D<T>>,
		ISubtractionOperators<Vector2D<T>, Vector2D<T>, Vector2D<T>>,
		IUnaryNegationOperators<Vector2D<T>, Vector2D<T>>,
		IMultiplyOperators<Vector2D<T>, T, Vector2D<T>>,
		IMultiplicativeIdentity<Vector2D<T>, Vector2D<T>>,
		IDivisionOperators<Vector2D<T>, T, Vector2D<T>>,
		IEqualityOperators<Vector2D<T>, Vector2D<T>>,
		IEquatable<Vector2D<T>>
	where T : INumber<T>
{
	public static Vector2D<T> AdditiveIdentity => new(T.AdditiveIdentity, T.AdditiveIdentity);

	public static Vector2D<T> MultiplicativeIdentity => new(T.MultiplicativeIdentity, T.MultiplicativeIdentity);

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

	public static Vector2D<T> operator /(Vector2D<T> coord, T facor) =>
		new(coord.X / facor, coord.Y / facor);
}
