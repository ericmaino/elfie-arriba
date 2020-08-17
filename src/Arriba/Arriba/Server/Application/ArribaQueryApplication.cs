// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Structures;
using System.Collections.Specialized;
using Arriba.ParametersCheckers;

namespace Arriba.Server
{
    /// <summary>
    /// Arriba restful application for query operations.
    /// </summary>
    public class ArribaQueryApplication : ArribaApplication
    {
        private const string DefaultFormat = "dictionary";

        public ArribaQueryApplication(DatabaseFactory f, ClaimsAuthenticationService auth, ISecurityConfiguration securityConfiguration)
            : base(f, auth, securityConfiguration)
        {
            // /table/foo?type=select
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.Select);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.Select);

            // /table/foo?type=distinct
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.Distinct);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.Distinct);

            // /table/foo?type=aggregate
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.Aggregate);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.Aggregate);

            this.GetAsync(new RouteSpecification("/allCount"), this.AllCount);
            this.GetAsync(new RouteSpecification("/suggest"), this.Suggest);
        }

        private async Task<IResponse> Select(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            string outputFormat = ctx.Request.ResourceParameters["fmt"];
            Table table = this.Database[tableName];

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to select from.");
            }

            try
            {
                var result = Select(tableName, ctx, p, user);
                var query = result.Query as SelectQuery;

                // Format the result in the return format
                switch ((outputFormat ?? "").ToLowerInvariant())
                {
                    case "":
                    case "json":
                        return ArribaResponse.Ok(result);
                    case "csv":
                        return ToCsvResponse(result, $"{tableName}-{DateTime.Now:yyyyMMdd}.csv");
                    case "rss":
                        // If output rss only the ID column
                        query.Columns = new string[] { table.IDColumn.Name };
                        return ToRssResponse(result, "", $"{query.TableName} : {query.Where}", ctx.Request.ResourceParameters["iURL"]);
                    default:
                        throw new ArgumentException($"OutputFormat [fmt] passed, '{outputFormat}', was invalid.");
                }
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }
        }

        private SelectResult Select(string tableName, ITelemetry telemetry, NameValueCollection parameters, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));
            Database.ThrowIfTableNotFound(tableName);

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            var table = this.Database[tableName];
            var query = SelectQueryFromRequest(this.Database, parameters);
            SelectResult result = null;

            // If no columns were requested get only the ID column
            if (query.Columns == null || query.Columns.Count == 0)
            {
                query.Columns = new string[] { table.IDColumn.Name };
            }

            // Read Joins, if passed
            IQuery<SelectResult> wrappedQuery = WrapInJoinQueryIfFound(query, this.Database, parameters);

            ICorrector correctors = this.CurrentCorrectors(user);
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run server correctors
                wrappedQuery.Correct(correctors);
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Select", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                // Run the query
                result = this.Database.Query(wrappedQuery, (si) => this.IsInIdentity(user, si));
            }

            // Canonicalize column names (if query successful)
            if (result.Details.Succeeded)
            {
                query.Columns = result.Values.Columns.Select((cd) => cd.Name).ToArray();
            }

            return result;

        }

        private SelectQuery SelectQueryFromRequest(Database db, NameValueCollection p)
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

        private static IResponse ToCsvResponse(SelectResult result, string fileName)
        {
            const string outputMimeType = "text/csv; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                var items = result.Values;

                // ***Crazy Excel Business***
                // This is pretty ugly. If the first 2 chars in a CSV file as ID, then excel is  thinks the file is a SYLK 
                // file not a CSV File (!) and will alert the user. Excel does not care about output mime types. 
                // 
                // To work around this, and have a _nice_ experience for csv export, we'll modify 
                // the first column name to " ID" to trick Excel. It's not perfect, but it'll do.
                // 
                // As a mitigation for round-tripping, the CsvReader will trim column names. Sigh. 
                List<string> columns = new List<string>();

                foreach (ColumnDetails column in items.Columns)
                {
                    if (columns.Count == 0 && column.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        columns.Add(" ID");
                    }
                    else
                    {
                        columns.Add(column.Name);
                    }
                }

                CsvWriter writer = new CsvWriter(context, columns);

                for (int row = 0; row < items.RowCount; ++row)
                {
                    for (int col = 0; col < items.ColumnCount; ++col)
                    {
                        writer.AppendValue(items[row, col]);
                    }

                    writer.AppendRowSeparator();
                }

                context.Writer.Flush();
                await s.FlushAsync();
            });

            resp.AddHeader("Content-Disposition", String.Concat("attachment;filename=\"", fileName, "\";"));

            return resp;
        }

        private static IResponse ToRssResponse(SelectResult result, string rssUrl, string query, string itemUrlWithoutId)
        {
            DateTime utcNow = DateTime.UtcNow;

            const string outputMimeType = "application/rss+xml; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                RssWriter w = new RssWriter(context);

                ByteBlock queryBB = (ByteBlock)query;
                w.WriteRssHeader(queryBB, queryBB, rssUrl, utcNow, TimeSpan.FromHours(1));

                ByteBlock baseLink = itemUrlWithoutId;
                var items = result.Values;
                for (int row = 0; row < items.RowCount; ++row)
                {
                    ByteBlock id = ConvertToByteBlock(items[row, 0]);
                    w.WriteItem(id, id, id, baseLink, utcNow);
                }

                w.WriteRssFooter();

                context.Writer.Flush();
                await s.FlushAsync();
            });

            return resp;
        }

        private static ByteBlock ConvertToByteBlock(object value)
        {
            if (value == null) return ByteBlock.Zero;

            if (value is ByteBlock)
            {
                return (ByteBlock)value;
            }
            else if (value is string)
            {
                return (ByteBlock)value;
            }
            else
            {
                return (ByteBlock)(value.ToString());
            }
        }

        private AllCountResult AllCount(ITelemetry telemetry, NameValueCollection parameters, IPrincipal user)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));

            if (!ValidateDatabaseAccessForUser(user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            string queryString = parameters["q"] ?? "";
            AllCountResult result = new AllCountResult(queryString);

            // Build a Count query
            IQuery<AggregationResult> query = new AggregationQuery("count", null, queryString);

            // Wrap in Joins, if found
            query = WrapInJoinQueryIfFound(query, this.Database, parameters);

            // Run server correctors
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "AllCount", detail: query.Where.ToString()))
            {
                query.Correct(this.CurrentCorrectors(user));
            }

            // Accumulate Results for each table
            using (telemetry.Monitor(MonitorEventLevel.Information, "AllCount", type: "AllCount", detail: query.Where.ToString()))
            {
                IExpression defaultWhere = query.Where;

                foreach (string tableName in this.Database.TableNames)
                {
                    if (this.HasTableAccess(tableName, user, PermissionScope.Reader))
                    {
                        query.TableName = tableName;
                        query.Where = defaultWhere;

                        AggregationResult tableCount = this.Database.Query(query, (si) => this.IsInIdentity(user, si));

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

        private async Task<IResponse> AllCount(IRequestContext ctx, Route route)
        {
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            var user = ctx.Request.User;
            AllCountResult result;

            try
            {
                result = AllCount(ctx, p, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

        private IntelliSenseResult Suggest(ITelemetry telemetry, NameValueCollection parameters, IPrincipal user)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            user.ThrowIfNull(nameof(user));

            if (!ValidateDatabaseAccessForUser(user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            IntelliSenseResult result = null;
            string query = parameters["q"];
            string selectedTable = parameters["t"];

            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Suggest", type: "Suggest", detail: query))
            {
                // Get all available tables
                List<Table> tables = new List<Table>();
                foreach (string tableName in this.Database.TableNames)
                {
                    if (this.HasTableAccess(tableName, user, PermissionScope.Reader))
                    {
                        if (String.IsNullOrEmpty(selectedTable) || selectedTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        {
                            tables.Add(this.Database[tableName]);
                        }
                    }
                }

                // Get IntelliSense results and return
                QueryIntelliSense qi = new QueryIntelliSense();
                result = qi.GetIntelliSenseItems(query, tables);
            }

            return result;
        }

        private async Task<IResponse> Suggest(IRequestContext ctx, Route route)
        {
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            IPrincipal user = ctx.Request.User;
            IntelliSenseResult result;

            try
            {
                result = Suggest(ctx, p, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }


        private T Query<T>(string tableName, ITelemetry telemetry, IQuery<T> query, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            ParamChecker.ThrowIfNull(query, nameof(query));
            user.ThrowIfNull(nameof(user));
            Database.ThrowIfTableNotFound(tableName);

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Not authorized");

            query.TableName = tableName;

            // Correct the query with default correctors
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Correct", type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                query.Correct(this.CurrentCorrectors(user));
            }

            // Execute and return results for the query
            using (telemetry.Monitor(MonitorEventLevel.Information, query.GetType().Name, type: "Table", identity: tableName, detail: query.Where.ToString()))
            {
                T result = this.Database.Query(query, (si) => this.IsInIdentity(user, si));
                return result;
            }
        }

        private IResponse Query<T>(IRequestContext ctx, Route route, IQuery<T> query, NameValueCollection p)
        {
            IQuery<T> wrappedQuery = WrapInJoinQueryIfFound(query, this.Database, p);
            var user = ctx.Request.User;

            // Ensure the table exists and set it on the query
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to query.");
            }

            T result;
            try
            {
                result = Query(tableName, ctx, wrappedQuery, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

        private async Task<IResponse> Aggregate(IRequestContext ctx, Route route)
        {
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            IQuery<AggregationResult> query = BuildAggregateFromContext(ctx, p);
            return Query(ctx, route, query, p);
        }

        private AggregationQuery BuildAggregateFromContext(ITelemetry telemetry, NameValueCollection parameters)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));

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

        private async Task<IResponse> Distinct(IRequestContext ctx, Route route)
        {
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            IQuery<DistinctResult> query = BuildDistinctFromContext(ctx, p);
            return Query(ctx, route, query, p);
        }

        private DistinctQuery BuildDistinctFromContext(ITelemetry telemetry, NameValueCollection parameters)
        {
            ParamChecker.ThrowIfNull(telemetry, nameof(telemetry));
            parameters.ThrowIfNullOrEmpty(nameof(parameters));

            DistinctQueryTop query = new DistinctQueryTop();
            query.Column = parameters["col"];
            if (String.IsNullOrEmpty(query.Column)) throw new ArgumentException("Distinct Column [col] must be passed.");

            string queryString = parameters["q"];
            using (telemetry.Monitor(MonitorEventLevel.Verbose, "Arriba.ParseQuery", String.IsNullOrEmpty(queryString) ? "<none>" : queryString))
            {
                query.Where = String.IsNullOrEmpty(queryString) ? new AllExpression() : SelectQuery.ParseWhere(queryString);
            }

            string take = parameters["t"];
            if (!String.IsNullOrEmpty(take)) query.Count = UInt16.Parse(take);

            return query;
        }
    }
}
