using Hl7.Fhir.ElementModel;
using Microsoft.AspNetCore.Http;
using Vonk.Core.Context;
using Vonk.Core.Repository;
using Vonk.Fhir.R4;
using Models = Hl7.Fhir.Model;

namespace IBRL.Firely.Plugin
{
	public class PreferredIdService
	{
		private readonly IAdministrationSearchRepository _administrationSearchRepository;

		public PreferredIdService(IAdministrationSearchRepository administrationSearchRepository)
		{
			_administrationSearchRepository = administrationSearchRepository;
		}

		public async Task HandleRequest(IVonkContext vonkContext)
		{
			var (_, args, response) = vonkContext.Parts();

			if(!ValidateRequest(args, response))
			{
				return;
			}

			var idArgumentValue = args.First(a => a.ArgumentName == "id").ArgumentValue;
			var typeArgumentValue = args.First(a => a.ArgumentName == "type").ArgumentValue;

			var argumentCollection = new ArgumentCollection(
					new Argument(ArgumentSource.Internal, "_type", "NamingSystem"),
					new Argument(ArgumentSource.Internal, "value", idArgumentValue)
				);
			var searchResults = await _administrationSearchRepository.Search(argumentCollection, new SearchOptions(VonkInteraction.all_read, new Uri("localhost:4080"), "Fhir4.0"));

			if(!ValidateSearchResults(searchResults, idArgumentValue, response))
			{
				return;
			}

			var namingSystem = searchResults.First().ToPoco<Models.NamingSystem>();

			if (!namingSystem.UniqueId.Any(id => id.Type?.ToString().ToLower() == typeArgumentValue))
			{
				response.Outcome.AddIssue(VonkIssue.PARAMETER_VALUE_INVALID, $"The NamingSystem does not contain a definition for 'type={typeArgumentValue}'.");
				response.HttpResult = StatusCodes.Status404NotFound;
				return;
			}

			var result = new Models.Parameters();
			result.Add("result", new Models.FhirString(namingSystem.UniqueId.First(id => id.Type?.ToString().ToLower() == typeArgumentValue).Value));
			response.Payload = result.ToIResource();
			response.HttpResult = StatusCodes.Status200OK;
		}

		private bool ValidateRequest(IArgumentCollection? args, IVonkResponse response)
		{
			if (args?.Any(a => a.ArgumentName == "id") != true)
			{
				response.Outcome.AddIssue(VonkIssue.INVALID_REQUEST, "Parameter 'id' is missing.");
			}

			if (args?.Any(a => a.ArgumentName == "type") != true)
			{
				response.Outcome.AddIssue(VonkIssue.INVALID_REQUEST, "Parameter 'type' is missing.");
			}

			if (response.Outcome.Issues.Any())
			{
				response.HttpResult = StatusCodes.Status400BadRequest;
				return false;
			}
			return true;
		}

		private bool ValidateSearchResults(SearchResult? searchResults, string idArgumentValue, IVonkResponse response)
		{
			if (searchResults?.Any() != true)
			{
				response.Outcome.AddIssue(VonkIssue.PARAMETER_VALUE_INVALID, $"The NamingSystem for 'id={idArgumentValue}' is not found.");
				response.HttpResult = StatusCodes.Status404NotFound;
				return false;
			}

			if (searchResults.Count() > 1)
			{
				response.Outcome.AddIssue(VonkIssue.INTERNAL_ERROR, $"There seems to be a duplicate entry for 'id={idArgumentValue}'");
				response.HttpResult = StatusCodes.Status500InternalServerError;
				return false;
			}
			return true;
		}
	}
}
