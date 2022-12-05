using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace GeneMatchingCalculator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int MIN_WINDOW = 3;
            int MAX_WINDOW = 6;


            Console.WriteLine("Enter Minimum Window Size: ");
            int.TryParse(Console.ReadLine(), out MIN_WINDOW);

            Console.WriteLine("Enter Maximum Window Size: ");
            int.TryParse(Console.ReadLine(), out MAX_WINDOW);

            Console.Clear();
            Console.WriteLine("Enter Directory Path: ");
            var dirPath = (Console.ReadLine()).Trim();

            if (!Directory.Exists(dirPath)) throw new Exception("Please enter a valid directory path!.");

            reread:
            var filesInfo = ReadFiles(dirPath);
            var fileWithInvalidName = filesInfo.FirstOrDefault(f => f.Key.Contains(","));
            if (fileWithInvalidName != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{fileWithInvalidName.Key}] this file contains a comma in its name please remove any commas or special characters.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Fix file name and press any button");
                Console.ReadKey();
                goto reread;
            }
            else Console.ResetColor();

            Console.Clear();

            if (!Directory.Exists(Path.Combine(dirPath, "stats")))
                Directory.CreateDirectory(Path.Combine(dirPath, "stats"));
            var outputPath = Path.Combine(dirPath, "stats", $"[{filesInfo.Length}-{MIN_WINDOW}-{MAX_WINDOW}]_{DateTime.Now.Ticks}.csv");

            var logs = new Dictionary<int, Dictionary<string, MatchInfo[]>>();


            var start = DateTime.Now;
            using (StreamWriter writer = new StreamWriter(outputPath))
                writer.WriteLine($"Piece,Length,{string.Join(",", filesInfo.Select(x => $"{x.Key}_Count,{x.Key}_Indexes"))}");

            var window = MIN_WINDOW;
            while (window <= MAX_WINDOW)
            {
                var readLog = new Dictionary<string, MatchInfo[]>();
                logs[window] = new Dictionary<string, MatchInfo[]>();
                if (window - MIN_WINDOW > 0)
                {
                    readLog = new Dictionary<string, MatchInfo[]>(logs[window - 1]);
                    logs[window - 1].Clear();
                    logs[window - 1] = null;
                }

                Console.WriteLine($"Sequencing window of size: {window}");

                for (int i = 0; i < filesInfo.Length; i++)
                {
                    if (filesInfo[i].Value.Length <= window) continue;

                    Console.WriteLine($"Looking in file: {filesInfo[i].Key} with window of size: {window}");
                    var pieceStart = 0;
                    while (pieceStart <= filesInfo[i].Value.Length - window)
                    {
                        var piece = filesInfo[i].Value.Substring(pieceStart, window);
                        if (!logs[window].ContainsKey(piece))
                        {

                            var startsWithPiece = piece.Substring(0, piece.Length - 1);
                            var search = piece.Last();
                            var cache = readLog.ContainsKey(startsWithPiece) ? readLog[startsWithPiece] : new MatchInfo[0];

                            var matches = filesInfo.Select(f =>
                            {
                                int[] matchesIndexes;
                                var inCahce = cache.Where(c => c.File == f.Key).SelectMany(x => x.Indexes).ToArray();
                                if (inCahce.Any())
                                {
                                    int length = startsWithPiece.Length;
                                    matchesIndexes = inCahce.Select(idx =>
                                    {
                                        if (f.Value.Length <= idx + length) return -1;
                                        var str = f.Value.Substring(idx + length, 1);
                                        return str.IndexOf(search) > -1 ? idx : -1;
                                    }).Where(x => x > -1).ToArray();
                                }
                                else matchesIndexes = FindMatches(f.Value, piece);
                                return new MatchInfo { Piece = piece, File = f.Key, Count = matchesIndexes.Length, Indexes = matchesIndexes };
                            }).ToArray();

                            logs[window].Add(piece, matches);

                        }
                        pieceStart++;
                    }
                    Console.WriteLine($"Looking in file: {filesInfo[i].Key} with window of size: {window}, [DONE] after {pieceStart - 1} slides.");
                }

                using (StreamWriter writer = new StreamWriter(outputPath, true))
                {
                    foreach (var pair in logs[window])
                    {
                        var sum = 0;
                        var row = $"{pair.Key},{pair.Key.Length}";
                        foreach (var f in filesInfo)
                        {
                            var fIdxes = pair.Value.Where(m => m.File == f.Key).SelectMany(z => z.Indexes);
                            sum += fIdxes.Count();
                            row += $",{fIdxes.Count()},{string.Join("|", fIdxes.Select(i => i + 1))}";
                        }
                        if (sum <= 1) continue;
                        Console.WriteLine(row);
                        writer.WriteLine(row);
                    }
                }

                window++;
            }
            var finished = DateTime.Now;

            Console.WriteLine($"Finished in: {(finished - start).TotalMinutes} Minutes");

            Console.WriteLine($"Results saved in: {outputPath}");
            Console.ReadLine();
        }

        private static int[] FindMatches(string str, string pattern)
        {
            var concurrencies = new List<int>();
            int idx = -1;
            do
            {
                idx = str.IndexOf(pattern, idx + 1);
                if (idx > -1) concurrencies.Add(idx);
            } while (idx > -1);

            return concurrencies.ToArray();
        }
        private static FileInfo[] ReadFiles(string dirPath)
        {
            var files = Directory.EnumerateFiles(dirPath, "*.txt");
            return files.Select(f => new FileInfo { Key = Path.GetFileName(f).Replace(".txt", ""), Value = File.ReadAllText(f).Replace(" ", "") }).ToArray();
        }
    }
}

class MatchInfo
{
    public string Piece { get; set; }
    public string File { get; set; }
    public int Count { get; set; }
    public int[] Indexes { get; set; }
}
class FileInfo
{
    public string Key { get; set; }
    public string Value { get; set; }
}