using System.Diagnostics;
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
	static readonly bool throw_on_compile_error = false;

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
	/// <summary>
	///	Compiles the string input into a assembly program
	/// </summary>
	/// <param name="input_file_contents">The input file as a string to compile</param>
	/// <returns>A string containing assembly program generated from the input</returns>
	static string Compile(in string input_file_contents) {
		Console.ForegroundColor = ConsoleColor.Red;
		List<Token> tokens = Tokenizer.Tokenize(input_file_contents);
		NodeProgram program = Parser.Parse(tokens);
		return Generator.Generate(program);
	}
	/// <summary>
	/// Executes the specified command in the command line
	/// </summary>
	/// <param name="command">The command to execute</param>
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
	/// <summary>
	/// Exits the program with the specified error message including line and column numbers
	/// </summary>
	/// <param name="error_message">The error message to be printed</param>
	/// <param name="line_number">The line number where the error occurred</param>
	/// <param name="column_number">The column number where the error occurred</param>
	public static void Error(in string error_message, in int line_number, in int column_number) {
		Console.ForegroundColor = ConsoleColor.Red;
		string output = "Error at line: " + line_number + " column: " + column_number  + ", " + error_message;
		if (throw_on_compile_error) {
			throw new Exception(output);
		} else {
			Console.Error.WriteLine(output);
			Exit(1);
		}
	}
	/// <summary>
	/// Exits the program with the specified error message
	/// </summary>
	/// <param name="error_message">The error message to be printed</param>
	public static void Error(in string error_message) {
		Console.ForegroundColor = ConsoleColor.Red;
		if (throw_on_compile_error) {
			throw new Exception(error_message);
		} else {
			Console.Error.WriteLine(error_message);
			Exit(1);
		}
	}
}