// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Web;
using Arriba.Communication;
using Arriba.Server;
using Arriba.Server.Owin;
using Microsoft.AspNetCore.Http;

namespace Arriba.Owin
{
    public class ArribaHttpContextRequest : ArribaRequest
    {
        private readonly HttpContext _context;

        public ArribaHttpContextRequest(HttpContext context, IContentReaderWriterService readerWriter)
            : base(readerWriter)
        {
            _context = context;
        }

        public override IPrincipal User
        {
            get
            {
                return new GenericPrincipal(new GenericIdentity("Anonymous"), Array.Empty<string>());
            }
        }

        public override string Origin
        {
            get
            {
                //Remote ip address;
                return string.Empty;
            }
        }
        public override Stream InputStream
        {
            get
            {
                return _context.Request.Body;
            }
        }

        protected override string HttpRequestVerb
        {
            get
            {
                return _context.Request.Method;
            }
        }

        protected override string GetRequestPath()
        {

            return _context.Request.Path;
        }

        protected override IValueBag GetQueryString()
        {
            return new NameValueCollectionValueBag(HttpUtility.ParseQueryString(_context.Request.QueryString.ToUriComponent(), Encoding.UTF8));
        }

        protected override IValueBag GetRequestHeaders()
        {
            return new DictionaryValueBag(AsDictionary(_context.Request.Headers));
        }

        private IDictionary<string, string[]> AsDictionary(IHeaderDictionary headers)
        {
            return new Dictionary<string, string[]>(headers.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value.ToArray())));
        }
    }
}
