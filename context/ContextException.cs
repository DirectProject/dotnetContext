﻿/* 
 Copyright (c) 2010-2021, Direct Project
 All rights reserved.

 Authors:
    Joseph Shook    Joseph.Shook@Surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/


using System;

namespace Health.Direct.Context
{
    /// <summary>
    /// Exception for Direct <see cref="Context"/> processing errors
    /// </summary>
    public class ContextException : ContextException<ContextError>
    {
        /// <summary>
        /// Creates an ContextException with the specified error
        /// </summary>
        /// <param name="error">error code</param>
        public ContextException(ContextError error)
            : base(error)
        {
        }

        /// <summary>
        /// Creates an ContextException with the specified error
        /// </summary>
        /// <param name="error">error code</param>
        /// <param name="innerException">Inner exception</param>
        public ContextException(ContextError error, Exception innerException)
            : base(error, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for Direct <see cref="Context"/> processing errors
    /// </summary>
    public class BaseContextException : Exception
    {
        /// <summary>
        /// Initializes a new instance of an <see cref="BaseContextException"/>.
        /// </summary>
        public BaseContextException()
        {
        }

        /// <summary>
        /// Initializes a new instance of an <see cref="BaseContextException"/> with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        public BaseContextException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of an <see cref="BaseContextException"/> with a reference 
        /// to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        /// <param name="innerException">The inner exception reference.</param>
        public BaseContextException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents Direct specific exceptions specialized to a specific type of error
    /// </summary>
    /// <remarks>
    /// The generic version of this class is used to create specialized exceptions with an enumeration error type
    /// that provides the subtype of error.
    /// </remarks>
    /// <typeparam name="T">The type of error this exception type is specialized to, generally an enumeration</typeparam>
    [Serializable]
    public class ContextException<T> : BaseContextException
    {
        /// <summary>
        /// Initializes a new specialized instance of an <see cref="BaseContextException"/>
        /// </summary>
        /// <param name="error">The error type for this instance.</param>
        public ContextException(T error)
            : base(FormatMessage(error, ""))
        {
            Error = error;
        }

        /// <summary>
        /// Initializes a new specialized instance of an <see cref="BaseContextException"/>
        /// </summary>
        /// <param name="error">The error type for this instance.</param>
        /// <param name="message">The message that describes the error. </param>
        public ContextException(T error, string message)
            : base(FormatMessage(error, message))
        {
            Error = error;
        }

        /// <summary>
        /// Initializes a new specialized instance of an <see cref="BaseContextException"/>
        /// </summary>
        /// <param name="error">The error type for this instance.</param>
        /// <param name="innerException">The inner exception reference.</param>
        public ContextException(T error, Exception innerException)
            : this(error, string.Empty, innerException)
        {
            Error = error;
        }

        /// <summary>
        /// Initializes a new specialized instance of an <see cref="BaseContextException"/>
        /// </summary>
        /// <param name="error">The error type for this instance.</param>
        /// <param name="message">The message that describes the error. </param>
        /// <param name="innerException">The inner exception reference.</param>
        public ContextException(T error, string message, Exception innerException)
            : base(FormatMessage(error, message), innerException)
        {
            Error = error;
        }

        /// <summary>
        /// Private helper method to format the message passed to the base exception ctor.
        /// </summary>
        /// <param name="error">The error object</param>
        /// <param name="message">The message</param>
        /// <returns>Returns a formatted error message</returns>
        private static string FormatMessage(T error, string message)
        {
            string msg = "Context Error=" + error;

            if (!string.IsNullOrEmpty(message))
            {
                msg += Environment.NewLine + message;
            }

            return msg;
        }

        /// <summary>
        /// The specific error type for this instance.
        /// </summary>
        public T Error
        {
            get;
        }
    }
}
