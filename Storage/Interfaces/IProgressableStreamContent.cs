using System;

namespace Storage.Interfaces
{
    internal interface IProgressableStreamContent
    {
        IProgress<float>? Progress { get; }
    }
}