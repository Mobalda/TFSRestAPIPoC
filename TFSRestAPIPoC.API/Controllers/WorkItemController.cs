using Microsoft.AspNetCore.Mvc;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using TFSRestAPIPoC.API.DTO;

namespace TFSRestAPIPoC.API.Controllers
{
    [Route("api/[controller]")]
    public class WorkItemController : Controller
    {
        public WorkItemController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;
        private const string Project = "TFS Rest API PoC";

        [HttpGet]
        public IActionResult GetAll()
        {
            string teamProjectCollectionUri = $"http://{_configuration["TFSIP"]}/tfs/DefaultCollection";

            var visualStudioServicesConnection = new VssConnection(new Uri(teamProjectCollectionUri),
                new VssCredentials(new WindowsCredential(new NetworkCredential(_configuration["TFSUserName"], _configuration["TFSPassword"]))));

            var workItemTrackingHttpClient = visualStudioServicesConnection.GetClient<WorkItemTrackingHttpClient>();

            const string query = "My Queries/All PBI";

            var queryItem = workItemTrackingHttpClient.GetQueryAsync(Project, query).Result;

            var workItemQueryResult = workItemTrackingHttpClient.QueryByIdAsync(queryItem.Id).Result;

            if (workItemQueryResult == null) return NotFound();

            var ids = workItemQueryResult.WorkItems.Select(item => item.Id).ToArray();

            var fields = new string[3];
            fields[0] = "System.Id";
            fields[1] = "System.Title";
            fields[2] = "System.State";

            var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(ids, fields, workItemQueryResult.AsOf).Result;

            var workItemsDTO = new List<WorkItemResultDTO>();

            foreach (var workItem in workItems)
            {
                var workItemDTO = new WorkItemResultDTO
                {
                    Id = workItem.Fields["System.Id"].ToString(),
                    Title = workItem.Fields["System.Title"].ToString(),
                    State = workItem.Fields["System.State"].ToString()
                };

                workItemsDTO.Add(workItemDTO);
            }

            return Ok(workItemsDTO);
        }

        [HttpPost]
        public IActionResult Post([FromBody] WorkItemCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string teamProjectCollectionUri = $"http://{_configuration["TFSIP"]}/tfs/DefaultCollection";

            var visualStudioServicesConnection = new VssConnection(new Uri(teamProjectCollectionUri),
                new VssCredentials(new WindowsCredential(new NetworkCredential(_configuration["TFSUserName"], _configuration["TFSPassword"]))));

            var workItemTrackingHttpClient = visualStudioServicesConnection.GetClient<WorkItemTrackingHttpClient>();

            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = dto.Title
                }
            };

            var result = workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, Project, "Product Backlog Item").Result;

            return Ok(result);
        }
    }
}