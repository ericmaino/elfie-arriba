using Arriba.Communication;
using Arriba.Composition;
using Arriba.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Arriba.Test.Composition
{
    [TestClass]
    public class HostCompositionTests
    {
        [TestMethod]
        public void VerifiedApplicationServerComposition()
        {
            var provider = ArribaServiceProvider.CreateTestingProvider();
            var server = provider.GetService<ApplicationServer>();
            Assert.IsNotNull(server);
        }

        [TestMethod]
        public void VerifyJsonWriterIsRegistered()
        {
            var provider = ArribaServiceProvider.CreateTestingProvider();
            var service = provider.GetService<ApplicationServer>();
            var writer = service.ReaderWriter.GetWriter("application/json", string.Empty);
            Assert.IsNotNull(writer);
        }
    }
}
