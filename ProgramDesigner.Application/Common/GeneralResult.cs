namespace ProgramDesigner.Application.Common;

public class GeneralResult
{
    public bool IsSuccess { get; init; }
    public ResultStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; init; }

    protected GeneralResult() { }

    public static GeneralResult Success(string message = "Operation completed successfully.")
        => new() { IsSuccess = true, Status = ResultStatus.Success, Message = message };

    public static GeneralResult Failure(string message = "Operation failed.")
        => new() { IsSuccess = false, Status = ResultStatus.Failure, Message = message };

    public static GeneralResult NotFound(string message = "The requested resource was not found.")
        => new() { IsSuccess = false, Status = ResultStatus.NotFound, Message = message };

    public static GeneralResult ValidationFailure(
        Dictionary<string, string[]> errors,
        string message = "Validation failed.")
        => new() { IsSuccess = false, Status = ResultStatus.ValidationError, Message = message, Errors = errors };
}

public sealed class GeneralResult<T> : GeneralResult
{
    public T? Data { get; init; }

    private GeneralResult() { }

    public static GeneralResult<T> Success(T data, string message = "Operation completed successfully.")
    {
        ArgumentNullException.ThrowIfNull(data);
        return new() { IsSuccess = true, Status = ResultStatus.Success, Message = message, Data = data };
    }

    public new static GeneralResult<T> Success(string message = "Operation completed successfully.")
        => new() { IsSuccess = true, Status = ResultStatus.Success, Message = message };

    public new static GeneralResult<T> Failure(string message = "Operation failed.")
        => new() { IsSuccess = false, Status = ResultStatus.Failure, Message = message };

    public new static GeneralResult<T> NotFound(string message = "The requested resource was not found.")
        => new() { IsSuccess = false, Status = ResultStatus.NotFound, Message = message };

    public new static GeneralResult<T> ValidationFailure(
        Dictionary<string, string[]> errors,
        string message = "Validation failed.")
        => new() { IsSuccess = false, Status = ResultStatus.ValidationError, Message = message, Errors = errors };
}

public enum ResultStatus
{
    Success,
    Failure,
    NotFound,
    ValidationError
}
