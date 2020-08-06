﻿using Arriba.Server.Application;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using System;

namespace Arriba.Communication.Server.Application
{
    public class ArribaManagementServiceFactory
    {
        private readonly DatabaseFactory databaseFactory;
        private readonly ClaimsAuthenticationService claimsAuthenticationService; 
        
        public ArribaManagementServiceFactory(DatabaseFactory databaseFactory)
        {
            if (databaseFactory == null) throw new ArgumentNullException(nameof(databaseFactory));
            
            this.databaseFactory = databaseFactory;
            claimsAuthenticationService = new ClaimsAuthenticationService();
        }

        public IArribaManagementService CreateArribaManagementService()
        {            
            return new ArribaManagement(databaseFactory, claimsAuthenticationService);
        }
    }
}
