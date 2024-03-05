using System;

namespace SpacetimeDB
{
    public interface ISpacetimeDBLogger
    {
        void Log(string message);
        void LogError(string message);
        void LogWarning(string message);
        void LogException(Exception e);
    }
}
