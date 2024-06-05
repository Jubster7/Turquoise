namespace Compiler;

static class Tokenizer {
	public enum TokenType {
		_return,
		int_literal,
		semi,
	}

	public struct Token(TokenType _type, string? _value = null) {
		public TokenType type = _type;
		public string? value = _value;
    }

    public static List<Token> Tokenize(string input) {
		List<Token> tokens = [];
		string buffer = "";
		for (int i = 0; i < input.Length; i++) {
			char c = input[i];
			if (char.IsAsciiLetter(c)) {
				buffer += c;
				i++;
				while (char.IsLetterOrDigit(input[i])) {
					buffer += input[i];
					i++;
				}
				i--;
				if (buffer == "return") {
					tokens.Add(new Token(TokenType._return));
					buffer = "";
				} else {
					throw new Exception("Error: Undeclared Idntifier: `" + buffer + "`");
				}
			} else if (char.IsDigit(c)) {
				buffer += c;
				i++;
				while (char.IsDigit(input[i])) {
					buffer += input[i];
					i++;
				}
				i--;
				tokens.Add(new Token(TokenType.int_literal, buffer));
				buffer = "";
			} else if (c == ';') {
				tokens.Add(new Token(TokenType.semi));
			} else if (char.IsWhiteSpace(c)) {
				continue;
			} else {
				throw new Exception("Error: invalid character: `"+ c + "`");
			}
		}
		return tokens;
	}
}
