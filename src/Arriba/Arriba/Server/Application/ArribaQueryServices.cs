using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;
using Arriba.Server.Authorization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Principal;

namespace Arriba.Communication.Server.Application
{
    public class ArribaQueryServices : IArribaQueryServices
    {
        private readonly SecureDatabase _database;
        private readonly IArribaAuthorization _arribaAuthorization;
        private readonly ICorrector _correctors;

        public ArribaQueryServices(SecureDatabase secureDatabase, ICorrector composedCorrector, ClaimsAuthenticationService claims, ISecurityConfiguration securityConfiguration)
        {
            _database = secureDatabase;
            _arribaAuthorization = new ArribaAuthorizationGrantDecorator(_database, claims, securityConfiguration);
            _correctors = composedCorrector;
        }

        private ICorrector CurrentCorrectors(IPrincipal user)
        {
            user.ThrowIfNull(nameof(user));

            if (user.Identity == null)
                throw new ArgumentException("User has no identity", nameof(user));

            // Add the 'MeCorrector' for the requesting user (must be first, to chain with the UserAliasCorrector)
            return new ComposedCorrector(new MeCorrector(user.Identity.Name), _correctors);
        }

        private SelectQuery SelectQueryFromRequest(NameValueCollection p)
        {
            SelectQuery query = new SelectQuery();

            query.Where = SelectQuery.ParseWhere(p["q"]);
            query.OrderByColumn = p["ob"];
            query.Columns = ReadParameterSet(p, "c", "cols");

            string take = p["t"];
            if (!String.IsNullOrEmpty(take)) query.Count = UInt16.Parse(take);

            string sortOrder = p["so"] ?? "";
            switch (sortOrder.ToLowerInvariant())
            {
                case "":
                case "asc":
                    query.OrderByDescending = false;
                    break;
                case "desc":
                    query.OrderByDescending = true;
                    break;
                default:
                    throw new ArgumentException($"SortOrder [so] passed, '{sortOrder}' was not 'asc' or 'desc'.");
            }

            string highlightString = p["h"];
            if (!String.IsNullOrEmpty(highlightString))
            {
                // Set the end highlight string to the start highlight string if it is not set. 
                query.Highlighter = new Highlighter(highlightString, p["h2"] ?? highlightString);
            }

            return query;
        }

        /// <summary>
        ///  Read a set of parameters into a List (C1=X&C2=Y&C3=Z) => { "X", "Y", "Z" }.
        /// </summary>
        /// <param name="request">IRequest to read from</param>
        /// <param name="baseName">Parameter name before numbered suffix ('C' -> look for 'C1', 'C2', ...)</param>
        /// <returns>List&lt;string&gt; containing values for the parameter set, if any are found, otherwise an empty list.</returns>
        protected static List<string> ReadParameterSet(NameValueCollection parameters, string baseName)
        {
            List<string> result = new List<string>();

            int i = 1;
            while (true)
            {
                string value = parameters[baseName + i.ToString()];
                if (String.IsNullOrEmpty(value)) break;

                result.Add(value);
                ++i;
            }

            return result;
        }

        /// <summary>
        ///  Read a set of parameters into a List, allowing a single comma-delimited fallback value.
        ///  (C1=X&C2=Y&C3=Z or Cols=X,Y,Z) => { "X", "Y", "Z" }
        /// </summary>
        /// <param name="request">IRequest to read from</param>
        /// <param name="nameIfSeparate">Parameter name prefix if parameters are passed separately ('C' -> look for 'C1', 'C2', ...)</param>
        /// <param name="nameIfDelimited">Parameter name if parameters are passed together comma delimited ('Cols')</param>
        /// <returns>List&lt;string&gt; containing values for the parameter set, if any are found, otherwise an empty list.</returns>
        protected static List<string> ReadParameterSet(NameValueCollection parameters, string nameIfSeparate, string nameIfDelimited)
        {
            List<string> result = ReadParameterSet(parameters, nameIfSeparate);

            if (result.Count == 0)
            {
                string delimitedValue = parameters[nameIfDelimited];
                if (!String.IsNullOrEmpty(delimitedValue))
                {
                    result = new List<string>(delimitedValue.Split(','));
                }
            }

            return result;
        }


