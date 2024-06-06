namespace Compiler;

static class Generator {
	public static string Tokens_to_assembly(List<Token> tokens) {
		string output = "global _start\n_start:\n";
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i].type == TokenType.exit) {
				if (i + 1 < tokens.Count && tokens[i + 1].type == TokenType.int_literal) {
					if (i + 2 < tokens.Count && tokens[i + 2].type == TokenType.semi) {
						output += "\tmov rax, 60\n";
						output += "\tmov rdi, " + tokens[i + 1].value + "\n";
						output += "\tsyscall\n";
					}
				} else {
					throw new Exception("Expected int literal after exit");
				}
			}
		}
		return output;
	}
}