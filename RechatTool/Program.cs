﻿// --------------------------------------------------------------------------------
// Copyright (c) J.D. Purcell
//
// Licensed under the MIT License (see LICENSE.txt)
// --------------------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;

namespace RechatTool {
	internal class Program {
		public const string Version = "1.1.0.0";

		private static int Main(string[] args) {
			int iArg = 0;
			string GetArg(bool optional = false) =>
				iArg < args.Length ? args[iArg++] : optional ? (string)null : throw new InvalidArgumentException();

			try {
				string mode = GetArg();
				if (mode == "-d" || mode == "-D") {
					string videoIdStr = GetArg();
					long videoId = videoIdStr.TryParseInt64() ??
						TryParseVideoIdFromUrl(videoIdStr) ??
						throw new InvalidArgumentException();
					string path = GetArg(true) ?? $"{videoId}.json";
					void UpdateProgress(int downloaded, int total) {
						Console.Write($"\rDownloaded {downloaded} of {total}");
					}
					Rechat.DownloadFile(videoId, path, false, UpdateProgress);
					if (mode == "-D") {
						Rechat.ProcessFile(path);
					}
					Console.WriteLine();
					Console.WriteLine("Done!");
				}
				else if (mode == "-p") {
					string[] paths = { GetArg() };
					if (paths[0].IndexOfAny(new[] { '*', '?'}) != -1) {
						paths = Directory.GetFiles(Path.GetDirectoryName(paths[0]), Path.GetFileName(paths[0]));
					}
					foreach (string p in paths) {
						Console.WriteLine("Processing " + Path.GetFileName(p));
						Rechat.ProcessFile(p);
					}
					Console.WriteLine("Done!");
				}
				else {
					throw new InvalidArgumentException();
				}

				return 0;
			}
			catch (InvalidArgumentException) {
				Console.WriteLine($"RechatTool v{new Version(Version).ToDisplayString()}");
				Console.WriteLine();
				Console.WriteLine("Modes:");
				Console.WriteLine("   -d videoid [path]");
				Console.WriteLine("      Downloads chat replay for the specified videoid. If path is not");
				Console.WriteLine("      specified, the output is saved to the current directory.");
				Console.WriteLine("   -D videoid [path]");
				Console.WriteLine("      Downloads and processes chat replay (combines -d and -p).");
				Console.WriteLine("   -p path");
				Console.WriteLine("      Processes a JSON chat replay file and outputs a human-readable text file.");
				Console.WriteLine("      Output is written to same folder as the input file with the extension");
				Console.WriteLine("      changed to .txt.");
				return 1;
			}
			catch (Exception ex) {
				Console.WriteLine("\rError: " + ex.Message);
				return 1;
			}
		}

		private static long? TryParseVideoIdFromUrl(string s) {
			string[] hosts = { "twitch.tv", "www.twitch.tv" };
			if (!s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
				s = "https://" + s;
			}
			if (!Uri.TryCreate(s, UriKind.Absolute, out Uri uri)) return null;
			if (!hosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase))) return null;
			if (!uri.AbsolutePath.StartsWith("/videos/", StringComparison.Ordinal)) return null;
			return uri.AbsolutePath.Substring(8).TryParseInt64();
		}

		private class InvalidArgumentException : Exception { }
	}
}
