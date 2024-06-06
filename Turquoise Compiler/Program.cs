﻿using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Compiler;

class Program {
	static string in_file_path = @"turquoise.tq";
	const string out_file_path = @"out.asm";
	const string assembler_command = @"nasm -f macho64 -o out.o out.asm";
	const string linker_command = @"ld -e _start out.o -o out -macosx_version_min 11.0 -L /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/lib -lSystem";

    static void Main(string[] args) {
		if (args.Length > 0) {
			in_file_path = args[0];
		}

		if (!File.Exists(in_file_path)) throw new FileNotFoundException();
		string input_file_contents = File.ReadAllText(in_file_path);
		string output_file_contents = Compile(input_file_contents);

		File.CreateText(out_file_path);
		File.AppendAllText(out_file_path, output_file_contents);

		ExecuteCommand(assembler_command);
		ExecuteCommand(linker_command);
	}

    static string Compile(string input_file_contents) {
		List<Token> tokens = Tokenizer.Tokenize(input_file_contents);
		NodeExit? tree = Parser.Parse(tokens);

		if (!tree.HasValue) {
			throw new Exception("No exit statement found");
		}

		return Generator.Generate(tree.Value);
	}



	static void ExecuteCommand(string command) {
        ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        Process process = new Process {
			StartInfo = psi
		};
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(output)) Console.WriteLine($"Output: {output}");
        if (!string.IsNullOrEmpty(error)) Console.WriteLine($"Error: {error}");
        process.WaitForExit();
    }
}