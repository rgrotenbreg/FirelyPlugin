using Hl7.Fhir.ElementModel;
using Moq;
using Vonk.Core.Common;
using Vonk.Core.Context;
using Vonk.Core.Repository;
using Vonk.Fhir.R4;
using Models = Hl7.Fhir.Model;

namespace IBRL.Firely.Plugin.Tests
{
	[TestClass]
	public class PreferredIdServiceTests
	{
		[TestMethod]
		public async Task HandleRequest_Without_Id_And_Without_Type_Returns_Error()
		{
			// Arrange
			var sut = new PreferredIdService(Mock.Of<IAdministrationSearchRepository>());

			var contextMock = new Mock<IVonkContext>();
			var arguments = new ArgumentCollection();
			contextMock.SetupGet(m => m.Arguments).Returns(arguments);
			var response = new VonkResponse();
			contextMock.SetupGet(m => m.Response).Returns(response);

			// Act
			await sut.HandleRequest(contextMock.Object);

			// Assert
			Assert.IsFalse(response.Success());
			Assert.AreEqual(2, response.Outcome.Issues.Count(i => i.IssueType == VonkOutcome.IssueType.Invalid));
			Assert.IsTrue(response.Outcome.Issues.Any(i => i.Details == "Parameter 'id' is missing."));
			Assert.IsTrue(response.Outcome.Issues.Any(i => i.Details == "Parameter 'type' is missing."));
		}

		[TestMethod]
		public async Task HandleRequest_With_Valid_Data_Executes_Search()
		{
			// Arrange
			var administrationSearchRepositoryMock = new Mock<IAdministrationSearchRepository>();
			var searchArguments = new ArgumentCollection();
			administrationSearchRepositoryMock.Setup(m => m.Search(It.IsAny<IArgumentCollection>(), It.IsAny<SearchOptions>()))
				.Callback<IArgumentCollection, SearchOptions>((args, options) => searchArguments = args as ArgumentCollection);

			var sut = new PreferredIdService(administrationSearchRepositoryMock.Object);

			var contextMock = new Mock<IVonkContext>();
			var arguments = new ArgumentCollection(
					new Argument(ArgumentSource.Query, "id", "idValue"),
					new Argument(ArgumentSource.Query, "type", "typeValue")
				);
			contextMock.SetupGet(m => m.Arguments).Returns(arguments);
			var response = new VonkResponse();
			contextMock.SetupGet(m => m.Response).Returns(response);

			// Act
			await sut.HandleRequest(contextMock.Object);

			// Assert
			administrationSearchRepositoryMock.Verify(m => m.Search(It.IsAny<IArgumentCollection>(), It.IsAny<SearchOptions>()), Times.Once);
			Assert.AreEqual(2, searchArguments.Count());
			Assert.IsTrue(searchArguments.Any(a => a.ArgumentName == "_type" && a.ArgumentValue == "NamingSystem"));
			Assert.IsTrue(searchArguments.Any(a => a.ArgumentName == "value" && a.ArgumentValue == arguments.First(arg => arg.ArgumentName == "id").ArgumentValue));
		}

		[TestMethod]
		public async Task HandleRequest_Returns_Result()
		{
			// Arrange
			var administrationSearchRepositoryMock = new Mock<IAdministrationSearchRepository>();
			administrationSearchRepositoryMock.Setup(m => m.Search(It.IsAny<IArgumentCollection>(), It.IsAny<SearchOptions>()))
				.ReturnsAsync(new SearchResult(new List<IResource> {
					(new Models.NamingSystem()
					{
						UniqueId = new List<Models.NamingSystem.UniqueIdComponent>
						{
							new Models.NamingSystem.UniqueIdComponent
							{
								Type=Models.NamingSystem.NamingSystemIdentifierType.Uri,
								Value = "uriValue"
							},
							new Models.NamingSystem.UniqueIdComponent
							{
								Type=Models.NamingSystem.NamingSystemIdentifierType.Oid,
								Value = "oidValue"
							}
						}
					}).ToIResource()
				}, 1));

			var sut = new PreferredIdService(administrationSearchRepositoryMock.Object);

			var contextMock = new Mock<IVonkContext>();
			var arguments = new ArgumentCollection(
					new Argument(ArgumentSource.Query, "id", "idValue"),
					new Argument(ArgumentSource.Query, "type", "oid")
				);
			contextMock.SetupGet(m => m.Arguments).Returns(arguments);
			var response = new VonkResponse();
			contextMock.SetupGet(m => m.Response).Returns(response);

			// Act
			await sut.HandleRequest(contextMock.Object);

			// Assert
			Assert.IsTrue(response.Success());
			var parameters = response.Payload.ToPoco<Models.Parameters>();
			Assert.IsNotNull(parameters);
			Assert.AreEqual("oidValue", parameters["result"].Value.ToString());

		}
	}
}
