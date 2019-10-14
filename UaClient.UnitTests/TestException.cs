using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Workstation.UaClient
{
    public sealed class TestException : Exception
    {
        public TestException()
        {
        }

        public TestException(string message) : base(message)
        {
        }

        public TestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
