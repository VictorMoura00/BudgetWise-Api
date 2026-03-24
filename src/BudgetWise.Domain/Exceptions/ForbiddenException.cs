namespace BudgetWise.Domain.Exceptions;

public class ForbiddenException(string message) : DomainException(message);
