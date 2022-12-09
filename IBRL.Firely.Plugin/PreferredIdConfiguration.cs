using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vonk.Core.Pluggability;

namespace IBRL.Firely.Plugin
{
	[VonkConfiguration(order:4409)]
	public static class PreferredIdConfiguration
	{
		public static IServiceCollection ConfigureServices(IServiceCollection services)
		{
			services.TryAddSingleton<PreferredIdService>();
			return services;
		}

		public static IApplicationBuilder Configure(IApplicationBuilder builder)
		{
			builder
				.OnCustomInteraction(Vonk.Core.Context.VonkInteraction.type_custom, "preferred-id")
				.AndResourceTypes(new[] { "NamingSystem" })
				.AndMethod("GET")
				.HandleAsyncWith<PreferredIdService>((svc, ctx) => svc.HandleRequest(ctx));
			return builder;
		}
	}
}
