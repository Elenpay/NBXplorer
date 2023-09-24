using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc;
using NBitcoin.JsonConverters;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http.Features;
using NBXplorer.Filters;
using NBXplorer.Logging;
using Microsoft.AspNetCore.Authentication;
using NBXplorer.Authentication;
using NBXplorer.Configuration;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#if NETCOREAPP21
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Hosting;
#endif

namespace NBXplorer
{
	public class Startup
	{
		public Startup(IConfiguration conf, IWebHostEnvironment env, ILoggerFactory loggerFactory)
		{
			Configuration = conf;
			_Env = env;
			LoggerFactory = loggerFactory;
			Logs = new Logs();
			Logs.Configure(loggerFactory);
		}
		
		private readonly IWebHostEnvironment _Env;
		public ILoggerFactory LoggerFactory { get; }
		public Logs Logs { get; }
		
		public IConfiguration Configuration
		{
			get;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			var logger = LoggerFactory.CreateLogger<Startup>();

			
			services.AddHttpClient();
			services.AddHttpClient(nameof(IRPCClients), httpClient =>
			{
				httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
			});
			services.AddNBXplorer(Configuration);
			services.ConfigureNBxplorer(Configuration);
			var builder = services.AddMvcCore();
#if NETCOREAPP21
			builder.AddJsonFormatters();
#else
			services.AddHealthChecks().AddCheck<HealthChecks.NodesHealthCheck>("NodesHealthCheck");
			builder.AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
				new Serializer(null).ConfigureSerializer(options.SerializerSettings);
			});
#endif
			builder.AddMvcOptions(o => o.InputFormatters.Add(new NoContentTypeInputFormatter()))
			.AddAuthorization()
			.AddFormatterMappings();
			services.AddAuthentication("Basic")
				.AddNBXplorerAuthentication();
			
			            //We need to expand the env-var with %ENV_VAR% for K8S
            var otelCollectorEndpointToBeExpanded = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

            if (otelCollectorEndpointToBeExpanded != null)
            {
                var otelCollectorEndpoint = Environment.ExpandEnvironmentVariables(otelCollectorEndpointToBeExpanded);

                if (!string.IsNullOrEmpty(otelCollectorEndpoint))
                {
                    const string otelResourceAttributes = "OTEL_RESOURCE_ATTRIBUTES";
                    var expandedResourceAttributes = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable(otelResourceAttributes));
                    Environment.SetEnvironmentVariable(otelResourceAttributes, expandedResourceAttributes);

                    logger.LogInformation($"Setting up OTEL to: {otelCollectorEndpoint} with attributes: {expandedResourceAttributes}");



                    services
                        .AddOpenTelemetry()
                        .WithMetrics(builder => builder
                            .SetResourceBuilder(ResourceBuilder.CreateEmpty().AddEnvironmentVariableDetector())
                            .AddAspNetCoreInstrumentation()
                            .AddRuntimeInstrumentation()
                            .AddOtlpExporter(options =>
                            {
                                options.Protocol = OtlpExportProtocol.Grpc;
                                options.ExportProcessorType = ExportProcessorType.Simple;
                                options.Endpoint = new Uri(otelCollectorEndpoint);
                            })
                            //.AddMeter(meterService.Meter.Name)
                        ).WithTracing((builder) => builder
                            .SetResourceBuilder(ResourceBuilder.CreateEmpty().AddEnvironmentVariableDetector())
                            // Add tracing of the AspNetCore instrumentation library
                            .AddAspNetCoreInstrumentation()
                            .AddOtlpExporter(options =>
                            {
                                options.Protocol = OtlpExportProtocol.Grpc;
                                options.ExportProcessorType = ExportProcessorType.Simple;
                                options.Endpoint = new Uri(otelCollectorEndpoint);
                            })
                            .AddEntityFrameworkCoreInstrumentation()
                        );

                    //If we don't have a propagator set, we set it to the NONE propagator
                    if (Environment.GetEnvironmentVariable("OTEL_PROPAGATORS") == "none" ||
                        Environment.GetEnvironmentVariable("OTEL_PROPAGATORS") == null) // This is different than what dotnet does, we dont want any propagator by default
                    {
                        logger.LogInformation("Setting default OTEL propagator to NONE");
                        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new List<TextMapPropagator>()));
                    }
                    else
                    {
                        //Add trace context propagator and baggage propagator
                        logger.LogInformation("Setting default OTEL propagator to TraceContextPropagator and BaggagePropagator");
                        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new List<TextMapPropagator>
                        {
                            new TraceContextPropagator(),
                            new BaggagePropagator()
                        }));
                    }
                }
            }
			
		}

		public void Configure(IApplicationBuilder app, IServiceProvider prov,
			ExplorerConfiguration explorerConfiguration,
			IWebHostEnvironment env,
			ILoggerFactory loggerFactory, IServiceProvider serviceProvider,
			CookieRepository cookieRepository)
		{
			cookieRepository.Initialize();
			if(env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			Logs.Configure(loggerFactory);
			if (!string.IsNullOrEmpty(explorerConfiguration.InstanceName))
			{
				app.Use(async (httpContext, next) =>
				{
					httpContext.Response.Headers.Add("instance-name", explorerConfiguration.InstanceName);
					await next();
				});
			}
#if !NETCOREAPP21
			app.UseRouting();
#endif
			app.UseAuthentication();
#if !NETCOREAPP21
			app.UseAuthorization();
#endif
			app.UseWebSockets();
			//app.UseMiddleware<LogAllRequestsMiddleware>();
#if NETCOREAPP21
			app.UseMvc();
#else
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapHealthChecks("health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
				{
					ResponseWriter = HealthChecks.HealthCheckWriters.WriteJSON
				});
				endpoints.MapControllers();
			});
#endif
		}
	}
}
