using FluentAssertions;
using NetArchTest.Rules;

namespace BudgetWise.UnitTests.Architecture;

/// <summary>
/// Enforces Clean Architecture dependency rules.
/// Failures here mean a layer boundary was crossed — fix the code, not the test.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(global::BudgetWise.Domain.Common.Abstractions.Entity).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(global::BudgetWise.Application.Interfaces.IUseCase).Assembly;

    private const string InfrastructureNs = "BudgetWise.Infrastructure";
    private const string ApiNs = "BudgetWise.Api";
    private const string DomainNs = "BudgetWise.Domain";

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("BudgetWise.Application")
            .GetResult().IsSuccessful
            .Should().BeTrue("Domain must have zero dependencies on Application.");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult().IsSuccessful
            .Should().BeTrue("Domain must have zero dependencies on Infrastructure.");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult().IsSuccessful
            .Should().BeTrue("Domain must have zero dependencies on Api.");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult().IsSuccessful
            .Should().BeTrue("Application must not reference Infrastructure directly.");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult().IsSuccessful
            .Should().BeTrue("Application must not reference Api.");
    }

    [Fact]
    public void DomainEntities_ShouldInherit_Entity()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace($"{DomainNs}.Entities")
            .Should()
            .Inherit(typeof(global::BudgetWise.Domain.Common.Abstractions.Entity))
            .GetResult().IsSuccessful
            .Should().BeTrue("All types in Entities folder must inherit from Entity.");
    }

    [Fact]
    public void DomainExceptions_ShouldInherit_DomainException()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace($"{DomainNs}.Exceptions")
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("DomainException") // base class itself is excluded
            .Should()
            .Inherit(typeof(global::BudgetWise.Domain.Exceptions.DomainException))
            .GetResult().IsSuccessful
            .Should().BeTrue("All domain exceptions should inherit DomainException.");
    }

    [Fact]
    public void DomainEvents_ShouldImplement_IDomainEvent()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace($"{DomainNs}.Events")
            .Should()
            .ImplementInterface(typeof(global::BudgetWise.Domain.Common.Abstractions.IDomainEvent))
            .GetResult().IsSuccessful
            .Should().BeTrue("All types in Domain.Events must implement IDomainEvent.");
    }
}
