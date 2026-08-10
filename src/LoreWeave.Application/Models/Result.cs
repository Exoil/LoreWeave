using System.Diagnostics.CodeAnalysis;

namespace LoreWeave.Application.Models;

/// <summary>
///     Result TError  is a readonly struct, that can be used to wrap the result
///     of an operation that could potentially fail with an exception. It provides
///     information about whether the operation was successful and the associated error message (if any).
/// </summary>
public readonly struct Result<TError>
    where TError : Exception
{
    public readonly TError? Error;
    private readonly bool _isError;
    public bool IsSuccess => !_isError;

    private Result(TError? error)
    {
        Error = error;
        _isError = error is not null;
    }

    /// <summary>
    ///     Implicit conversion from TError type to Result TError. It enables you to
    ///     create a Result without having to explicitly instantiate a new Result.
    /// </summary>
    public static implicit operator Result<TError>(TError error)
        => new(error);
}

/// <summary>
///     Result TValue, TError is a readonly struct that can be used to wrap
///     the result of an operation that could potentially fail with an exception and provides
///     the result of operation itself. It provides information about whether the operation
///     was successful, the returned value, and the associated error message (if any).
/// </summary>
public readonly struct Result<TValue, TError>
    where TError : Exception
{
    private readonly TValue? _value;

    [SuppressMessage("Major Code Smell", "S2372:Exceptions should not be thrown from property getters",
        Justification =
            "Throwing from this getter is the point - it mirrors Nullable<T>.Value and forces callers "
            + "to check IsSuccess before reading the payload.")]
    public TValue Value
    {
        get
        {
            if (_isError)
            {
                throw Error!;
            }

            return _value!;
        }
    }

    public readonly TError? Error;
    private readonly bool _isError;
    public bool IsSuccess => !_isError;

    private Result(TValue value)
    {
        _isError = false;
        _value = value;
        Error = default;
    }

    private Result(TError? error)
    {
        _isError = true;
        _value = default;
        Error = error;
    }

    /// <summary>
    ///     Implicit conversion from TValue type to Result TValue, TError. It enables you to
    ///     create a Result without having to explicitly instantiate a new Result.
    /// </summary>
    public static implicit operator Result<TValue, TError>(TValue value)
        => new(value);

    /// <summary>
    ///     Implicit conversion from TError type to Result TValue, TError. It enables you to
    ///     create a Result without having to explicitly instantiate a new Result.
    /// </summary>
    public static implicit operator Result<TValue, TError>(TError error)
        => new(error);
}
