using Arriba.Communication.Server.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;

namespace Arriba.Test.Services
{
    [TestClass]
    public partial class ArribaQueryServicesTests : ArribaServiceBase
    {
        private readonly IArribaQueryServices _queryService;
        private readonly NameValueCollection parameters = new NameValueCollection();
        public ArribaQueryServicesTests() : base()
        {
            _queryService = _serviceProvider.GetService<IArribaQueryServices>();
        }
    }
}
