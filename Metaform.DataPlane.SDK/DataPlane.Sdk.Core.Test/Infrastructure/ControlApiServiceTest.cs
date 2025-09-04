using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static System.Text.Json.JsonSerializer;

namespace DataPlane.Sdk.Core.Test.Infrastructure;

[TestSubject(typeof(ControlApiService))]
public class ControlApiServiceTest : IDisposable
{
    private const string TestDataPlaneId = "dotnet-sdk-test-dataplane";
    private readonly WireMockServer _mockServer = WireMockServer.Start();

    private readonly ControlApiService _service;

    public ControlApiServiceTest()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();

        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        _service = new ControlApiService(httpClientFactory.Object, Options.Create(new ControlApiOptions {
            BaseUrl = $"http://localhost:{_mockServer.Port}/api/control"
            // BaseUrl = "http://localhost:8083/api/control"
        }));
    }

    // BaseUrl = $"http://localhost:{_mockServer.Port}/api/control"

    public void Dispose()
    {
        _mockServer.Stop();
        _mockServer.Dispose();
    }

    [Fact]
    public async Task Register_Success()
    {
        _mockServer
            .Given(Request.Create().WithPath("/api/control/v1/dataplanes").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(IdResponseJson("test-dataplane")));
        var result = await _service.RegisterDataPlane(new DataPlaneInstance(TestDataPlaneId) {
            Url = new Uri("http://localhost/dataplane"),
            State = DataPlaneState.Available,
            AllowedSourceTypes = ["test-source-type"],
            AllowedTransferTypes = ["test-transfer-type"]
        });

        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }

    private static string IdResponseJson(string id)
    {
        return Serialize(new IdResponse(id));
    }

    [Fact]
    public async Task Register_Failure()
    {
        _mockServer
            .Given(Request.Create().WithPath("/api/control/v1/dataplanes").UsingPost())
            .RespondWith(Response.Create().WithNotFound());
        var result = await _service.RegisterDataPlane(new DataPlaneInstance(TestDataPlaneId) {
            Url = new Uri("http://localhost:8082"),
            State = DataPlaneState.Available,
            AllowedSourceTypes = ["test-source-type"],
            AllowedTransferTypes = ["test-transfer-type"]
        });

        result.IsSucceeded.ShouldBeFalse();
        result.Failure.ShouldNotBeNull();
        result.Failure.Reason.ShouldBe(FailureReason.NotFound);
    }

    [Fact]
    public async Task Register_MissingSourceOrTransferType()
    {
        await Should.ThrowAsync<ArgumentException>(async () => {
            await _service.RegisterDataPlane(new DataPlaneInstance(TestDataPlaneId) {
                Url = new Uri("http://localhost:8082"),
                State = DataPlaneState.Available,
                AllowedSourceTypes = [],
                AllowedTransferTypes = []
            });
        });
    }

    [Fact]
    public async Task UnregisterDataPlane_Success()
    {
        _mockServer
            .Given(Request.Create().WithPath($"/api/control/v1/dataplanes/{TestDataPlaneId}/unregister").UsingPut())
            .RespondWith(Response.Create().WithSuccess());
        var result = await _service.UnregisterDataPlane(TestDataPlaneId);
        result.IsSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task UnregisterDataPlane_NotFound()
    {
        _mockServer
            .Given(Request.Create().WithPath($"/api/control/v1/dataplanes/{TestDataPlaneId}/unregister").UsingPut())
            .RespondWith(Response.Create().WithNotFound());
        var result = await _service.UnregisterDataPlane(TestDataPlaneId);
        result.IsSucceeded.ShouldBeFalse();
        result.Failure.ShouldNotBeNull();
        result.Failure.Reason.ShouldBe(FailureReason.NotFound);
    }
}
