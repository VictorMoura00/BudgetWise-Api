using BudgetWise.Application.Identity;
using BudgetWise.Domain.Entities;
using BudgetWise.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    DomainEventDispatcherInterceptor domainEventInterceptor)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<UserCategoryExclusion> UserCategoryExclusions => Set<UserCategoryExclusion>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<SharedExpense> SharedExpenses => Set<SharedExpense>();
    public DbSet<SharedExpenseParticipant> SharedExpenseParticipants => Set<SharedExpenseParticipant>();


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}