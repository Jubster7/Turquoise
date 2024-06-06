namespace Compiler;

class Program {
	static string in_file_path = @"turquoise.tq";
	const string out_file_path = @"out.asm";

    static void Main(string[] args) {
		if (args.Length > 0) {
			in_file_path = args[0];
		}

		if (!File.Exists(in_file_path)) throw new FileNotFoundException();
		string input_file_contents = File.ReadAllText(in_file_path);
		string output_file_contents = Compile(input_file_contents);

		File.CreateText(out_file_path);
		File.AppendAllText(out_file_path, output_file_contents);
	}

    static string Compile(string input_file_contents) {
		List<Token> tokens = Tokenizer.Tokenize(input_file_contents);
		return Generator.Tokens_to_assembly(tokens);
	}
}