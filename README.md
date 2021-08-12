# RCommon Application Framework

## Overview
RCommon was originally born as the (now abandoned) [NCommon](https://github.com/riteshrao/ncommon "NCommon") project but was resurrected out of the need to generate a productive, yet a relatively sound (architecturally speaking) application. Architectural patterns are used to implement some of the most commonly used tools in the .NET Core (and soon .NET 5) stack. The primary goals of this framework are:
1. Future proofing applications against changing architectural needs whether changes are required from lower level code (e.g. .NET Framework), or in response to changing technology conditions (e.g. using EFCore instead of Linq2Sql, NLog for Logger.NET, StructureMap vs. Autofac, etc.)
2. Solve common problems under the presentation layer. Presentation frameworks are something else entirely. We try to keep everything nice under the hood. Cross cutting concerns, data access strategies, transaction management, validation, business rules, exception management, and logging is where we want to shine.
3. Code testability. We try to limit the "magic" used. Things like dependency injection are used but in a very straightforward manner. Unit tests, and integration tests should be implemented to the highest degree possible. Afterall, we want the applications you build on top of this to work :) 
4. Last but not least - open source forever. 

We track bugs, enhancement requests, new feature requests, and general issues on [GitHub Issues](https://github.com/Reactor2Team/RCommon/issues "GitHub Issues") and are very responsive. General "how to" and community support should be managed on [Stack Overflow](https://stackoverflow.com/questions/tagged/rcommon "Stack Overflow"). 

## Repository Pattern & Object Persistence
RCommon provides a common abstraction and underlying strategies/implementations for a variety of repositories including SQL via Dapper (soon), Entity Framework Core, Nhibernate, and MongoDB (soon) making RCommon one of the most versatile Object Access Repositories available. Each implementation is unit tested (soon) and integration tested in web, single threaded, and multithreaded hosting environments. "Full featured" object access strategies such as EFCore and NHibernate come packaged with the ability to eager load additionally entities into the IQueryable expression map.
```csharp
if (includeDetails)
    {
        _pageRepository.EagerlyWith(y => y.ParentPage);
        _pageRepository.EagerlyWith(y => y.Status);
    }
    cmd.DataResult = await _pageRepository.FindAsync(x => x.SiteId == siteId);
```


## Unit of Work & Transaction Management
The unit of work (UoW) pattern is loosely coupled from all object access strategies but provides granular control over transactions using ACID properties. Transactions are currently implemented through the UnitOfWork and the UnitOfWorkManager which provides a wrapper for TransactionScope. Natively supported transaction providers (via Nhibernate, and EF Core) are coming soon.
```csharp
using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
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

## Domain Services & Entity Validation
A set of domain service base classes allows you access to underlying API's including validation, exception management, unit of work management, logging, and repository operations. Business rule validators are implemented via the Specification pattern and rules may be auto-wired and mapped to domain services and entities. Repository operations may also be auto-wired to run after the business rules/validation layer successfully concludes.

Setting up of Entity Validator, and Rules Evaluator
```csharp
private void AddRulesAndValidators()
{
    this.SetEntityValidator(new CustomerValidator()); // This will get called before execution against repository

    this.SetBusinessRulesEvaluator(new CustomerBusinessRulesEvaluator()); // These rules will be evaluated before execution against repository
}
```
Customer Validator Example
```csharp
public class CustomerValidator : EntityValidatorBase<Customer>
{
    public CustomerValidator()
    {
        this.AddValidation("ZipCode Rule", new ValidationRule<Customer>(
            new Specification<Customer>(x => x.ZipCode != "30062"), "We don't like people from that zip code!", "ZipCode"));
    }
}
```
Customer Business Rules Evaluator Example
```csharp
public class CustomerBusinessRulesEvaluator : BusinessRulesEvaluatorBase<Customer>
{
    public CustomerBusinessRulesEvaluator()
    {
        var rule = new BusinessRule<Customer>(
            new Specification<Customer>(x => x.ZipCode != "30062"),
                x => this.SomeImportantBusinessAction(x)
            );
        this.AddRule("ZipCodeRule", rule);
    }

    private void SomeImportantBusinessAction(Customer customer)
    {
        Debug.WriteLine("We are doing something important related to the business rule for " + customer.FirstName + " " + customer.LastName);
    }
}
```
And Finally
```csharp
protected virtual ValidationResult ValidateEntity(TEntity entity)
{
    var result = new ValidationResult();
    if (_entityValidator != null)
    {
        result = _entityValidator.Validate(entity);
    }
    return result;
}

protected virtual void EvaluateBusinessRules(TEntity entity)
{
    if (_businessRulesEvaluator != null)
    {
        _businessRulesEvaluator.Evaluate(entity);
    }
}

public virtual async Task<CommandResult<bool>> CreateAsync(TEntity entity)
{
    var result = new CommandResult<bool>();
    try
    {
        result.ValidationResult = this.ValidateEntity(entity);
        if (result.ValidationResult.IsValid)
        {
            this.EvaluateBusinessRules(entity);
            await _repository.AddAsync(entity);
            this.Logger.LogDebug("Creating entity of type {0}.", entity);
            result.DataResult = true;
        }
        else
        {
            this.Logger.LogWarning("Validator of type " + this._entityValidator.GetType().ToString() + " was not able to validate entity of type " + entity.GetType().ToString());
            result.DataResult = false;
        }
        return result;
    }
    catch (ApplicationException ex)
    {
        result.Exception = ex;
        this.ExceptionManager.HandleException(ex, DefaultExceptionPolicies.BusinessWrapPolicy);
        throw ex;
    }
}
```



## Application Layer
A set of application service bases classes are included to simplify the mapping of entities to DTO's and implement the UnitOfWork pattern as well as encapsulating output from domain/business services and wrapping them in JSON friendly containers for handling by the application service or presentation layer. Additionally, the application layer simplifies exposing application services as Http API services.
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

            using (var scope = UnitOfWorkScopeFactory.Create()) // Always use a Unit of Work
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



