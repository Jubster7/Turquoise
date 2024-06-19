﻿using System.Diagnostics;
using static System.Environment;

namespace Turquoise;
class Program {
	static string in_file_path = @"turquoise.tq";
	const string out_file_path = @"out.asm";
	const string out_object_file_path = @"out.o";
	const string out_executable_file_path = @"out";

	public const string assembly_entry_point_label = "_main";

	const string assembler_command = @"nasm -f macho64 " + out_file_path;
	const string linker_command = @"gcc -arch x86_64 -o " + out_executable_file_path + " " + out_object_file_path;
	const bool throw_on_compile_error = true;

	static void Main(string[] args) {
		if (args.Length == 1) {
			in_file_path = args[0];
		} else if (args.Length > 1) {
			Error("Error: Expected Usage:\nTurquoise <input file>");
		}

		if (!File.Exists(in_file_path)) Error("File: "+ in_file_path +" not found");
		string input_file_contents = File.ReadAllText(in_file_path);
		string output_file_contents = Compile(input_file_contents);

		File.CreateText(out_file_path);
		File.AppendAllText(out_file_path, output_file_contents);

		ExecuteCommand(assembler_command + " && " + linker_command);
	}

	static string Compile(string input_file_contents) {
		Console.ForegroundColor = ConsoleColor.Red;
		List<Token> tokens = Tokenizer.Tokenize(input_file_contents);
		NodeProgram program = Parser.Parse(tokens);
		return Generator.Generate(program);
	}

	static void ExecuteCommand(string command) {
		Process process = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = $"-c \"{command}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			}
		};
		process.Start();
		string output = process.StandardOutput.ReadToEnd();
		string error = process.StandardError.ReadToEnd();
		Console.ForegroundColor = ConsoleColor.Yellow;
		if (!string.IsNullOrEmpty(output)) Console.WriteLine($"Command Output: {output}");
		Console.ForegroundColor = ConsoleColor.Red;
		if (!string.IsNullOrEmpty(error)) Console.WriteLine($"Command Error: {error}");
		process.WaitForExit();
	}

	public static void Error(string error_message) {
		if (throw_on_compile_error) {
			throw new Exception(error_message);
		} else {
			Console.Error.WriteLine(error_message);
			Exit(0);
		}
	}
}