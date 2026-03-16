using System;
using System.Collections.Generic;
using System.Text;

namespace BudgetWise.Domain.Exceptions;

public class ForbiddenException(string message) : Exception(message);
