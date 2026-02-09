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

using System;

namespace RCommon.ApplicationServices.Commands
{
    /// <summary>
    /// Exception thrown when no command handlers are registered for a given command type.
    /// </summary>
    /// <remarks>
    /// This exception is raised by <see cref="CommandBus"/> when it cannot resolve any
    /// <see cref="ICommandHandler"/> implementation for the dispatched command.
    /// </remarks>
    public class NoCommandHandlersException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NoCommandHandlersException"/> with the specified error message.
        /// </summary>
        /// <param name="message">A message describing which command type has no registered handlers.</param>
        public NoCommandHandlersException(string message) : base(message)
        {
        }
    }
}
