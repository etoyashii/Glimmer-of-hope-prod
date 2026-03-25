using System;

namespace GlimmerOfHope.Core.Services
{
    public interface IService
    {
        void Initialize();
        void Shutdown();
    }
}
