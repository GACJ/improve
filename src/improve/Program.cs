using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GACJ.Ringing;

namespace improve
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            WriteInfo();
            if (args.Length >= 1)
            {
                var input = args[0];
                try
                {
                    return Process(input);
                }
                catch (Exception ex)
                {
                    var prevColour = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = prevColour;
                    return 2;
                }
            }
            else
            {
                WriteUsage();
                return 1;
            }
        }

        private static int Process(string path)
        {
            var rows = ParseRows(path);
            if (rows.Length == 1)
                return 0;

            ExtendedProof(rows);

            return 0;
        }

        private static void ExtendedProof(Row[] rows)
        {
            int testStage = rows[0].Stage;
            Row rowmask = new Row(new string('x', testStage));
            bool distinct = false;
            var filteredRows = RemoveExtents(testStage, rowmask, rows, out distinct);
        }

        private static Row[] RemoveExtents(int testStage, Row rowmask, Row[] rows, out bool distinct)
        {
            testStage = 5;
            int position = 5;
            int extentLength = (int) Stage.GetFactorial(testStage);
            var rowGrps = rows.GroupBy(x => x[position]).OrderBy(y => y.Key);
            var newRows = new List<Row>();
            foreach (var grp in rowGrps)
            {
                if (grp.Count() >= extentLength)
                {
                    var ptable = new Dictionary<int, int>();
                    byte[] fixedbells = grp.First().ToArray();
                    foreach (var row in grp)
                    {
                        var hash = row.GetHashCode();
                        if (!ptable.TryAdd(hash, 1))
                            ptable[hash]++;
                        // Determine fixed bells
                        for (int i = 0; i < rows[0].Stage; i++)
                        {
                            if (row[i] != fixedbells[i])
                                fixedbells[i] = 255;
                        } 
                    }
                    // How many extents in the group
                    int numExtents = ptable.Count < extentLength ? 0 : ptable.Min(x => x.Value);
                    if (numExtents > 0)
                    {
                        // Use dictionary to control removal of the right number of rows
                        ptable = ptable.ToDictionary(p => p.Key, p => numExtents);
                        // Remove extents
                        foreach (var row in grp)
                        {
                            var hash = row.GetHashCode();
                            if (ptable[hash] > 0)
                            {
                                ptable[hash]--;
                                continue;
                            }
                            newRows.Add(row);
                        }
                    }
                    Console.WriteLine($"Contains {numExtents} {Stage.GetName(testStage)} {ExtentText(numExtents)} with {new Row(fixedbells)} as fixed bell(s).");
                }
                else
                    newRows.AddRange(grp);
            }

            // Check whether remaining rows are distinct
            distinct = true;
            var pt = new HashSet<int>();
            foreach (var row in newRows)
            {
                var hash = row.GetHashCode();
                if (!pt.Add(hash))
                {
                    distinct = false;
                    break;
                }
            }
            if (distinct)
                Console.WriteLine($"The remaining {newRows.Count} {Stage.GetName(testStage)} rows are distinct.");

            return newRows.ToArray();
        }

        private static void StandardProof(Row[] rows)
        {
            // Standard multi-stage proof
            int maxExtents = (int)((ulong)rows.Length / Stage.GetFactorial(rows[0].Stage)) + 1;
            var ps = new MultiExtentProofStrategy(rows[0].Stage, maxExtents);
            ps.Prove(rows, true);
            if (ps.IsTrue)
                Console.WriteLine($"Touch of {ps.TrueRows} changes is true.");
            else
                Console.WriteLine($"Touch is false with {ps.FalseRows} repeated rows.");

            Console.WriteLine($"Contains {ps.CompleteExtents} complete {ExtentText(ps.CompleteExtents)}.");
            Console.WriteLine($"Contains {ps.IncompleteExtents} incomplete {ExtentText(ps.IncompleteExtents)}.");

            // Check for Round Block
            if (ps.IsRoundBlock)
            {
                if (ps.InitialRow.IsRounds)
                    Console.WriteLine($"Touch is a Round Block starting with Rounds.");
                else
                    Console.WriteLine($"Touch is a Round Block starting with {ps.InitialRow}.");
            }
            else
            {
                Console.WriteLine($"Touch is a Non-Round Block starting with {ps.InitialRow} and finishing with {ps.FinalRow}.");
                Console.WriteLine("The final row will be included in the proof.");
            }

            if (ps.CompleteExtents == 1 && ps.IncompleteExtents == 1 && !ps.IsInternallyRound)
            {
                if (ps.InitialRow.IsRounds)
                    Console.WriteLine($"Touch does not contain Rounds internally.");
                else
                    Console.WriteLine($"Touch does not contain the initial row {ps.InitialRow} internally.");
            }
        }

        private static Row[] ParseRows(string inputPath)
        {
            var lines = File.ReadAllLines(inputPath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            if (lines.Length == 0)
            {
                throw new Exception("No rows found.");
            }

            var stage = lines[0].Length;
            Console.WriteLine($"Detected stage as {stage}.");
            var rows = new List<Row>();
            foreach (var line in lines)
            {
                if (!Row.TryParse(stage, line, out var row))
                {
                    throw new Exception($"Invalid row: {line}");
                }
                rows.Add(row);
            }

            if (rows.Count == 1)
                Console.WriteLine($"Parsed only one row {rows.First()}.");
            else
                Console.WriteLine($"Parsed {rows.Count} rows.");

            return rows.ToArray();
        }

        private static void WriteInfo()
        {
            Console.WriteLine("iMprove v1.0");
            Console.WriteLine("Copyright (C) Graham A C John 2018. All rights reserved.");
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Usage: improve <input>");
        }

        private static string ExtentText(int numExtents)
        {
            if (numExtents == 1)
                return "extent";
            return "extents";
        }
    }
}
