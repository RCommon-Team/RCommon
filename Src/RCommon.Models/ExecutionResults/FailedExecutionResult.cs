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
    /// Represents a failed execution result, optionally containing one or more error messages
    /// that describe the reason(s) for failure.
    /// </summary>
    /// <seealso cref="ExecutionResult"/>
    /// <seealso cref="SuccessExecutionResult"/>
    [DataContract]
    public record FailedExecutionResult : ExecutionResult
    {
        /// <summary>
        /// Gets the collection of error messages associated with the failure.
        /// </summary>
        public IReadOnlyCollection<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FailedExecutionResult"/> record.
        /// </summary>
        /// <param name="errors">
        /// A collection of error messages describing the failure. If <c>null</c>, an empty collection is used.
        /// </param>
        public FailedExecutionResult(
            IEnumerable<string> errors)
        {
            // Materialize to a list and guard against null to ensure Errors is never null.
            Errors = (errors ?? Enumerable.Empty<string>()).ToList();
        }

        /// <inheritdoc />
        [DataMember]
        public override bool IsSuccess { get; } = false;

        /// <summary>
        /// Returns a string describing the failure, including error messages if any are present.
        /// </summary>
        /// <returns>A human-readable description of the failure and its associated errors.</returns>
        public override string ToString()
        {
            return Errors.Any()
                ? $"Failed execution due to: {string.Join(", ", Errors)}"
                : "Failed execution";
        }
    }
}
