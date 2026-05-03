using BudgetWise.Application.Reports.UseCases;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Reports;

public sealed class GetDueTransactionsReportUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly GetDueTransactionsReportUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 5, 3);

    public GetDueTransactionsReportUseCaseTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));
        _sut = new GetDueTransactionsReportUseCase(_repository, _timeProvider);
    }

    private static Transaction MakePending(decimal amount, DateOnly? dueDate) =>
        Transaction.Create(UserId, "Despesa", amount, TransactionType.Expense,
            Today, dueDate: dueDate, isConfirmed: false);

    private void Setup(IReadOnlyList<Transaction> items)
    {
        _repository.GetPendingForDueReportAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(items);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoTransactions_ReturnsZeroedGroups()
    {
        Setup([]);

        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        var r = result.Value;
        r.ReportDate.Should().Be(Today);
        r.TotalPendingCount.Should().Be(0);
        r.TotalPendingAmount.Should().Be(0m);
        r.Overdue.Count.Should().Be(0);
        r.DueToday.Count.Should().Be(0);
        r.DueNext7Days.Count.Should().Be(0);
        r.DueNext30Days.Count.Should().Be(0);
        r.Future.Count.Should().Be(0);
        r.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_UsesTodayFromTimeProvider()
    {
        Setup([]);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.ReportDate.Should().Be(Today);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesOverdueCorrectly()
    {
        Setup([
            MakePending(100m, Today.AddDays(-1)),
            MakePending(200m, Today.AddDays(-5)),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.Overdue.Count.Should().Be(2);
        r.Overdue.TotalAmount.Should().Be(300m);
        r.DueToday.Count.Should().Be(0);
        r.DueNext7Days.Count.Should().Be(0);
        r.DueNext30Days.Count.Should().Be(0);
        r.Future.Count.Should().Be(0);
        r.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesDueTodayCorrectly()
    {
        Setup([MakePending(500m, Today)]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.DueToday.Count.Should().Be(1);
        r.DueToday.TotalAmount.Should().Be(500m);
        r.Overdue.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesDueNext7DaysCorrectly()
    {
        Setup([
            MakePending(150m, Today.AddDays(1)),
            MakePending(250m, Today.AddDays(7)),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.DueNext7Days.Count.Should().Be(2);
        r.DueNext7Days.TotalAmount.Should().Be(400m);
        r.DueNext30Days.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesDueNext30DaysCorrectly()
    {
        Setup([
            MakePending(300m, Today.AddDays(8)),
            MakePending(100m, Today.AddDays(30)),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.DueNext30Days.Count.Should().Be(2);
        r.DueNext30Days.TotalAmount.Should().Be(400m);
        r.DueNext7Days.Count.Should().Be(0);
        r.Future.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesFutureCorrectly()
    {
        Setup([
            MakePending(700m, Today.AddDays(31)),
            MakePending(300m, Today.AddDays(90)),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.Future.Count.Should().Be(2);
        r.Future.TotalAmount.Should().Be(1000m);
        r.DueNext30Days.Count.Should().Be(0);
        r.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_BoundaryDayPlus31_AppearsInFuture()
    {
        Setup([MakePending(50m, Today.AddDays(31))]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.Future.Count.Should().Be(1);
        r.Future.TotalAmount.Should().Be(50m);
        r.DueNext30Days.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ClassifiesWithoutDueDateCorrectly()
    {
        Setup([
            MakePending(80m, null),
            MakePending(120m, null),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.WithoutDueDate.Count.Should().Be(2);
        r.WithoutDueDate.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllGroups_DistributesCorrectly()
    {
        Setup([
            MakePending(100m, Today.AddDays(-2)),
            MakePending(200m, Today),
            MakePending(300m, Today.AddDays(3)),
            MakePending(400m, Today.AddDays(15)),
            MakePending(600m, Today.AddDays(45)),
            MakePending(500m, null),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        var r = result.Value;
        r.Overdue.Count.Should().Be(1);
        r.DueToday.Count.Should().Be(1);
        r.DueNext7Days.Count.Should().Be(1);
        r.DueNext30Days.Count.Should().Be(1);
        r.Future.Count.Should().Be(1);
        r.WithoutDueDate.Count.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_TotalPendingCount_SumsAllGroups()
    {
        Setup([
            MakePending(100m, Today.AddDays(-1)),
            MakePending(200m, Today),
            MakePending(300m, Today.AddDays(5)),
            MakePending(400m, Today.AddDays(20)),
            MakePending(500m, Today.AddDays(60)),
            MakePending(600m, null),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.TotalPendingCount.Should().Be(6);
    }

    [Fact]
    public async Task ExecuteAsync_TotalPendingAmount_SumsAllGroups()
    {
        Setup([
            MakePending(100m, Today.AddDays(-1)),
            MakePending(200m, Today),
            MakePending(300m, Today.AddDays(5)),
            MakePending(400m, Today.AddDays(20)),
            MakePending(500m, Today.AddDays(60)),
            MakePending(600m, null),
        ]);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.TotalPendingAmount.Should().Be(2100m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeItemsFalse_AllGroupItemsAreEmpty()
    {
        Setup([
            MakePending(100m, Today),
            MakePending(200m, Today.AddDays(60)),
        ]);

        var result = await _sut.ExecuteAsync(UserId, includeItems: false);

        var r = result.Value;
        r.DueToday.Items.Should().BeEmpty();
        r.Future.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeItemsTrue_ItemsArePopulated()
    {
        Setup([MakePending(100m, Today)]);

        var result = await _sut.ExecuteAsync(UserId, includeItems: true);

        result.Value.DueToday.Items.Should().HaveCount(1);
        result.Value.DueToday.Items[0].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task ExecuteAsync_Future_WhenIncludeItemsTrue_ItemsArePopulated()
    {
        Setup([MakePending(750m, Today.AddDays(45))]);

        var result = await _sut.ExecuteAsync(UserId, includeItems: true);

        result.Value.Future.Items.Should().HaveCount(1);
        result.Value.Future.Items[0].Amount.Should().Be(750m);
    }

    [Fact]
    public async Task ExecuteAsync_ItemsAreCappedAt10PerGroup()
    {
        var many = Enumerable.Range(1, 15)
            .Select(i => MakePending(10m * i, Today))
            .ToList();

        Setup(many);

        var result = await _sut.ExecuteAsync(UserId, includeItems: true);

        result.Value.DueToday.Count.Should().Be(15);
        result.Value.DueToday.TotalAmount.Should().Be(many.Sum(t => t.Amount));
        result.Value.DueToday.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task ExecuteAsync_Future_ItemsAreCappedAt10()
    {
        var many = Enumerable.Range(1, 15)
            .Select(i => MakePending(10m * i, Today.AddDays(31 + i)))
            .ToList();

        Setup(many);

        var result = await _sut.ExecuteAsync(UserId, includeItems: true);

        result.Value.Future.Count.Should().Be(15);
        result.Value.Future.Items.Should().HaveCount(10);
    }
}
