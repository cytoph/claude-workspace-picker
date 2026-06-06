using System.Diagnostics.CodeAnalysis;

namespace ClaudeWorkspacePicker.Models;

static class Result
{
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Error<T>(string errorMessage) => new([errorMessage]);
    public static Result<T> Errors<T>(IReadOnlyList<string> errors) => new(errors);
}

sealed class Result<T>
{
    public T? Value { get; }

    public IReadOnlyList<string>? Errors { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsSuccess { get; }

    public Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    public Result(IReadOnlyList<string> errors)
    {
        Errors = errors;
        IsSuccess = false;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = Value;
        return IsSuccess;
    }

    public static implicit operator Result<T>(T value) => new(value);
}
