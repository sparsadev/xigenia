namespace Xigenia.Shared;

public enum Quadrant
{
    Alcanzar,
    Proteger,
    Eliminar,
    Evitar
}

public enum FinancialAction
{
    Comprar,
    Vender,
    Pagar,
    Cobrar
}

public sealed record TransactionSummary(
    string Title,
    decimal Amount,
    Quadrant Quadrant,
    FinancialAction Action);
