using Core.Authentication.Data;
using Core.Authentication.Models;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Core.Authentication.Infrastructure
{
    public static class IdentityServerExtensions
    {
        public static void IdentityServerInMemory(this IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(InMemoryConfig.GetIdentityResources())
                .AddInMemoryApiResources(InMemoryConfig.GetApiResources())
                .AddInMemoryClients(InMemoryConfig.GetClients())
                .AddTestUsers(InMemoryConfig.GetUsers());
        }

        /// <summary>
        /// IdentityServer em memória(clients, resources) e apenas IdentityUser no banco. Seleciona entre SQLlite e SQLServer, default SQLLite.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connectionString"></param>
        public static void IdentityServerInMemory(this IServiceCollection services, string connectionString, bool sqlServer = false)
        {
            if(sqlServer)
              services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            else
              services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(InMemoryConfig.GetIdentityResources())
                .AddInMemoryApiResources(InMemoryConfig.GetApiResources())
                .AddInMemoryClients(InMemoryConfig.GetClients())
                .AddAspNetIdentity<ApplicationUser>();

            InitUserDataBase(services.BuildServiceProvider());

        }

        public static void IdentityServerSqlServer(this IServiceCollection services, string connectionString)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(builder =>
                builder.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));

            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
            .AddOperationalStore(options =>
                options.ConfigureDbContext = builder =>
                builder.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)))
            .AddConfigurationStore(options =>
                options.ConfigureDbContext = builder =>
                builder.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)))
            .AddAspNetIdentity<ApplicationUser>()
            .AddDeveloperSigningCredential();
        }

        public static void IdentityServerSQLlite(this IServiceCollection services, string connectionString)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
            .AddAspNetIdentity<ApplicationUser>()
            // this adds the config data from DB (clients, resources)
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlite(connectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlite(connectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly));

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = true;
                // options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
            });

            //if (Environment.IsDevelopment())
            //{
            builder.AddDeveloperSigningCredential();
            //}
            //else
            //{
            //    throw new Exception("need to configure key material");
            //}
            //var serviceProvider = services.BuildServiceProvider();
            InitDataBase(services.BuildServiceProvider());
        }
        
        private static void InitDataBase(IServiceProvider service)
        {
            using (var scope = service.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
                scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

                var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (!context.Clients.Any())
                {
                    foreach (var client in InMemoryConfig.GetClients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in InMemoryConfig.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in InMemoryConfig.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                if (!userManager.Users.Any())
                {
                    foreach (var testUser in InMemoryConfig.GetUsers())
                    {
                        var identityUser = new ApplicationUser()
                        {
                            Id = testUser.SubjectId,
                            UserName = testUser.Username
                        };

                        userManager.CreateAsync(identityUser, testUser.Password).Wait();
                        userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList()).Wait();
                    }

                }
            }
        }

        private static void InitUserDataBase(IServiceProvider service)
        {
            using (var scope = service.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                if (!userManager.Users.Any())
                {
                    foreach (var testUser in InMemoryConfig.GetUsers())
                    {
                        var identityUser = new ApplicationUser()
                        {
                            Id = testUser.SubjectId,
                            UserName = testUser.Username
                        };

                        userManager.CreateAsync(identityUser, testUser.Password).Wait();
                        userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList()).Wait();
                    }

                }
            }
        }
    }
}