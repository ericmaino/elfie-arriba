using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;

namespace Arriba.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArribaController : ControllerBase
    {
        private readonly IArribaManagementService _arribaManagement;
        private readonly IArribaServerConfiguration _arribaServerConfiguration;

        public ArribaController(IArribaManagementService arribaManagement, IArribaServerConfiguration arribaServerConfiguration)
        {
            _arribaManagement = arribaManagement;
            _arribaServerConfiguration = arribaServerConfiguration;
        }

        private IActionResult ExecuteAction(Action action, Func<IActionResult> result)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ExceptionToActionResult(ex);
            }
            return result();
        }

        [HttpGet]
        public IActionResult GetTables()
        {
            return Ok(_arribaManagement.GetTables());
        }

        [HttpGet("allBasics")]
        public IActionResult GetAllBasics()
        {
            return Ok(_arribaManagement.GetTablesForUser(this.User));
        }

        [HttpGet("unloadAll")]
        public IActionResult GetUnloadAll()
        {
            if (!_arribaManagement.UnloadAllTableForUser(this.User))
                return new ForbidResult();

            return Ok($"All tables unloaded");
        }

        [HttpGet("table/{tableName}/unload")]
        public IActionResult GetUnloadTable(string tableName)
        {
            if (!_arribaManagement.UnloadTableForUser(tableName, this.User))
                return new ForbidResult();

            return Ok($"Table {tableName} unloaded");
        }

        [HttpPost("table")]
        public IActionResult PostCreateNewTable([Required] CreateTableRequest table)
        {
            return ExecuteAction(() => _arribaManagement.CreateTableForUser(table, this.User),
                          () => CreatedAtAction(nameof(PostCreateNewTable), null));
        }

        [HttpPost("table/{tableName}/addcolumns")]
        public IActionResult PostAddColumn(string tableName, [FromBody, Required] IList<ColumnDetails> columnDetails)
        {
            return ExecuteAction(() => _arribaManagement.AddColumnsToTableForUser(tableName, columnDetails, this.User),
                          () => CreatedAtAction(nameof(PostAddColumn), "Columns Added"));
        }

        private IActionResult ExceptionToActionResult(Exception ex)
        {
            if (ex is ArribaAccessForbiddenException)
                if (_arribaServerConfiguration.EnabledAuthentication)
                    return Forbid();
                else
                    return new StatusCodeResult((int)HttpStatusCode.Forbidden);

            if (ex is TableNotFoundException)
                return NotFound(ex.Message);

            return BadRequest(ex.Message);
        }

        [HttpGet("table/{tableName}/save")]
        public IActionResult GetSaveTable(string tableName)
        {
            return ExecuteAction(() => _arribaManagement.SaveTableForUser(tableName, this.User, VerificationLevel.Normal),
                          () => Ok("Saved"));
        }

        [HttpGet("table/{tableName}/reload")]
        public IActionResult GetReloadTable(string tableName)
        {

            return ExecuteAction(() => _arribaManagement.ReloadTableForUser(tableName, this.User),
                          () => Ok("Reloaded"));
        }

        [HttpDelete("table/{tableName}")]
        [HttpGet("table/{tableName}/delete")]
        public IActionResult DeleteTable(string tableName)
        {
            return ExecuteAction(() => _arribaManagement.DeleteTableForUser(tableName, this.User),
                          () => Ok("Deleted"));
        }

        // {POST | GET} /table/foo?action=delete
        [HttpPost("table/{tableName}")]
        [HttpGet("table/{tableName}")]
        public IActionResult PostDeleteTableRows(string tableName, [FromQuery, Required] string action, [FromQuery, Required] string q)
        {
            if (action != "delete")
                return BadRequest($"Action {action} not supported");

            DeleteResult result = null;

            return ExecuteAction(() => result = _arribaManagement.DeleteTableRowsForUser(tableName, q, this.User),
                           () => Ok(result.Count));
        }

        [HttpPost("table/{tableName}/permissions/{scope}")]
        public IActionResult PostGrantTableAccess([FromQuery, Required] string tableName,
            [FromQuery, Required] PermissionScope scope,
            [FromBody, Required] SecurityIdentity identity)
        {
            return ExecuteAction(() => _arribaManagement.GrantAccessForUser(tableName, identity, scope, this.User),
                           () => Ok("Granted"));
        }

        [HttpDelete("table/{tableName}/permissions/{scope}")]
        public IActionResult DeleteRevokeTableAccess([FromQuery, Required] string tableName,
            [FromQuery, Required] PermissionScope scope,
            [FromBody, Required] SecurityIdentity identity)
        {

            return ExecuteAction(() => _arribaManagement.RevokeAccessForUser(tableName, identity, scope, this.User),
                           () => Ok("Granted"));
        }

    }
}
