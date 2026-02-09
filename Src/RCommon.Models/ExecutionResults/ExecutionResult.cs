// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RCommon.Models.ExecutionResults
{
    /// <summary>
    /// Abstract base record for execution results, providing factory methods
    /// to create <see cref="SuccessExecutionResult"/> and <see cref="FailedExecutionResult"/> instances.
    /// </summary>
    /// <remarks>
    /// Cached singleton instances are used for <see cref="Success"/> and the parameterless <see cref="Failed()"/>
    /// to avoid unnecessary allocations for common result types.
    /// </remarks>
    /// <seealso cref="IExecutionResult"/>
    [DataContract]
    public abstract record ExecutionResult : IExecutionResult
    {
        // Cached singleton for the common success case to avoid repeated allocations.
        private static readonly IExecutionResult SuccessResult = new SuccessExecutionResult();

        // Cached singleton for a generic failure with no error messages.
        private static readonly IExecutionResult FailedResult = new FailedExecutionResult(Enumerable.Empty<string>());

        /// <summary>
        /// Returns a cached <see cref="SuccessExecutionResult"/> instance.
        /// </summary>
        /// <returns>An <see cref="IExecutionResult"/> representing a successful operation.</returns>
        public static IExecutionResult Success() => SuccessResult;

        /// <summary>
        /// Returns a cached <see cref="FailedExecutionResult"/> instance with no error messages.
        /// </summary>
        /// <returns>An <see cref="IExecutionResult"/> representing a failed operation.</returns>
        public static IExecutionResult Failed() => FailedResult;

        /// <summary>
        /// Creates a new <see cref="FailedExecutionResult"/> with the specified error messages.
        /// </summary>
        /// <param name="errors">A collection of error messages describing the failure.</param>
        /// <returns>An <see cref="IExecutionResult"/> representing a failed operation with error details.</returns>
        public static IExecutionResult Failed(IEnumerable<string> errors) => new FailedExecutionResult(errors);

        /// <summary>
        /// Creates a new <see cref="FailedExecutionResult"/> with the specified error messages.
        /// </summary>
        /// <param name="errors">One or more error messages describing the failure.</param>
        /// <returns>An <see cref="IExecutionResult"/> representing a failed operation with error details.</returns>
        public static IExecutionResult Failed(params string[] errors) => new FailedExecutionResult(errors);

        /// <inheritdoc />
        [DataMember]
        public abstract bool IsSuccess { get; }

        /// <summary>
        /// Returns a string representation of the execution result, including the success status.
        /// </summary>
        /// <returns>A string in the format "ExecutionResult - IsSuccess:{value}".</returns>
        public override string ToString()
        {
            return $"ExecutionResult - IsSuccess:{IsSuccess}";
        }
    }
}
