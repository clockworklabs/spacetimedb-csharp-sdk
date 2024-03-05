using System;

namespace SpacetimeDB
{
    public class ReducerMismatchException : Exception
    {
        public ReducerMismatchException(string originalReducerName, string attemptedConversionReducerName)
            : base($"Cannot cast agruments from {originalReducerName} reducer call into {attemptedConversionReducerName} reducer arguments")
        {
        }
    }
}
