// Result pattern implementation to avoid exception-driven flow
// Demonstrates: Generic types, discriminated unions pattern, fluent API
namespace CloudPizza.Shared.Common;

/// <summary>
/// Result pattern for explicit error handling without exceptions.
/// Use this for business logic failures, not infrastructure failures.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;
    private readonly Dictionary<string, string[]>? _validationErrors;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access value of a failed result");

    public string Error =>
        IsFailure
            ? _error ?? "Unknown error"
            : throw new InvalidOperationException("Cannot access error of a successful result");

    public Dictionary<string, string[]>? ValidationErrors => _validationErrors;

    private Result(T value)
    {
        _value = value;
        _error = null;
        _validationErrors = null;
        IsSuccess = true;
    }

    private Result(string error, Dictionary<string, string[]>? validationErrors = null)
    {
        _value = default;
        _error = error;
        _validationErrors = validationErrors;
        IsSuccess = false;
    }

    // Factory methods for creating results
    public static Result<T> Success(T value)
    {
        return new(value);
    }

    public static Result<T> Failure(string error)
    {
        return new(error);
    }

    public static Result<T> ValidationFailure(string error, Dictionary<string, string[]> validationErrors)
    {
        return new(error, validationErrors);
    }

    // Functional programming helpers
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(Error);
    }

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(await mapper(Value))
            : Result<TNew>.Failure(Error);
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess
            ? binder(Value)
            : Result<TNew>.Failure(Error);
    }

    public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder)
    {
        return IsSuccess
            ? await binder(Value)
            : Result<TNew>.Failure(Error);
    }

    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value : defaultValue;
    }

    public void Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Error);
        }
    }
}

/// <summary>
/// Result without a value (for operations that don't return data).
/// </summary>
public sealed class Result
{
    private readonly string? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public string Error =>
        IsFailure
            ? _error ?? "Unknown error"
            : throw new InvalidOperationException("Cannot access error of a successful result");

    private Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result Success()
    {
        return new(true);
    }

    public static Result Failure(string error)
    {
        return new(false, error);
    }

    public Result<T> Map<T>(Func<T> mapper)
    {
        return IsSuccess
            ? Result<T>.Success(mapper())
            : Result<T>.Failure(Error);
    }
}
