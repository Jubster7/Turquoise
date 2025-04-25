using System.Diagnostics;
using static System.Environment;

namespace Turquoise;

static class Program {
	static string In_file_path = @"turquoise.tq";
	const string Out_file_path = @"out.asm";
	const string Out_object_file_path = @"out.o";
	//const string out_executable_file_path = @"out";

	public const string Assembly_entry_point_label = "_main";

	const string Assembler_command = @"nasm -f macho64 " + Out_file_path;
	//const string linker_command = @"gcc -arch x86_64 -o " + out_executable_file_path + " " + out_object_file_path;
	const bool Exception_on_compile_error = false;

	static void Main(string[] args) {
		if (args.Length == 1) {
			In_file_path = args[0];
		} else if (args.Length > 1) {
			Error("Error: Expected Usage:\nTurquoise <input file>");
		}

		if (!File.Exists(In_file_path)) Error("File: " + In_file_path + " not found");
		string input_file_contents = File.ReadAllText(In_file_path);
		string output_file_contents = Compile(input_file_contents);

		File.CreateText(Out_file_path);
		File.AppendAllText(Out_file_path, output_file_contents);

		////ExecuteCommand(assembler_command + " && " + linker_command);
		ExecuteCommand(Assembler_command);
	}

	static string Compile(in string input_file_contents) {
		Console.ForegroundColor = ConsoleColor.Red;
		List<Token> tokens = Tokenizer.Tokenize(input_file_contents);
		NodeProgram program = Parser.Parse(tokens);
		return Generator.Generate(program);
	}

	static void ExecuteCommand(in string command) {
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

	public static void Error(in string error_message, in int line_number, in int column_number) {
		Console.ForegroundColor = ConsoleColor.Red;
		string output = "Error at line: " + line_number + " column: " + column_number + ", " + error_message;
		if (Exception_on_compile_error) {
			throw new Exception(output);
		} else {
			Console.Error.WriteLine(output);
			Exit(1);
		}
	}

	public static void Error(in string error_message) {
		Console.ForegroundColor = ConsoleColor.Red;
		if (Exception_on_compile_error) {
			throw new Exception(error_message);
		} else {
			Console.Error.WriteLine(error_message);
			Exit(1);
		}
	}
}