        private DistinctQuery BuildDistinctFromContext(ITelemetry telemetry, NameValueCollection parameters)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));

            DistinctQueryTop query = new DistinctQueryTop();
            query.Column = parameters["col"];
            query.Column.ThrowIfNullOrWhiteSpaced(nameof(query.Column), "Distinct Column [col] must be passed.");

            string queryString = parameters["q"];
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            string take = parameters["t"];
            if (!String.IsNullOrEmpty(take)) query.Count = UInt16.Parse(take);

            return query;
        }

        private AggregationQuery BuildAggregateFromContext(ITelemetry telemetry, NameValueCollection parameters)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));

            string aggregationFunction = parameters["a"] ?? "count";
            string columnName = parameters["col"];
            string queryString = parameters["q"];

            AggregationQuery query = new AggregationQuery();
            query.Aggregator = AggregationQuery.BuildAggregator(aggregationFunction);
            query.AggregationColumns = String.IsNullOrEmpty(columnName) ? null : new string[] { columnName };

            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            for (char dimensionPrefix = 'd'; true; ++dimensionPrefix)
            {
                List<string> dimensionParts = ReadParameterSet(parameters, dimensionPrefix.ToString());
                if (dimensionParts.Count == 0) break;

                if (dimensionParts.Count == 1 && dimensionParts[0].EndsWith(">"))
                {
                    query.Dimensions.Add(new DistinctValueDimension(QueryParser.UnwrapColumnName(dimensionParts[0].TrimEnd('>'))));
                }
                else
                {
                    query.Dimensions.Add(new AggregationDimension("", dimensionParts));
                }
            }

            return query;
        }

        private static IQuery<T> WrapInJoinQueryIfFound<T>(IQuery<T> primaryQuery, Database db, NameValueCollection p)
        {
            List<SelectQuery> joins = new List<SelectQuery>();

            List<string> joinQueries = ReadParameterSet(p, "q");
            List<string> joinTables = ReadParameterSet(p, "t");

            for (int queryIndex = 0; queryIndex < Math.Min(joinQueries.Count, joinTables.Count); ++queryIndex)
            {
                joins.Add(new SelectQuery() { Where = SelectQuery.ParseWhere(joinQueries[queryIndex]), TableName = joinTables[queryIndex] });
            }

            if (joins.Count == 0)
            {
                return primaryQuery;
            }
            else
            {
                return new JoinQuery<T>(db, primaryQuery, joins);
            }
        }

        private T Query<T>(string tableName, ITelemetry telemetry, IQuery<T> query, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            ParamChecker.ThrowIfNull(query, nameof(query));
            user.ThrowIfNull(nameof(user));
            _database.ThrowIfTableNotFound(tableName);

            if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            query.TableName = tableName;

            // Correct the query with default correctors
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                query.Correct(CurrentCorrectors(user));
            }

            // Execute and return results for the query
            using (telemetry.Monitor(MonitorEventLevel.Information, query.GetType().Name, type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                T result = _database.Query(query, (si) => _arribaAuthorization.IsInIdentity(user, si));
                return result;
            }
        }

        public SelectResult QueryTableForUser(string tableName, NameValueCollection parameters, ITelemetry telemetry, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));
            _database.ThrowIfTableNotFound(tableName);

            if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            var table = _database[tableName];
            var query = SelectQueryFromRequest(parameters);
            query.TableName = tableName;

            SelectResult result = null;

            // If no columns were requested get only the ID column
            if (query.Columns == null || query.Columns.Count == 0)
            {
                query.Columns = new string[] { table.IDColumn.Name };
            }

            // Read Joins, if passed
            IQuery<SelectResult> wrappedQuery = WrapInJoinQueryIfFound(query, _database, parameters);

            ICorrector correctors = this.CurrentCorrectors(user);
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                wrappedQuery.Correct(correctors);
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Select", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                result = _database.Query(wrappedQuery, (si) => _arribaAuthorization.IsInIdentity(user, si));
            }

            // Canonicalize column names (if query successful)
            if (result.Details.Succeeded)
            {
                query.Columns = result.Values.Columns.Select((cd) => cd.Name).ToArray();
            }

            return result;
        }

        public DistinctResult DistinctQueryTableForUser(string tableName, NameValueCollection parameters, ITelemetry telemetry, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            user.ThrowIfNull(nameof(user));
            _database.ThrowIfTableNotFound(tableName);

            if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            IQuery<DistinctResult> query = BuildDistinctFromContext(telemetry, parameters);

            var wrappedQuery = WrapInJoinQueryIfFound(query, _database, parameters);

            return Query(tableName, telemetry, wrappedQuery, user);
        }

        public AggregationResult AggregateQueryTableForUser(string tableName, NameValueCollection parameters, ITelemetry telemetry, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            user.ThrowIfNull(nameof(user));
            _database.ThrowIfTableNotFound(tableName);

            if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            IQuery<AggregationResult> query = BuildAggregateFromContext(telemetry, parameters);

            var wrappedQuery = WrapInJoinQueryIfFound(query, _database, parameters);

            return Query(tableName, telemetry, wrappedQuery, user);
        }

        public IntelliSenseResult IntelliSenseTableForUser(NameValueCollection parameters, ITelemetry telemetry, IPrincipal user)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));

            if (!_arribaAuthorization.ValidateDatabaseAccessForUser(user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            IntelliSenseResult result = null;
            string query = parameters["q"];
            string selectedTable = parameters["t"];

            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Suggest", type: "Suggest", detail: query))
            {
                // Get all available tables
                List<Table> tables = new List<Table>();
                foreach (string tableName in _database.TableNames)
                {
                    if (_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                    {
                        if (String.IsNullOrEmpty(selectedTable) || selectedTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        {
                            tables.Add(_database[tableName]);
                        }
                    }
                }

                // Get IntelliSense results and return
                QueryIntelliSense qi = new QueryIntelliSense();
                result = qi.GetIntelliSenseItems(query, tables);
            }

            return result;
        }

        public AllCountResult AllCountForUser(NameValueCollection parameters, ITelemetry telemetry, IPrincipal user)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));

            if (!_arribaAuthorization.ValidateDatabaseAccessForUser(user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            string queryString = parameters["q"] ?? "";
            AllCountResult result = new AllCountResult(queryString);

            // Build a Count query
            IQuery<AggregationResult> query = new AggregationQuery("count", null, queryString);

            // Wrap in Joins, if found
            query = WrapInJoinQueryIfFound(query, _database, parameters);

            // Run server correctors
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "AllCount", detail: query.Where.ToString()))
            {
                query.Correct(this.CurrentCorrectors(user));
            }

            // Accumulate Results for each table
            using (telemetry.Monitor(MonitorEventLevel.Information, "AllCount", type: "AllCount", detail: query.Where.ToString()))
            {
                IExpression defaultWhere = query.Where;

                foreach (string tableName in _database.TableNames)
                {
                    if (_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                    {
                        query.TableName = tableName;
                        query.Where = defaultWhere;

                        AggregationResult tableCount = _database.Query(query, (si) => _arribaAuthorization.IsInIdentity(user, si));

                        if (!tableCount.Details.Succeeded || tableCount.Values == null)
                        {
                            result.ResultsPerTable.Add(new CountResult(tableName, 0, true, false));
                        }
                        else
                        {
                            result.ResultsPerTable.Add(new CountResult(tableName, (ulong)tableCount.Values[0, 0], true, tableCount.Details.Succeeded));
                        }
                    }
                    else
                    {
                        result.ResultsPerTable.Add(new CountResult(tableName, 0, false, false));
                    }
                }
            }

            // Sort results so that succeeding tables are first and are subsorted by count [descending]
            result.ResultsPerTable.Sort((left, right) =>
            {
                int order = right.Succeeded.CompareTo(left.Succeeded);
                if (order != 0) return order;

                return right.Count.CompareTo(left.Count);
            });

            return result;
        }
    }
}
