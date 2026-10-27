namespace FoodOrderingSystem.Services
{
    public enum OperationResultCode
    {
        Success,
        NotFound,
        InvalidStatus,
        InvalidRequest,
        OutOfStock
    }

    public sealed record OperationResult(
        OperationResultCode Code,
        string? ErrorMessage = null)
    {
        public bool IsSuccess => Code == OperationResultCode.Success;

        public static OperationResult Success() =>
            new(OperationResultCode.Success);

        public static OperationResult NotFound(string message) =>
            new(OperationResultCode.NotFound, message);

        public static OperationResult InvalidStatus(string message) =>
            new(OperationResultCode.InvalidStatus, message);

        public static OperationResult InvalidRequest(string message) =>
            new(OperationResultCode.InvalidRequest, message);

        public static OperationResult OutOfStock(string message) =>
            new(OperationResultCode.OutOfStock, message);
    }
}
