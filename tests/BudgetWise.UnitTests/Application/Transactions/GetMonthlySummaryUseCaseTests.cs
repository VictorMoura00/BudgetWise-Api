using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class GetMonthlySummaryUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly GetMonthlySummaryUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    public GetMonthlySummaryUseCaseTests()
    {
        _sut = new GetMonthlySummaryUseCase(_repository);

        _repository.GetMonthlySummaryForUserAsync(
                UserId,
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitDates_ReturnsGroupedByMonth()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 3, 31);

        _repository.GetMonthlySummaryForUserAsync(
                UserId, start, end,
                null, null, null, null, null,
                Arg.Any<CancellationToken>())
            .Returns([
                new MonthlySummaryResult(2026, 1, 1000m, 500m),
                new MonthlySummaryResult(2026, 2, 800m, 300m),
                new MonthlySummaryResult(2026, 3, 1200m, 700m),
            ]);

        var result = await _sut.ExecuteAsync(UserId, start, end);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Month.Should().Be("2026-01");
        result.Value[0].Income.Should().Be(1000m);
        result.Value[1].Month.Should().Be("2026-02");
        result.Value[2].Month.Should().Be("2026-03");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ReturnsEmptyList()
    {
        var result = await _sut.ExecuteAsync(UserId, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_PassesFiltersToRepository()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 3, 31);

        await _sut.ExecuteAsync(UserId, start, end,
            type: TransactionType.Expense,
            isConfirmed: true,
            paymentMethod: PaymentMethod.Pix);

        await _repository.Received(1).GetMonthlySummaryForUserAsync(
            UserId, start, end,
            TransactionType.Expense, true, null, null, PaymentMethod.Pix,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MonthIsFormattedAsYYYYMM()
    {
        _repository.GetMonthlySummaryForUserAsync(
                UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<TransactionType?>(), Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<PaymentMethod?>(),
                Arg.Any<CancellationToken>())
            .Returns([new MonthlySummaryResult(2026, 3, 100m, 50m)]);

        var result = await _sut.ExecuteAsync(UserId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        result.Value[0].Month.Should().Be("2026-03");
    }
}
