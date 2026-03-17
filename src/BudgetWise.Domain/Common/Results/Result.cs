using System;
using System.Diagnostics;

namespace BudgetWise.Domain.Common.Results;

/// <summary>
/// Representa o resultado de uma operação que não retorna valor (void).
/// Indica se a operação foi bem-sucedida ou falhou, contendo o erro associado em caso de falha.
/// </summary>
public class Result
{
    /// <summary>
    /// Indica se a operação foi concluída com sucesso.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica se a operação falhou.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Obtém o erro associado à falha. Retorna <see cref="Error.None"/> se for sucesso.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Result"/>.
    /// </summary>
    /// <param name="isSuccess">Estado de sucesso da operação.</param>
    /// <param name="error">Erro associado (deve ser <see cref="Error.None"/> se sucesso).</param>
    protected Result(bool isSuccess, Error error)
    {
        Debug.Assert(isSuccess && error == Error.None || !isSuccess && error != Error.None,
            "Estado de sucesso/falha incompatível com o erro fornecido.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Cria uma instância representando uma operação bem-sucedida.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Cria uma instância representando uma falha.
    /// </summary>
    /// <param name="error">O erro que descreve a falha.</param>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Cria um resultado genérico de sucesso contendo um valor.
    /// </summary>
    /// <typeparam name="T">O tipo do valor retornado.</typeparam>
    /// <param name="value">O valor a ser retornado.</param>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Cria um resultado genérico de falha.
    /// </summary>
    /// <typeparam name="T">O tipo do valor esperado.</typeparam>
    /// <param name="error">O erro que descreve a falha.</param>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}