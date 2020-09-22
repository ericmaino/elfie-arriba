using Arriba.Server.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Arriba.Authorization
{
    [TestClass]
    public class ArribaIdentityNormalizerTest
    {
        private const string JWTNameClaimType = "name";
        private const string AuthenticationType = "TestAuthenticationType";
        private readonly IList<Claim> _claims;
        private readonly ArribaIdentityNormalizer _identityNormalizer;

        public ArribaIdentityNormalizerTest()
        {
            _identityNormalizer = new ArribaIdentityNormalizer();

            _claims = new List<Claim>();
            _claims.Add(new Claim(JWTNameClaimType, "Visouza"));
            _claims.Add(new Claim(ClaimTypes.Role, "Writer"));
        }

        [TestMethod]
        public void ArribaIdentityNormalizerPolulateIdentityName()
        {
            var identity = new ClaimsIdentity(_claims, AuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var claimsName = _claims.Where(c => c.Type == JWTNameClaimType).First().Value;

            Assert.IsNull(principal.Identity.Name);

            var normalizedPrincipal = _identityNormalizer.NormalizeIdentity(principal);

            Assert.AreEqual(claimsName, normalizedPrincipal.Identity.Name);
            Assert.AreEqual(_claims.Count + 1, normalizedPrincipal.Claims.Count());
        }

        [TestMethod]
        public void ArribaIdentityNormalizerInvalidIdentityNoAuthenticationType()
        {
            var identity = new ClaimsIdentity(_claims);
            var principal = new ClaimsPrincipal(identity);

            Assert.IsNull(principal.Identity.Name);
            Assert.ThrowsException<ArgumentException>(() => _identityNormalizer.NormalizeIdentity(principal));
        }

        [TestMethod]
        public void ArribaIdentityNormalizerInvalidClaimsNoName()
        {
            _claims.Remove(_claims.Where(c => c.Type == JWTNameClaimType).First());
            var identity = new ClaimsIdentity(_claims, AuthenticationType);
            var principal = new ClaimsPrincipal(identity);

            Assert.IsNull(principal.Identity.Name);
            Assert.ThrowsException<ArribaIdentityNormalizerException>(() => _identityNormalizer.NormalizeIdentity(principal));
        }


        [TestMethod]
        public void ArribaIdentityNormalizerEmptyAppRoleClaims()
        {
            foreach (var claim in _claims.Where(c => c.Type == ClaimTypes.Role).ToList())
            {
                _claims.Remove(claim);
            };

            var identity = new ClaimsIdentity(_claims, AuthenticationType);
            var principal = new ClaimsPrincipal(identity);

            Assert.ThrowsException<ArribaIdentityNormalizerException>(() => _identityNormalizer.NormalizeIdentity(principal));            
        }

    }
}
