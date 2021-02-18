using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using Photos.AnalyzerService;
using Photos.AnalyzerService.Abstractions;

using PhotosAzure;

[assembly: FunctionsStartup(typeof(Startup))]

namespace PhotosAzure
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IAnalyzerService, ComputerVisionAnalyzerServices>();
        }
    }
}
