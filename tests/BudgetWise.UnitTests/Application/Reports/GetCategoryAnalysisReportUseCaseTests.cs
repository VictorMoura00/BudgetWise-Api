using BudgetWise.Application.Reports.UseCases;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Reports;

public sealed class GetCategoryAnalysisReportUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly GetCategoryAnalysisReportUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 5, 15);

    public GetCategoryAnalysisReportUseCaseTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero));
        _sut = new GetCategoryAnalysisReportUseCase(_repository, _timeProvider);

        _repository.GetCategoryAnalysisForUserAsync(
                UserId,
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultsToCurrentMonth_WhenNoDatesProvided()
    {
        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(new DateOnly(2026, 5, 1));
        result.Value.EndDate.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task ExecuteAsync_UsesProvidedDates()
    {
        var start = new DateOnly(2026, 3, 1);
        var end = new DateOnly(2026, 3, 31);

        var result = await _sut.ExecuteAsync(UserId, startDate: start, endDate: end);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task ExecuteAsync_StartDateAfterEndDate_ReturnsValidationError()
    {
        var result = await _sut.ExecuteAsync(UserId,
            startDate: new DateOnly(2026, 5, 31),
            endDate: new DateOnly(2026, 5, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CategoryAnalysis.InvalidDateRange");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoResults_ReturnsEmptyItems()
    {
        var result = await _sut.ExecuteAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalAmount.Should().Be(0m);
        result.Value.TotalTransactions.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesCorrectTotalAndPercentage()
    {
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();
        _repository.GetCategoryAnalysisForUserAsync(
                UserId,
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([
                new CategoryAnalysisResult(catId1, "Alimentação", "#FF0000", null, 600m, 10),
                new CategoryAnalysisResult(catId2, "Transporte", "#00FF00", null, 400m, 5),
            ]);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.TotalAmount.Should().Be(1000m);
        result.Value.TotalTransactions.Should().Be(15);
        result.Value.Items.Should().HaveCount(2);

        var food = result.Value.Items.First(i => i.CategoryName == "Alimentação");
        food.Percentage.Should().Be(60m);

        var transport = result.Value.Items.First(i => i.CategoryName == "Transporte");
        transport.Percentage.Should().Be(40m);
    }

    [Fact]
    public async Task ExecuteAsync_NullCategoryId_ShownAsSemCategoria()
    {
        _repository.GetCategoryAnalysisForUserAsync(
                UserId,
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([
                new CategoryAnalysisResult(null, "Sem categoria", null, null, 300m, 3),
            ]);

        var result = await _sut.ExecuteAsync(UserId);

        result.Value.Items.Should().ContainSingle(i => i.CategoryName == "Sem categoria" && i.CategoryId == null);
    }

    [Fact]
    public async Task ExecuteAsync_PassesFiltersToRepository()
    {
        await _sut.ExecuteAsync(UserId,
            type: TransactionType.Expense,
            isConfirmed: true,
            paymentMethod: PaymentMethod.Pix);

        await _repository.Received(1).GetCategoryAnalysisForUserAsync(
            UserId,
            Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
            TransactionType.Expense, true, null, null, PaymentMethod.Pix,
            Arg.Any<CancellationToken>());
    }
}
