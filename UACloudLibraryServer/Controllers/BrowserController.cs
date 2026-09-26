using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.Models;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ServiceFilter(typeof(DppAuditFailureFilter))]
    public class BrowserController : Controller
    {
        private readonly UAClient _client;
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;
        private readonly IDppAuditLog _auditLog;
        private readonly ILogger<BrowserController> _logger;

        // Node values may contain characters like <, >, & that System.Text.Json escapes to \uXXXX by
        // default; relaxed escaping keeps the stored values blob readable and Newtonsoft-equivalent.
        private static readonly JsonSerializerOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public BrowserController(UAClient client, DbFileStorage storage, CloudLibDataProvider database, IDppAuditLog auditLog, ILogger<BrowserController> logger)
        {
            _client = client;
            _storage = storage;
            _database = database;
            _auditLog = auditLog;
            _logger = logger;
        }

        private string OperatorId => User?.Identity?.Name ?? "anonymous";

        public ActionResult Index(string nodesetIdentifier, string nodesetName, string userName, string statusMessage, string search)
        {
            return View("Index", new BrowserModel() {
                NodesetIdentifier = nodesetIdentifier,
                NodesetName = nodesetName,
                UserName = userName,
                StatusMessage = statusMessage,
                Search = search
            });
        }

        [HttpPost]
        public async Task<ActionResult> Download(BrowserModel model)
        {
            // A raw browse returns every node value, including elements the per-DPP controlledElements
            // map restricts. This endpoint only requires ApiPolicy, so without an ownership check any
            // authenticated caller could export a published DPP in full and bypass the role filtering
            // that DPPLifecycleApiController applies on the read path. Gate on ownership exactly as
            // Save does, rather than re-deriving role filtering over an untyped value dictionary.
            NodeSetModel nodeSet = _database.GetNodeSets(User.Identity.Name, model.NodesetIdentifier).FirstOrDefault();
            if ((nodeSet == null) || (User.Identity.Name != nodeSet.Metadata.UserId))
            {
                return Forbid();
            }

            Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(User.Identity.Name, model.NodesetIdentifier, null).ConfigureAwait(false);

            // A browse returns node values only. Exporting them alone would produce a file that, when
            // re-uploaded, silently strips the per-DPP controlledElements map and turns every
            // controlled element public - so re-attach it the same way the Save path does.
            //
            // A missing row is refused rather than exported: "no stored file" and "the store was
            // unreachable" both used to arrive here as null, and exporting on either produces exactly
            // the policy-stripped file this merge exists to prevent. Storage faults now propagate out
            // of DownloadFileAsync, so the remaining null is a genuinely absent row - which still
            // means there is no policy to preserve and no safe export to produce.
            DbFiles nodesetXml = await _storage.DownloadFileAsync(model.NodesetIdentifier).ConfigureAwait(false);
            if (nodesetXml == null)
            {
                _logger.LogError(
                    "Refusing to export nodeset {NodesetIdentifier}: its stored values could not be loaded, so the controlledElements map cannot be preserved.",
                    model.NodesetIdentifier);

                return StatusCode(
                    Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable,
                    "The nodeset's stored values could not be loaded, so an export would drop its access policy. Try again later.");
            }

            string values = DppControlledElements.Merge(JsonSerializer.Serialize(results, s_jsonOptions), nodesetXml.Values);

            // This exports the whole value set, controlled elements included. Ownership stops an
            // unauthorised read, but the non-repudiation invariant is that DPP reads are recorded, so
            // audit before the bytes are handed over: appending afterwards would mean a failed append
            // turns into a 503 on a response that already disclosed the data. A failure here raises
            // DppAuditException, which DppAuditFailureFilter converts into a 503.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Read, model.NodesetIdentifier, null, "Success").ConfigureAwait(false);

            return File(Encoding.UTF8.GetBytes(values), "text/json", "nodevalues.json");
        }

        [HttpPost]
        public async Task<ActionResult> Save(BrowserModel model)
        {
            NodeSetModel nodeSet = _database.GetNodeSets(User.Identity.Name, model.NodesetIdentifier).FirstOrDefault();

            if ((nodeSet != null) && (User.Identity.Name == nodeSet.Metadata.UserId))
            {
                DbFiles nodesetXml = await _storage.DownloadFileAsync(model.NodesetIdentifier).ConfigureAwait(false);
                if (nodesetXml == null)
                {
                    // Previously this dereferenced straight into a NullReferenceException. Saving
                    // without the existing blob would also rewrite Values from the browse alone and
                    // drop the controlledElements map, so refuse explicitly instead.
                    _logger.LogError(
                        "Refusing to save nodeset {NodesetIdentifier}: its stored values could not be loaded, so the controlledElements map cannot be preserved.",
                        model.NodesetIdentifier);

                    return RedirectToAction("Index", new {
                        nodesetIdentifier = model.NodesetIdentifier,
                        nodesetName = model.NodesetName,
                        userName = model.UserName,
                        statusMessage = "The nodeset's stored values could not be loaded, so the save was refused to avoid dropping its access policy."
                    });
                }

                Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(User.Identity.Name, model.NodesetIdentifier, null).ConfigureAwait(false);
                // Re-attach the per-DPP controlledElements access map: a browse only returns node values,
                // so merge it back from the existing values blob to avoid dropping it on save.
                nodesetXml.Values = DppControlledElements.Merge(JsonSerializer.Serialize(results, s_jsonOptions), nodesetXml.Values);

                // Write-ahead audit intent, as on the other mutation paths: the storage write below is
                // not transactional with the audit table, so an "Attempted" entry with no matching
                // outcome is the signal that a save may have completed without being fully logged.
                // The shared operation id is what makes that pairing unambiguous when saves of the
                // same nodeset interleave.
                string operationId = DppAuditOperationId.New();
                await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, model.NodesetIdentifier, null, "Attempted", operationId).ConfigureAwait(false);

                string name = await _storage.UploadFileAsync(model.NodesetIdentifier, nodesetXml.Blob, nodesetXml.Values).ConfigureAwait(false);
                if (name == null)
                {
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, model.NodesetIdentifier, null, "Failed", operationId).ConfigureAwait(false);
                    model.StatusMessage = "Failed to save changes to this nodeset.";
                }
                else
                {
                    // The upload already committed, so a failure to record this outcome must not be
                    // reported as a retryable refusal.
                    await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Modify, model.NodesetIdentifier, null, "Success", operationId).ConfigureAwait(false);
                    model.StatusMessage = "Save operation successful";
                }
            }
            else
            {
                await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, model.NodesetIdentifier, null, "Denied").ConfigureAwait(false);
                model.StatusMessage = "You are not authorized to save changes to this nodeset.";
            }

            return LocalRedirect($"/browser?NodesetIdentifier={model.NodesetIdentifier}&NodesetName={model.NodesetName}&UserName={model.UserName}&StatusMessage={model.StatusMessage}");
        }
    }
}
