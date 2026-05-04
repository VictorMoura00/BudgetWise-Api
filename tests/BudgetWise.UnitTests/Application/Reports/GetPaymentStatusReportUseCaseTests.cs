using BudgetWise.Application.Reports.UseCases;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Reports;

public sealed class GetPaymentStatusReportUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly GetPaymentStatusReportUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 5, 15);

    public GetPaymentStatusReportUseCaseTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero));
        _sut = new GetPaymentStatusReportUseCase(_repository, _timeProvider);

        _repository.GetTransactionsForPaymentStatusAsync(
                UserId,
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private static Transaction MakeTransaction(decimal amount, bool isConfirmed, DateOnly? dueDate = null) =>
        Transaction.Create(UserId, "Teste", amount, TransactionType.Expense, Today,
            isConfirmed: isConfirmed, dueDate: dueDate);

    [Fact]
    public async Task ExecuteAsync_DefaultsToCurrentMonth_WhenNoDatesProvided()
    {
        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(new DateOnly(2026, 5, 1));
        result.Value.EndDate.Should().Be(new DateOnly(2026, 5, 31));
        result.Value.ReportDate.Should().Be(Today);
    }

    [Fact]
    public async Task ExecuteAsync_StartDateAfterEndDate_ReturnsValidationError()
    {
        var result = await _sut.ExecuteAsync(UserId,
            startDate: new DateOnly(2026, 5, 31),
            endDate: new DateOnly(2026, 5, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentStatus.InvalidDateRange");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoTransactions_ReturnsZeroedGroups()
    {
        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        var r = result.Value;
        r.Confirmed.Count.Should().Be(0);
        r.Pending.Count.Should().Be(0);
        r.Overdue.Count.Should().Be(0);
        r.DueToday.Count.Should().Be(0);
        r.DueNext7Days.Count.Should().Be(0);
        r.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_CorrectlyGroupsByConfirmedAndPending()
    {
        var transactions = new List<Transaction>
        {
            MakeTransaction(100m, isConfirmed: true),
            MakeTransaction(200m, isConfirmed: true),
            MakeTransaction(50m, isConfirmed: false),
        };
        _repository.GetTransactionsForPaymentStatusAsync(
                UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.Confirmed.Count.Should().Be(2);
        result.Value.Confirmed.TotalAmount.Should().Be(300m);
        result.Value.Pending.Count.Should().Be(1);
        result.Value.Pending.TotalAmount.Should().Be(50m);
    }

    [Fact]
    public async Task ExecuteAsync_CorrectlyGroupsPendingByDueDate()
    {
        var transactions = new List<Transaction>
        {
            MakeTransaction(100m, isConfirmed: false, dueDate: Today.AddDays(-1)),  // overdue
            MakeTransaction(200m, isConfirmed: false, dueDate: Today),              // due today
            MakeTransaction(300m, isConfirmed: false, dueDate: Today.AddDays(5)),   // due next 7
            MakeTransaction(400m, isConfirmed: false, dueDate: null),               // without due date
        };
        _repository.GetTransactionsForPaymentStatusAsync(
                UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.Overdue.Count.Should().Be(1);
        result.Value.Overdue.TotalAmount.Should().Be(100m);
        result.Value.DueToday.Count.Should().Be(1);
        result.Value.DueToday.TotalAmount.Should().Be(200m);
        result.Value.DueNext7Days.Count.Should().Be(1);
        result.Value.DueNext7Days.TotalAmount.Should().Be(300m);
        result.Value.WithoutDueDate.Count.Should().Be(1);
        result.Value.WithoutDueDate.TotalAmount.Should().Be(400m);
    }

    [Fact]
    public async Task ExecuteAsync_UsesTimeProvider_ForTodayGrouping()
    {
        var future = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.GetUtcNow().Returns(future);

        var transactions = new List<Transaction>
        {
            MakeTransaction(100m, isConfirmed: false, dueDate: Today),
        };
        _repository.GetTransactionsForPaymentStatusAsync(
                UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _sut.ExecuteAsync(UserId);

        // Today (2026-05-15) is overdue relative to future date (2026-06-01)
        result.Value.Overdue.Count.Should().Be(1);
    }
}
