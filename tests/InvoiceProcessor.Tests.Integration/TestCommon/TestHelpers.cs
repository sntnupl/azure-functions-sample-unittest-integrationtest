using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace InvoiceProcessor.Tests.Integration.TestCommon
{
    public static class TestHelpers
    {
        public static IHostBuilder ConfigureDefaultTestHost<TProgram>(
            this IHostBuilder builder,
            Action<IWebJobsBuilder> configureWebJobs,
            INameResolver nameResolver = null,
            IJobActivator activator = null)
        {
            return builder.ConfigureDefaultTestHost(configureWebJobs, typeof(TProgram))
                .ConfigureServices(services => {
                    services.AddSingleton<IJobHost, JobHost>();

                    if (nameResolver != null) {
                        services.AddSingleton(nameResolver);
                    }

                    if (activator != null) {
                        services.AddSingleton(activator);
                    }
                });
        }

        public static IHostBuilder ConfigureDefaultTestHost(
            this IHostBuilder builder,
            Action<IWebJobsBuilder> configureWebJobs,
            params Type[] types)
        {
            return builder.ConfigureWebJobs(configureWebJobs)
                .ConfigureAppConfiguration(c => {
                    c.AddTestSettings();
                })
                .ConfigureServices(services => {
                    services.AddSingleton<ITypeLocator>(new FakeTypeLocator(types));

                    // Register this to fail a test if a background exception is thrown
                    services.AddSingleton<IWebJobsExceptionHandlerFactory, TestExceptionHandlerFactory>();
                })
                .ConfigureLogging((context, builder) => {
                    builder.AddConsole();
                });
        }

        
        public static Task<TResult> AwaitWithTimeout<TResult>(this TaskCompletionSource<TResult> taskSource)
        {
            return taskSource.Task;
        }
    }
}
