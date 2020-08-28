using Arriba.Communication;
using Arriba.Composition;
using Arriba.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Composition
{
    [TestClass]
    public class HostCompositionTests
    {        
        private Host GetArribaHost()
        {
            var host = new Arriba.Composition.Host();
            host.Add<ISecurityConfiguration>(new ArribaServerConfiguration());
            host.Compose();
            return host;
        }

        [TestMethod]
        public void VerifiedApplicationServerComposition()
        {
            var host = GetArribaHost();

            var server = host.GetService<ApplicationServer>();
            Assert.IsNotNull(server);
        }

        [TestMethod]
        public void VerifyJsonWriterIsRegistered()
        {
            var host = GetArribaHost();

            var service = host.GetService<ApplicationServer>();
            var writer = service.ReaderWriter.GetWriter("application/json", string.Empty);
            Assert.IsNotNull(writer);
        }
    }
}
