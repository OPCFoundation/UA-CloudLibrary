using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Tests that a version read withholds elements whenever the access policy for that version is
    /// unavailable.
    /// </summary>
    /// <remarks>
    /// A snapshot can lack a policy for two reasons - archived before the archive recorded policy,
    /// or a live version whose stored values row is missing. Both arrive as a null values blob, and
    /// <see cref="DppControlledElements.Read(string)"/> reports a blank input as
    /// <see cref="DppControlledElements.MappingState.Absent"/>, i.e. public. Since a DPP is built
    /// from the live OPC UA address space it can still be served in that state, so the distinction
    /// has to be carried explicitly rather than inferred from the blob.
    /// </remarks>
    public class DppVersionPolicyFilterTests
    {
        private const string ControlledPath = "secret";

        private static readonly string[] s_anonymous = Array.Empty<string>();
        private static readonly string[] s_auditor = { "Auditor" };
        private static readonly string[] s_publicOnly = { "public" };

        // Only _accessPolicy and _logger are touched by the filtering path under test.
        private static DPPService CreateService() =>
            new(null, null, null, null, new DppAccessPolicy(), NullLoggerFactory.Instance);

        private static string PolicyJson() =>
            new JsonObject {
                ["controlledElements"] = new JsonObject { [ControlledPath] = "Auditor" }
            }.ToJsonString();

        private static SingleValuedDataElement Leaf(string id) => new()
        {
            ElementId = id,
            Value = JsonValue.Create(id + "-value")
        };

        private static DigitalProductPassport Dpp() => new()
        {
            DigitalProductPassportId = "dpp-1",
            UniqueProductIdentifier = "prod-1",
            DppSchemaVersion = "1.0",
            DppStatus = "active",
            LastUpdate = DateTimeOffset.UtcNow,
            EconomicOperatorId = "EO-1",
            Elements = new List<DataElement> { Leaf("public"), Leaf(ControlledPath) }
        };

        private static string[] ElementIds(DigitalProductPassport dpp)
        {
            var ids = new List<string>();
            foreach (DataElement element in dpp.Elements)
            {
                ids.Add(element.ElementId);
            }

            return ids.ToArray();
        }

        [Fact]
        public void UnavailablePolicy_WithholdsEveryElement()
        {
            // The reported case: a live version whose DbFiles row is missing arrives here as a null
            // policy. Returning the full tree would publish controlled data, so the DPP's envelope
            // is returned with no elements at all.
            var snapshot = new DppVersionSnapshot(Dpp(), null, policyArchived: false);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_anonymous);

            Assert.NotNull(filtered);
            Assert.Equal("dpp-1", filtered.DigitalProductPassportId);
            Assert.Empty(filtered.Elements);
        }

        [Fact]
        public void UnavailablePolicy_WithholdsElementsEvenForAnAuthorizedCaller()
        {
            // The policy is unknown, not permissive: holding a role cannot grant access to a mapping
            // nobody can read, since the missing policy might have controlled far more.
            var snapshot = new DppVersionSnapshot(Dpp(), null, policyArchived: false);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_auditor);

            Assert.Empty(filtered.Elements);
        }

        [Fact]
        public void AvailablePolicy_FiltersNormally()
        {
            var snapshot = new DppVersionSnapshot(Dpp(), PolicyJson(), policyArchived: true);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_anonymous);

            Assert.Equal(s_publicOnly, ElementIds(filtered));
        }

        [Fact]
        public void AvailablePolicy_GrantsTheMappedRole()
        {
            var snapshot = new DppVersionSnapshot(Dpp(), PolicyJson(), policyArchived: true);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_auditor);

            Assert.Equal(2, filtered.Elements.Count);
        }

        [Fact]
        public void PolicyPresentButEmpty_IsPublic()
        {
            // A row that exists and declares no controlled elements genuinely is public. This is the
            // case the missing-row handling must not swallow: failing closed here would withhold
            // data that was never controlled.
            var snapshot = new DppVersionSnapshot(Dpp(), "{}", policyArchived: true);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_anonymous);

            Assert.Equal(2, filtered.Elements.Count);
        }

        [Fact]
        public void MalformedPolicy_WithholdsEveryElement()
        {
            var snapshot = new DppVersionSnapshot(Dpp(), "{ not json", policyArchived: true);

            DigitalProductPassport filtered = CreateService().FilterVersionForRoles(snapshot, s_anonymous);

            Assert.Empty(filtered.Elements);
        }
    }
}
