#if MONO
namespace System.Runtime.CompilerServices
{
    // C# 9 records need this type; .NET 6 has it, netstandard2.1 (the Mono build) does not.
    internal static class IsExternalInit { }
}
#endif
