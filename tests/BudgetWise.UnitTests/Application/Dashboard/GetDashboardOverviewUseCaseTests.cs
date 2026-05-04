using BudgetWise.Application.Dashboard.UseCases;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Dashboard;

public sealed class GetDashboardOverviewUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly GetDashboardOverviewUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 5, 15);

    public GetDashboardOverviewUseCaseTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero));
        _sut = new GetDashboardOverviewUseCase(_repository, _timeProvider);

        var empty = new TransactionSummaryResult(0m, 0m, 0, 0m);
        var emptyProjection = new MonthProjectionResult(0m, 0m, 0m);
        _repository.GetSummaryForUserAsync(
                UserId,
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns(empty);
        _repository.GetMonthProjectionForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(emptyProjection);
        _repository.GetLargestExpenseForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);
        _repository.GetCategoryComparisonForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetTopCategoryByTypeAsync(UserId, Arg.Any<TransactionType>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((CategoryTotalResult?)null);
        _repository.GetPendingForDueReportAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private static Transaction MakeTransaction(decimal amount, TransactionType type, DateOnly date,
        bool isConfirmed = false, DateOnly? dueDate = null) =>
        Transaction.Create(UserId, "Test", amount, type, date, isConfirmed: isConfirmed, dueDate: dueDate);

    [Fact]
    public async Task ExecuteAsync_DefaultsToCurrentMonth_WhenNoParamsProvided()
    {
        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Period.StartDate.Should().Be(new DateOnly(2026, 5, 1));
        result.Value.Period.EndDate.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task ExecuteAsync_UsesProvidedYearAndMonth()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2025, month: 3);

        result.Value.Period.StartDate.Should().Be(new DateOnly(2025, 3, 1));
        result.Value.Period.EndDate.Should().Be(new DateOnly(2025, 3, 31));
    }

    [Fact]
    public async Task ExecuteAsync_WithStartDateAndEndDate_UsesThem()
    {
        var start = new DateOnly(2026, 3, 10);
        var end = new DateOnly(2026, 4, 20);

        var result = await _sut.ExecuteAsync(UserId, startDate: start, endDate: end);

        result.IsSuccess.Should().BeTrue();
        result.Value.Period.StartDate.Should().Be(start);
        result.Value.Period.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task ExecuteAsync_StartDateAfterEndDate_ReturnsValidationError()
    {
        var result = await _sut.ExecuteAsync(UserId,
            startDate: new DateOnly(2026, 5, 31),
            endDate: new DateOnly(2026, 5, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Dashboard.InvalidDateRange");
    }

    [Fact]
    public async Task ExecuteAsync_StartDateAndEndDateTakePriorityOverYearMonth()
    {
        var start = new DateOnly(2026, 3, 10);
        var end = new DateOnly(2026, 4, 20);

        var result = await _sut.ExecuteAsync(UserId, startDate: start, endDate: end, year: 2025, month: 1);

        result.Value.Period.StartDate.Should().Be(start);
        result.Value.Period.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task ExecuteAsync_FinancialSummary_ReflectsRepositoryData()
    {
        _repository.GetSummaryForUserAsync(
                UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns(new TransactionSummaryResult(1000m, 400m, 2, 200m));
        _repository.GetMonthProjectionForUserAsync(UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), Arg.Any<CancellationToken>())
            .Returns(new MonthProjectionResult(600m, -200m, 400m));

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var fs = result.Value.FinancialSummary;
        fs.TotalIncome.Should().Be(1000m);
        fs.TotalExpense.Should().Be(400m);
        fs.Balance.Should().Be(600m);
        fs.SavingsRate.Should().Be(60m);
        fs.ConfirmedBalance.Should().Be(600m);
        fs.PendingImpact.Should().Be(-200m);
        fs.ProjectedBalance.Should().Be(400m);
    }

    [Fact]
    public async Task ExecuteAsync_SavingsRate_IsNullWhenNoIncome()
    {
        _repository.GetSummaryForUserAsync(
                UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns(new TransactionSummaryResult(0m, 500m, 0, 0m));

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.FinancialSummary.SavingsRate.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithTypeFilter_PassedOnlyToCurrentSummary()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5, type: TransactionType.Expense);

        result.IsSuccess.Should().BeTrue();

        await _repository.Received(1).GetSummaryForUserAsync(
            UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
            TransactionType.Expense, null, null, null, null,
            Arg.Any<CancellationToken>());

        // previous month summary must NOT receive the type filter
        await _repository.Received(1).GetSummaryForUserAsync(
            UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30),
            null, null, null, null, null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PendingSummary_CorrectlyGroupsByDueDate()
    {
        var pending = new List<Transaction>
        {
            MakeTransaction(100m, TransactionType.Expense, Today, dueDate: Today.AddDays(-1)),
            MakeTransaction(200m, TransactionType.Expense, Today, dueDate: Today),
            MakeTransaction(300m, TransactionType.Expense, Today, dueDate: Today.AddDays(5)),
            MakeTransaction(400m, TransactionType.Expense, Today, dueDate: Today.AddDays(15)),
            MakeTransaction(500m, TransactionType.Expense, Today, dueDate: Today.AddDays(45)),
            MakeTransaction(600m, TransactionType.Expense, Today, dueDate: null),
        };
        _repository.GetPendingForDueReportAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(pending);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var ps = result.Value.PendingSummary;
        ps.TotalPendingCount.Should().Be(6);
        ps.TotalPendingAmount.Should().Be(2100m);
        ps.Overdue.Count.Should().Be(1);
        ps.Overdue.TotalAmount.Should().Be(100m);
        ps.DueToday.Count.Should().Be(1);
        ps.DueToday.TotalAmount.Should().Be(200m);
        ps.DueNext7Days.Count.Should().Be(1);
        ps.DueNext7Days.TotalAmount.Should().Be(300m);
        ps.DueNext30Days.Count.Should().Be(1);
        ps.DueNext30Days.TotalAmount.Should().Be(400m);
        ps.Future.Count.Should().Be(1);
        ps.Future.TotalAmount.Should().Be(500m);
        ps.WithoutDueDate.Count.Should().Be(1);
        ps.WithoutDueDate.TotalAmount.Should().Be(600m);
    }

    [Fact]
    public async Task ExecuteAsync_PendingSummary_IsZeroed_WhenNoPendingTransactions()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var ps = result.Value.PendingSummary;
        ps.TotalPendingCount.Should().Be(0);
        ps.TotalPendingAmount.Should().Be(0m);
        ps.Overdue.Count.Should().Be(0);
        ps.Future.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_TopExpense_IsFromCategoryComparison()
    {
        _repository.GetCategoryComparisonForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([
                new CategoryComparisonResult(Guid.NewGuid(), "Food", "#FF0000", 500m, 400m),
                new CategoryComparisonResult(Guid.NewGuid(), "Transport", "#00FF00", 200m, 100m),
            ]);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var top = result.Value.CategoryHighlights.TopExpenseCategory;
        top.Should().NotBeNull();
        top!.CategoryName.Should().Be("Food");
        top.Amount.Should().Be(500m);
        top.ChangePercent.Should().Be(25m);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_TopExpense_IsNull_WhenNoCategoryData()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.CategoryHighlights.TopExpenseCategory.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_FastestGrowing_PicksHighestGrowthRate()
    {
        _repository.GetCategoryComparisonForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([
                new CategoryComparisonResult(Guid.NewGuid(), "Food", null, 500m, 400m),
                new CategoryComparisonResult(Guid.NewGuid(), "Leisure", null, 300m, 100m),
            ]);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var fg = result.Value.CategoryHighlights.FastestGrowingCategory;
        fg.Should().NotBeNull();
        fg!.CategoryName.Should().Be("Leisure");
        fg.ChangePercent.Should().Be(200m);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_FastestGrowing_IsNull_WhenNoGrowth()
    {
        _repository.GetCategoryComparisonForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([
                new CategoryComparisonResult(Guid.NewGuid(), "Food", null, 500m, 600m),
            ]);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.CategoryHighlights.FastestGrowingCategory.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_TopIncome_ReflectsRepository()
    {
        var catId = Guid.NewGuid();
        _repository.GetTopCategoryByTypeAsync(UserId, TransactionType.Income, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new CategoryTotalResult(catId, "Salary", "#AABBCC", 3000m));

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var topIncome = result.Value.CategoryHighlights.TopIncomeCategory;
        topIncome.Should().NotBeNull();
        topIncome!.CategoryName.Should().Be("Salary");
        topIncome.Amount.Should().Be(3000m);
        topIncome.ChangePercent.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryHighlights_TotalExpenseCategories_CountsCurrentPeriodOnly()
    {
        _repository.GetCategoryComparisonForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([
                new CategoryComparisonResult(Guid.NewGuid(), "A", null, 100m, 0m),
                new CategoryComparisonResult(Guid.NewGuid(), "B", null, 0m, 200m),
                new CategoryComparisonResult(Guid.NewGuid(), "C", null, 50m, 0m),
            ]);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.CategoryHighlights.TotalExpenseCategories.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_LargestExpense_IsNull_WhenNoExpenses()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.LargestExpense.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_LargestExpense_ReflectsRepositoryData()
    {
        var t = MakeTransaction(999m, TransactionType.Expense, new DateOnly(2026, 5, 10), dueDate: new DateOnly(2026, 5, 20));
        _repository.GetLargestExpenseForUserAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(t);

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var le = result.Value.LargestExpense;
        le.Should().NotBeNull();
        le!.Amount.Should().Be(999m);
        le.Description.Should().Be("Test");
        le.DueDate.Should().Be(new DateOnly(2026, 5, 20));
    }

    [Fact]
    public async Task ExecuteAsync_MonthlyComparison_ComputesDiffsCorrectly()
    {
        _repository.GetSummaryForUserAsync(
                UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns(new TransactionSummaryResult(1200m, 800m, 0, 0m));
        _repository.GetSummaryForUserAsync(
                UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30),
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns(new TransactionSummaryResult(1000m, 600m, 0, 0m));

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        var mc = result.Value.MonthlyComparison;
        mc.PreviousIncome.Should().Be(1000m);
        mc.PreviousExpense.Should().Be(600m);
        mc.IncomeDiff.Should().Be(200m);
        mc.IncomeDiffPercent.Should().Be(20m);
        mc.ExpenseDiff.Should().Be(200m);
        mc.ExpenseDiffPercent.Should().BeApproximately(33.33m, 0.01m);
    }

    [Fact]
    public async Task ExecuteAsync_MonthlyComparison_PercentIsNull_WhenNoPreviousData()
    {
        _repository.GetSummaryForUserAsync(
                UserId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31),
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns(new TransactionSummaryResult(500m, 300m, 0, 0m));

        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.Value.MonthlyComparison.IncomeDiffPercent.Should().BeNull();
        result.Value.MonthlyComparison.ExpenseDiffPercent.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ReturnsSuccessWithZeroedValues()
    {
        var result = await _sut.ExecuteAsync(UserId, year: 2026, month: 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinancialSummary.TotalIncome.Should().Be(0m);
        result.Value.PendingSummary.TotalPendingCount.Should().Be(0);
        result.Value.LargestExpense.Should().BeNull();
        result.Value.CategoryHighlights.TopExpenseCategory.Should().BeNull();
        result.Value.CategoryHighlights.TopIncomeCategory.Should().BeNull();
    }
}
