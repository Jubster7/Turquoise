namespace Compiler;

static class Generator {
	public static string Tokens_to_assembly(List<Tokenizer.Token> tokens) {
		string output = "global _start\n_start:\n";
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i].type == Tokenizer.TokenType._return) {
				if (i + 1 < tokens.Count && tokens[i + 1].type == Tokenizer.TokenType.int_literal) {
					if (i + 2 < tokens.Count && tokens[i + 2].type == Tokenizer.TokenType.semi) {
						output += "\tmov rax, 60\n";
						output += "\tmov rdi, " + tokens[i + 1].value + "\n";
						output += "\tsyscall\n";
					}
				} else {
					throw new Exception("Expected int literal after expression");
				}
			}
		}
		return output;
	}
}