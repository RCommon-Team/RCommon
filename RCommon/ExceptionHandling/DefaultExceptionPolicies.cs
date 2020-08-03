using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public class DefaultExceptionPolicies
    {
		/// <summary>
		/// This policy publishes the exception and then rethrows the same exception.
		/// </summary>
		public const string BasePolicy = "BasePolicy";

		/// <summary>
		/// This policy publishes the exception, wraps it in a custom exception, and then throws the custom exception. The intention is
		/// that there may be any variety of exceptions generated from the business tier which need to be caught, logged, and
		/// then wrapped so that business logic is able to appropriately handle the exception. 
		/// </summary>
		public const string BusinessWrapPolicy = "BusinessWrapPolicy";

		/// <summary>
		/// This policy should not publish the exception. The assumption is that the publishing has already been handled or does not need 
		/// to be handled. The exception is then replaced, typically by a "friendly exception" which can be output to a screen.
		/// </summary>
		/// <remarks>Do not use this unless you intend for your business object to control output that the user sees.</remarks>
		public const string BusinessReplacePolicy = "BusinessReplacePolicy";


		/// <summary>
		/// This policy publishes the exception, wraps it in a custom exception, and then throws the custom exception. The intention is
		/// that there may be any variety of exceptions generated from the application tier which need to be caught, logged, and
		/// then wrapped so that logic is able to appropriately handle the exception. 
		/// </summary>
		public const string ApplicationWrapPolicy = "ApplicationWrapPolicy";

		/// <summary>
		/// This policy should not publish the exception. The assumption is that the publishing has already been handled or does not need 
		/// to be handled. The exception is then replaced, typically by a "friendly exception" which can be output to a screen.
		/// </summary>
		public const string ApplicationReplacePolicy = "ApplicationReplacePolicy";

		/// <summary>
		/// This policy should not publish the exception. The assumption is that the publishing has already been handled or does not need 
		/// to be handled. The exception is then replaced, typically by a "friendly exception" which can be output to a screen.
		/// </summary>
		public const string PresentationReplacePolicy = "PresentationReplacePolicy";

		/// <summary>
		/// This policy is intended to be used with only exceptions created in the security layer. The intention is that 
		/// these exceptions will be logged and replaced with information that is suitable to be bubbled up to non-security layers.
		/// </summary>
		public const string SecurityReplacePolicy = "SecurityReplacePolicy";
    }
}
