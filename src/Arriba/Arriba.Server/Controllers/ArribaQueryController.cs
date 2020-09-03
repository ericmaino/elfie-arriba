using Arriba.Communication;
using Arriba.Communication.Server.Application;
using Arriba.Filters;
using Arriba.Monitoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace Arriba.Controllers
{
    [Route("api/Arriba")]
    [ApiController]
    [ArribaResultFilter]
    public class ArribaQueryController : ControllerBase
    {
        private readonly IArribaQueryServices _arribaQuery;
        private readonly ITelemetry _telemetry;

        public ArribaQueryController(IArribaQueryServices arribaQuery, ITelemetry telemetry)
        {
            _arribaQuery = arribaQuery;
            _telemetry = telemetry;
        }

        [HttpPost("table/{tableName}/select")]
        [HttpGet("table/{tableName}/select")]
        public async Task<IActionResult> GetTable(string tableName)
        {
            var parameters = await GetParametersFromQueryStringAndBody(Request);
            return Ok(_arribaQuery.QueryTableForUser(tableName, parameters, _telemetry, User));
        }

        private async Task<NameValueCollection> GetParametersFromQueryStringAndBody(HttpRequest request)
        {
            NameValueCollection parameters = new NameValueCollection();

            // Read parameters from query string
            parameters.Add(HttpUtility.ParseQueryString(request.QueryString.Value));

            // Read parameters from body (these will override ones from the query string)
            using var sr = new StreamReader(Request.Body);
            var queryStringInBody = await sr.ReadToEndAsync();
            parameters.Add(HttpUtility.ParseQueryString(queryStringInBody));

            return parameters;
        }
    }
}
