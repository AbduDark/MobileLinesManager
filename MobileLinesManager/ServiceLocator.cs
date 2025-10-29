using System;
using Microsoft.Extensions.DependencyInjection;

namespace MobileLinesManager
{
    public static class ServiceLocator
    {
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
