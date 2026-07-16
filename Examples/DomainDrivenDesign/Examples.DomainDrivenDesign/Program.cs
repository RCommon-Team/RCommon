using Examples.DomainDrivenDesign;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;
using RCommon.Security.Claims;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        logging.AddFilter("RCommon.EventHandling", LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        // Registered before WithPersistence<EFCorePersistenceBuilder> so its TryAddTransient<ITenantIdAccessor,
        // NullTenantIdAccessor> below is a no-op -- see multi-tenancy/overview.mdx.
        services.AddSingleton<ITenantIdAccessor, FixedTenantIdAccessor>();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // UnitOfWork needs IGuidGenerator to stamp its TransactionId.
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase("ddd-recipe-example"));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
            })
            .WithEventHandling<InMemoryEventBusBuilder>(eh =>
            {
                eh.AddSubscriber<TeamMemberAddedEvent, TeamMemberAddedEventHandler>();
            });
    })
    .Build();

Console.WriteLine("Example Starting");

using (var schemaScope = host.Services.CreateScope())
{
    await schemaScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}

Guid teamId;

Console.WriteLine("--- Create the aggregate and add its first member ---");
using (var scope = host.Services.CreateScope())
{
    var teams = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Team, Guid>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    var team = new Team("Platform Engineering");
    team.AddMember(Guid.NewGuid(), EmailAddress.Create("ada@example.com"), TeamRole.Lead);
    teamId = team.Id;

    using var uow = unitOfWorkFactory.Create();
    // EF Core's AddAsync cascades new children automatically (provider-specific -- see
    // persistence/aggregate-repository.mdx's provider comparison table).
    await teams.AddAsync(team);
    await uow.CommitAsync(); // dispatches TeamMemberAddedEvent to the subscriber above

    Console.WriteLine($"Created team {teamId} with {team.Memberships.Count} member(s). Tenant: {team.TenantId}");
}

Console.WriteLine();
Console.WriteLine("--- Pattern 1: load, mutate, update in one scope (recommended) ---");
using (var scope = host.Services.CreateScope())
{
    var teams = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Team, Guid>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    var team = await teams.Include(t => t.Memberships).GetByIdAsync(teamId);
    team!.AddMember(Guid.NewGuid(), EmailAddress.Create("grace@example.com"), TeamRole.Member);

    using var uow = unitOfWorkFactory.Create();
    await teams.UpdateAsync(team); // the new membership is inserted; the domain event is queued
    await uow.CommitAsync();       // dispatched here

    Console.WriteLine($"Team now has {team.Memberships.Count} member(s).");
}

Console.WriteLine();
Console.WriteLine("--- Pattern 2: cross-scope mutation (e.g. a background worker) ---");
using (var scope = host.Services.CreateScope())
{
    var teams = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Team, Guid>>();
    var memberships = scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TeamMembership>>();
    var writableMemberships = scope.ServiceProvider.GetRequiredService<IWriteOnlyRepository<TeamMembership>>();
    var eventTracker = scope.ServiceProvider.GetRequiredService<IEntityEventTracker>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    // Simulates a team payload built in a different scope/process than the one that will commit.
    var team = await teams.GetByIdAsync(teamId);
    team!.AddMember(Guid.NewGuid(), EmailAddress.Create("alan@example.com"), TeamRole.Member);

    using var uow = unitOfWorkFactory.Create();
    // UpdateAsync is deliberately NOT called here -- persist the new child through its own
    // repository instead (this DbContext cannot tell "genuinely new" apart from "existing but
    // never loaded in this scope").
    await writableMemberships.AddAsync(team.Memberships.Last());

    // Required: nothing else registers `team` for event dispatch without UpdateAsync -- this
    // reproduces the same tracking UpdateAsync would have done, by hand.
    eventTracker.AddEntity(team);
    await uow.CommitAsync();

    Console.WriteLine($"Persisted a new membership via its own repository; team subscriber invocations so far: {TeamMemberAddedEventHandler.HandledCount}");
}

Console.WriteLine();
Console.WriteLine("--- Soft delete ---");
using (var scope = host.Services.CreateScope())
{
    var teams = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Team, Guid>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    var team = await teams.GetByIdAsync(teamId);

    using var uow = unitOfWorkFactory.Create();
    await teams.DeleteAsync(team!); // Team implements ISoftDelete -- sets IsDeleted rather than removing the row
    await uow.CommitAsync();

    var stillExists = await teams.ExistsAsync(teamId);
    Console.WriteLine($"Team.IsDeleted: {team!.IsDeleted}. ExistsAsync still reports: {stillExists}");
}

Console.WriteLine();
Console.WriteLine($"Total subscriber invocations: {TeamMemberAddedEventHandler.HandledCount}");
Console.WriteLine("Example Complete");
