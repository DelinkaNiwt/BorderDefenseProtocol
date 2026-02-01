using System;
using System.Runtime.CompilerServices;

namespace Scriban.Helpers;

internal struct FastStack<T>
{
	private T[] _array;

	private int _size;

	private const int DefaultCapacity = 4;

	public int Count => _size;

	public T[] Items => _array;

	public FastStack(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", "Capacity must be > 0");
		}
		_array = new T[capacity];
		_size = 0;
	}

	public void Clear()
	{
		Array.Clear(_array, 0, _size);
		_size = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Peek()
	{
		if (_size == 0)
		{
			ThrowForEmptyStack();
		}
		return _array[_size - 1];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Pop()
	{
		if (_size == 0)
		{
			ThrowForEmptyStack();
		}
		T result = _array[--_size];
		_array[_size] = default(T);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Push(T item)
	{
		if (_size == _array.Length)
		{
			Array.Resize(ref _array, (_array.Length == 0) ? 4 : (2 * _array.Length));
		}
		_array[_size++] = item;
	}

	private void ThrowForEmptyStack()
	{
		throw new InvalidOperationException("Stack is empty");
	}
}
