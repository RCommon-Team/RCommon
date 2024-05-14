using FluentAssertions;
using NUnit.Framework;
using RCommon.Mediator.Subscribers;
using RCommon.TestBase;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.UnitTests
{
	[TestFixture()]
	public class MediatorHandlerRegistrationTests : TestBootstrapper 
    {
		[Test]
		public void AllRequests_ShouldHaveMatchingHandler()
		{
			var requestTypes = typeof(ApplicationServicesRegistration).Assembly.GetTypes() // Contracts
				.Where(IsRequest)
				.ToList();

			var handlerTypes = typeof(ApplicationServicesRegistration).Assembly.GetTypes() // Application Services
				.Where(IsIRequestHandler)
				.ToList();

			foreach (var requestType in requestTypes) ShouldContainHandlerForRequest(handlerTypes, requestType);
		}

		private static void ShouldContainHandlerForRequest(IEnumerable<Type> handlerTypes, Type requestType)
		{
			handlerTypes.Should().ContainSingle(handlerType => IsHandlerForRequest(handlerType, requestType), $"Handler for type {requestType} expected");
		}

		private static bool IsRequest(Type type)
		{
			return typeof(IAppRequest).IsAssignableFrom(type);
		}

		private static bool IsIRequestHandler(Type type)
		{
			return type.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IAppRequestHandler<>));
		}

		private static bool IsHandlerForRequest(Type handlerType, Type requestType)
		{
			return handlerType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(ta => ta == requestType));
		}
		
	}
}
