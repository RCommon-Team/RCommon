# RCommon Application Framework

## Overview
RCommon was originally born as the (now abandoned) [NCommon](https://github.com/riteshrao/ncommon "NCommon") project but was resurrected out of the need to generate a productive, yet a relatively sound (architecturally speaking) application. Architectural patterns are used to implement some of the most commonly used tools in the .NET 6 stack. The primary goals of this framework are:
1. Future proofing applications against changing architectural or infrastructure needs.
2. Solve common problems under the presentation layer. Presentation frameworks are something else entirely. We try to keep everything nice under the hood. Cross cutting concerns, persistence strategies, transaction management, validation, business rules, exception management, and logging is where we want to shine.
3. Code testability. We try to limit the "magic" used. Things like dependency injection are used but in a very straightforward manner. Unit tests, and integration tests should be implemented to the highest degree possible. Afterall, we want the applications you build on top of this to work :) 
4. Last but not least - open source forever. 

We track bugs, enhancement requests, new feature requests, and general issues on [GitHub Issues](https://github.com/Reactor2Team/RCommon/issues "GitHub Issues") and are very responsive. General "how to" and community support should be managed on [Stack Overflow](https://stackoverflow.com/questions/tagged/rcommon "Stack Overflow"). 

## Repository Pattern & Object Persistence
RCommon provides a common abstraction and underlying strategies/implementations for a variety of repositories including Dapper, Entity Framework Core, and NHibernate making RCommon one of the most versatile persistence Repositories available. Each implementation is unit tested and integration tested in web, single threaded, and multithreaded hosting environments. "Full featured" object access strategies such as EFCore and NHibernate come packaged with the ability to eager load additionally entities into the IQueryable expression map.
```csharp
if (includeDetails)
    {
        _pageRepository.EagerlyWith(y => y.ParentPage);
        _pageRepository.EagerlyWith(y => y.Status);
    }
    cmd.DataResult = await _pageRepository.FindAsync(x => x.SiteId == siteId);
```


## Unit of Work & Transaction Management
The unit of work (UoW) pattern is loosely coupled from all persistence strategies but provides granular control over transactions using ACID properties. Transactions are currently implemented through the UnitOfWork and the UnitOfWorkManager which provides a wrapper for TransactionScope. [As .NET Core does not support transactions across multiple databases or providers](https://github.com/dotnet/runtime/issues/715), we've had to rely on distributed events and messaging patterns such as "outbox" to implement support for these types of transactions. 
```csharp
await using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
{
    var domainData = await _crudDomainService.CreateAsync(entity); // Perform the work

    if (domainData.HasException)
    {

        throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
    }
    else
    {
        result.DataResult = domainData.DataResult; // Set the data to return to the DTO
    }

    scope.Commit(); // Commit the transaction
}
```


## Dependency Injection
Rcommon provides a common Dependency Injection container adapter that may be used with any of the major DI containers available including, Castle Windsor, StructureMap, and AutoFac. This gives your team the flexibility to use common objects across application tiers while using different DI containers with only one 3 lines of code to change in most cases. 
```csharp
        ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services)) // 
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<EFCoreConfiguration>(x =>
                {
                    // Add all the DbContexts here
                    x.UsingDbContext<TestDbContext>();
                })
                .WithExceptionHandling<EhabExceptionHandlingConfiguration>(x =>
                    x.UsingDefaultExceptionPolicies())
                .And<CommonApplicationServicesConfiguration>()
                .And<SampleAppConfiguration>();
```


## Exception Management
Provides granular control over every possible exception that can be generated, and how to handle it. There are several pre-rolled policies for managing exceptions across all layers of your application including infrastructure, business, and presentation layers. Additionally, the ability to recover from exceptions is woven into the exception manager by wrapping exceptions in generic command results and allowing the layer responsible for managing the exception to decide whether or not to recover, rethrow, or simply log the issues.
```csharp
public async Task<CommandResult<bool>> NewCustomerSignupPromotion(CustomerDto customerDto)
    {
        var result = new CommandResult<bool>();
        try
        {
            // Do a bunch of stuff
        }
        catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
        {
            result.Exception = ex;
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            throw ex;
        }
        catch (AutoMapperMappingException ex) // Mapping Exception
        {
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            throw ex;
        }
        catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
        {
            result.Exception = ex;
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            throw ex;
        }

        return result;
    }
}
```


## Logging
Logging is used throughout the framework all the way down to the infrastructure. Microsoft's native logging is used but may be overridden by Nlog or other logging providers using the native interface. Rcommon uses the options pattern to allow you to subscribe to events generated in specific layers, or adjust verbosity overall. 

## Application Layer
A set of application service bases classes are included to simplify the mapping of entities to DTO's, implementing the UnitOfWork pattern in distributed systems. [MassTransit](https://masstransit-project.com/ "MassTransit") and [MediatR](https://github.com/jbogard/MediatR "MediatR") are first class citizens of RCommon as they are themselves excellent uses of abstractions and RCommon merely provides wrappers their usage. As such, RCommon's application layer is also seamlessly integrated with these libraries to support loosely coupled patterns for distributed computing systems. 
```csharp
public class CrudAppService<TDataTransferObject, TEntity> : RCommonAppService, ICrudAppService<TDataTransferObject> where TEntity : class
{
    private readonly ICrudDomainService<TEntity> _crudDomainService;
    private IMapper _objectMapper;

    public CrudAppService(ICrudDomainService<TEntity> crudDomainService, IMapper objectMapper, ILogger logger, IExceptionManager exceptionManager,
        IUnitOfWorkScopeFactory unitOfWorkScopeFactory)
        : base(logger, exceptionManager, unitOfWorkScopeFactory)
    {
        this._crudDomainService = crudDomainService;
        this._objectMapper = objectMapper;
    }

    public virtual async Task<CommandResult<bool>> CreateAsync(TDataTransferObject dto)
    {
        var result = new CommandResult<bool>(); // We only return serializable Data transfer objects (DTO) from this layer

        try
        {
            var entity = this._objectMapper.Map<TEntity>(dto); // Map the entity to a DTO

            await using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
            {
                var domainData = await _crudDomainService.CreateAsync(entity); // Perform the work

                if (domainData.HasException)
                {

                    throw domainData.Exception; // This generally doesn't happen since we allow domain exceptions to bubble up to the application layer
                }
                else
                {
                    result.DataResult = domainData.DataResult; // Set the data to return to the DTO
                }

                scope.Commit(); // Commit the transaction
            }

            return result;
        }
        catch (BusinessException ex) // The exception was handled at a lower level if we get BusinessException
        {
            result.Exception = ex;
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationReplacePolicy);
            throw ex;
        }
        catch (AutoMapperMappingException ex) // Mapping Exception
        {
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            throw ex;
        }
        catch (ApplicationException ex) // We didn't do a good job handling exceptions at a lower level or have failed logic in this class
        {
            result.Exception = ex;
            this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.ApplicationWrapPolicy);
            throw ex;
        }

    }
    ...
```

##Credits
We give special thanks to a number of projects that have been inspirational to us in building this project:
NCommon
ABP Framework
MassTransit
MediatR

