using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Authentication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var seed = args.Contains("/seed");

            if (seed)
                args = args.Except(new[] { "/seed" }).ToArray();

            var host = BuildWebHost(args);

            if (seed)
                SeedData.EnsureSeedData(host.Services);

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    //builder.AddSerilog();
                })
                .UseStartup<Startup>()
                .Build();
    }
}
