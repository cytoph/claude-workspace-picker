using System.Diagnostics.CodeAnalysis;

namespace ClaudeWorkspacePicker.Models;

static class Result
{
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Error<T>(string errorMessage) => new(errorMessage);
}

sealed class Result<T>
{
    public T? Value { get; }

    public string? ErrorMessage { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public bool IsSuccess { get; }

    public Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    public Result(string errorMessage)
    {
        ErrorMessage = errorMessage;
        IsSuccess = false;
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = Value;
        return IsSuccess;
    }

    public static implicit operator Result<T>(T value) => new(value);
}
