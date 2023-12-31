﻿namespace Skyline.DataMiner.CICD.Assemblers.Common
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The exception that is thrown when the DataMiner item could not be assembled.
    /// </summary>
    [Serializable]
    public class AssemblerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblerException"/> class.
        /// </summary>
        public AssemblerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblerException"/> class with a specified error message..
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AssemblerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblerException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If the inner parameter is not null, the current exception is raised in a catch block that handles the inner exception.</param>
        public AssemblerException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblerException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected AssemblerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
