namespace DataPlane.Sdk.Core.Domain;

/// <summary>
///     Represents a criterion used for filtering or comparison operations, consisting of a left operand,
///     an operator, and an optional right operand.
/// </summary>
/// <param name="operandLeft">The left operand of the criterion.</param>
/// <param name="operator">The operator used for comparison or evaluation.</param>
/// <param name="operandRight">The optional right operand of the criterion.</param>
public class Criterion(object operandLeft, string @operator, object? operandRight = null)
{
    public object OperandLeft { get; } = operandLeft;
    public string Operator { get; } = @operator;
    public object? OperandRight { get; } = operandRight;
}
