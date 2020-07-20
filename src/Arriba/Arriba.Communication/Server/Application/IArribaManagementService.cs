using Arriba.Types;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Communication.Server.Application
{
    public interface IArribaManagementService
    {
        IEnumerable<string> GetTables();

        IDictionary<string, TableInformation> GetTablesForUser(IPrincipal user);

        bool UnloadTableForUser(string tableName, IPrincipal user);

        bool UnloadAllTableForUser(IPrincipal user);

        TableInformation GetTableInformationForUser(string tableName, IPrincipal user);
    }
}
