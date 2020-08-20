# RCommon Application Framework

## Overview
RCommon was born out of the need to generate a productive, yet a relatively sound (architecturally speaking) application. Architectural patterns are used to implement some of the most commonly used tools in the .NET Core (and soon .NET 5) stack. The primary goals of this framework are:
1. Future proofing applications against changing architectural needs whether changes are required from lower level code (e.g. .NET Framework), or in response to changing technology conditions (e.g. using EFCore instead of Linq2Sql, NLog for Logger.NET, StructureMap vs. Autofac, etc.)
2. Solve common problems under the presentation layer. Presentation frameworks are something else entirely. We try to keep everything nice under the hood. Cross cutting concerns, data access strategies, transaction management, validation, business rules, exception management, and logging is where we want to shine.
3. Code testability. We try to limit the "magic" used. Things like dependency injection are used but in a very straightforward manner. Unit tests, and integration tests should be implemented to the highest degree possible. Afterall, we want the applications you build on top of this to work :) 
4. Last but not least - open source forever. 

## Repository Pattern & Object Persistence
RCommon provides a common abstraction and underlying strategies/implementations for a variety of repositories including SQL via Dapper (soon), Entity Framework Core, Nhibernate, and MongoDB (soon) making RCommon one of the most versatile Object Access Repositories available. Each implementation is unit tested (soon) and integration tested in web, single threaded, and multithreaded hosting environments.

## Unit of Work & Transaction Management
The unit of work (UoW) pattern is loosely coupled from all object access strategies but provides granular control over transactions using ACID properties. Transactions are currently implemented through the UnitOfWork and the UnitOfWorkManager which provides a wrapper for TransactionScope. Natively supported transaction providers (via Nhibernate, and EF Core) are coming soon.

## Dependency Injection
Rcommon provides a common Dependency Injection container adapter that may be used with any of the major DI containers available including, Castle Windsor, StructureMap, and AutoFac. This gives your team the flexibility to use common objects across application tiers while using different DI containers with only one 3 lines of code to change in most cases. 

## Exception Management
Provides granular control over every possible exception that can be generated, and how to handle it. There are several pre-rolled policies for managing exceptions across all layers of your application including infrastructure, business, and presentation layers. Additionally, the ability to recover from exceptions is woven into the exception manager by wrapping exceptions in generic command results and allowing the layer responsible for managing the exception to decide whether or not to recover, rethrow, or simply log the issues.

## Logging
Logging is used throughout the framework all the way down to the infrastructure. Microsoft's native logging is used but may be overridden by Nlog or other logging providers using the native interface. Rcommon uses the options pattern to allow you to subscribe to events generated in specific layers, or adjust verbosity overall. 

## Domain Services & Entity Validation
A set of domain service base classes allows you access to underlying API's including validation, exception management, unit of work management, logging, and repository operations. Business rule validators are implemented via the Specification pattern and rules may be auto-wired and mapped to domain services and entities. Repository operations may also be auto-wired to run after the business rules/validation layer successfully concludes.

## Application Layer
A set of application service bases classes are included to simplify the mapping of entities to DTO's and implement the UnitOfWork pattern as well as encapsulating output from domain/business services and wrapping them in JSON friendly containers for handling by the application service or presentation layer. Additionally, the application layer simplifies exposing application services as Http API services.

## Presentation Layer
A set of base classes designed for MVC Controllers, Web API Controllers, and Razor Pages. The base classes encapsulate responses from the application layer or Http API services and simplify model/view model validation and interactions with the application layer.

