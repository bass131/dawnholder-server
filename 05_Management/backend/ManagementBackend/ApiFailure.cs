namespace Dawnholder.Management.Backend;

internal sealed class ApiFailure(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
