namespace BudgetWise.Domain.Common.Results;

/// <summary>
/// Representa uma falha de domínio de forma estruturada e imutável.
/// </summary>
public record Error
{
    /// <summary>
    /// Código único e legível por máquina para identificar o erro (ex: "User.NotFound").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Descrição legível por humanos detalhando o motivo da falha.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// A categoria do erro, usada para determinar a resposta HTTP adequada.
    /// </summary>
    public ErrorType Type { get; }

    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    /// <summary>
    /// Representa a ausência de erro (Sucesso).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Erro padrão para valores nulos não permitidos.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido é nulo.", ErrorType.Validation);

    /// <summary>
    /// Cria um erro genérico (HTTP 500).
    /// Use quando a falha não se encaixa em validação, busca ou conflito.
    /// </summary>
    /// <param name="code">Código único do erro.</param>
    /// <param name="description">Descrição detalhada.</param>
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    /// <summary>
    /// Cria um erro de validação (HTTP 400).
    /// Use quando os dados fornecidos pelo cliente estão incorretos.
    /// </summary>
    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    /// <summary>
    /// Cria um erro de recurso não encontrado (HTTP 404).
    /// Use quando uma busca por ID ou chave não retorna resultados.
    /// </summary>
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Cria um erro de conflito (HTTP 409).
    /// Use para violações de regras de negócio (ex: item duplicado, saldo insuficiente).
    /// </summary>
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Cria um erro de autenticação (HTTP 401).
    /// Use quando o usuário não enviar um token válido.
    /// </summary>
    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    /// <summary>
    /// Cria um erro de autorização (HTTP 403).
    /// Use quando o usuário está autenticado mas não tem permissão para a operação.
    /// </summary>
    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

}
