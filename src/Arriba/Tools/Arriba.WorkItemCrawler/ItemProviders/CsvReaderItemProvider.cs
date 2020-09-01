﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Arriba.Diagnostics.Tracing;
using Arriba.ItemConsumers;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Structures;

namespace Arriba.ItemProviders
{
    // Fix scanning to skip rows before the changed date is into the range.
    // Read only to the end of the range.
    // Modify CsvWriter to write based on ChangedDate rather than crawl date, one per day/month.
    // Setup scripted pathway to crawl into CSV and then import into Arriba. Maybe delete CSV on success? Need design for how we know which steps succeeded.

    /// <summary>
    ///  CsvReadItemProvider. 
    ///    Reads from a CSV archive on disk to re-create an Arriba table.
    ///    Used for situations when Arriba is unstable so that crawl results
    ///    can be cached on disk and added to Arriba multiple times if needed.
    /// </summary>
    public class CsvReaderItemProvider
    {
        private string TableName { get; set; }
        private string ChangedDateColumn { get; set; }
        private DateTimeOffset Start { get; set; }
        private DateTimeOffset End { get; set; }

        private Queue<string> RemainingCsvs { get; set; }
        private CsvReader CurrentCsvReader { get; set; }
        private IEnumerator<CsvRow> CurrentRowEnumerator { get; set; }
        
        private readonly ILoggingContext _log;

        public CsvReaderItemProvider(string tableName, string changedDateColumn, DateTimeOffset start, DateTimeOffset end, ILoggingContext log)
        {
            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            _log = log;

            TableName = tableName;
            ChangedDateColumn = changedDateColumn;
            Start = start;
            End = end;

            // Find the set of CSVs to crawl the desired interval
            RemainingCsvs = new Queue<string>(FindCsvsBetween(start, end));

            // Open the first one and get the first row
            MoveNextCsv();
        }

        public DataBlock GetNextBlock(int maximumCount)
        {
            // If there were no matching CSVs, return immediately
            if (CurrentRowEnumerator == null) return null;

            DataBlock block = new DataBlock(CurrentCsvReader.ColumnNames, maximumCount);
            int rowCount = 0;

            // Read up to maximumCount rows from remaining CSVs
            do
            {
                while (CurrentRowEnumerator.MoveNext())
                {
                    // Copy this row to the block
                    CsvRow row = CurrentRowEnumerator.Current;
                    for (int columnIndex = 0; columnIndex < block.ColumnCount; ++columnIndex)
                    {
                        block.SetValue(rowCount, columnIndex, row[columnIndex]);
                    }

                    // Stop if the DataBlock is full
                    rowCount++;
                    if (rowCount == maximumCount) return block;
                }

                // If this is the end of the CSV, open the next one
            } while (MoveNextCsv());

            if (rowCount == 0) return null;

            // If we ran out of CSVs, set the DataBlock to the number of rows written and return the last block
            block.SetRowCount(rowCount);
            return block;
        }

        public void Dispose()
        {
            if (CurrentCsvReader != null)
            {
                CurrentCsvReader.Dispose();

                CurrentCsvReader = null;
                CurrentRowEnumerator = null;
            }
        }

        /// <summary>
        ///  Return the set of CSVs containing items with changed dates within the provided range.
        ///  CSVs are titled with the changed date [UTC] of the first item contained, so we want
        ///  the last CSV with a DateTime before start up to the last CSV with a DateTime before end.
        /// </summary>
        /// <remarks>
        ///  Ex: Suppose we want 2016-02-01 to 2016-02-03 from:
        ///   20160125   [Exclude: Next CSV name is still before start, so all items will be before start.]
        ///   20160130 * [Include: The last name before Start. 2016-02-01 is in here]
        ///   20160202 * [Include: Name is still before end. 2016-02-03 is in here]
        ///   20160204   [Exclude: Name is after end, so all items will be after end]
        /// </remarks>
        /// <param name="start">StartDate from which to include</param>
        /// <param name="end">EndDate before which to include</param>
        /// <returns>List of CsvPaths which contain all items between start and end</returns>
        private List<string> FindCsvsBetween(DateTimeOffset start, DateTimeOffset end)
        {
            List<string> result = new List<string>();

            string csvFolderPath = Path.Combine(BinarySerializable.CachePath, string.Format(CsvWriterItemConsumer.CsvPathForTable, TableName));
            foreach (string csvFilePath in BinarySerializable.EnumerateUnder(csvFolderPath))
            {
                if (Path.GetExtension(csvFilePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime csvFirstItemChangedDate = DateTime.ParseExact(Path.GetFileNameWithoutExtension(csvFilePath), CsvWriterItemConsumer.CsvFileNameDateTimeFormatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                    // If items from here are after the end, stop
                    if (csvFirstItemChangedDate > end)
                    {
                        break;
                    }

                    // If this name is before the start, all previous CSVs are not needed
                    if (csvFirstItemChangedDate < start)
                    {
                        result.Clear();
                    }

                    // Add this to the list
                    result.Add(csvFilePath);
                }
            }

            return result;
        }

        private bool MoveNextCsv()
        {
            if (RemainingCsvs.Count == 0)
            {
                _log.ProcessingComplete<CsvReaderItemProvider>();
                return false;
            }

            // Close the current CSV
            Dispose();

            // Open the next CSV and get the first row
            string nextCsvPath = RemainingCsvs.Dequeue();
            _log.LoadFile<CsvReaderItemProvider>(nextCsvPath);
            CurrentCsvReader = new CsvReader(new FileStream(nextCsvPath, FileMode.Open), new CsvReaderSettings() { MaximumSingleCellLines = -1 });
            CurrentRowEnumerator = CurrentCsvReader.Rows.GetEnumerator();

            return true;
        }
    }
}
