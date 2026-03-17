namespace BudgetWise.Domain.Common.Results;

/// <summary>
/// Representa o resultado de uma operação que retorna um valor do tipo <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">O tipo do dado retornado em caso de sucesso.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Obtém o valor do resultado se a operação foi bem-sucedida.
    /// </summary>
    /// <exception cref="InvalidOperationException">Lançada ao tentar acessar o valor de um resultado de falha.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado de falha.");

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Cria um resultado de sucesso contendo o valor especificado.
    /// </summary>
    /// <param name="value">O valor retornado pela operação.</param>
    public static new Result<T> Success(T value) => new(value, true, Error.None);

    /// <summary>
    /// Cria um resultado de falha contendo o erro especificado.
    /// </summary>
    /// <param name="error">O erro que descreve a causa da falha.</param>
    public static new Result<T> Failure(Error error) => new(default, false, error);

    /// <summary>
    /// Converte implicitamente um valor do tipo <typeparamref name="T"/> em um <see cref="Result{T}"/> de sucesso.
    /// </summary>
    /// <param name="value">O valor a ser convertido.</param>
    public static implicit operator Result<T>(T? value) =>
        value is not null ? Success(value) : Failure(Error.NullValue);

    /// <summary>
    /// Converte implicitamente um <see cref="Error"/> em um <see cref="Result{T}"/> de falha.
    /// </summary>
    /// <param name="error">O erro a ser convertido.</param>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Executa uma função baseada no estado do resultado (Sucesso ou Falha).
    /// Útil para eliminar verificação explícita de propriedades e if/else.
    /// </summary>
    /// <typeparam name="TResult">O tipo de retorno da função executada.</typeparam>
    /// <param name="onSuccess">Função executada se o resultado for sucesso. Recebe o valor de <typeparamref name="T"/>.</param>
    /// <param name="onFailure">Função executada se o resultado for falha. Recebe o <see cref="Error"/>.</param>
    /// <returns>O resultado da função executada.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        if (IsFailure)
        {
            return onFailure(Error);
        }

        return onSuccess(Value);
    }
}