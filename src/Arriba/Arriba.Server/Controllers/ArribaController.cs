using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arriba.Server.Controllers
{
    [ApiController]
    public class ArribaController : ControllerBase
    {
        [Route("SomeRoute")]
        public Task<IActionResult> DefaultRequest()
        {
            return Task.FromResult<IActionResult>(this.Ok());
        }
    }
}
