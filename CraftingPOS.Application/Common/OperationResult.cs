namespace CraftingPOS.Application.Common;

/// <summary>
/// Reusable success/failure result for Create/Update/Delete operations.
/// Every future module (Products, Suppliers, Customers, Purchases, Sales...)
/// should use this instead of inventing its own result type.
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static OperationResult Ok() => new() { Success = true };
    public static OperationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Same as OperationResult, but carries a return value on success (e.g. the created entity's DTO).
/// </summary>
public class OperationResult<T>
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }

    public static OperationResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static OperationResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}