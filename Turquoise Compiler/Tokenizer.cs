using System.Diagnostics;

namespace Compiler;

public enum TokenType {
	exit,
	int_literal,
	semi,
}

public struct Token {
	public TokenType type;
	public string? value;

	public Token (TokenType _type, string? _value = null) {
		type = _type;
		value = _value;
	}
}


#pragma warning disable CS8629 // Nullable value type may be null.
static class Tokenizer {

    public static List<Token> Tokenize(string file_contents) {

		int index = 0;

		[System.Diagnostics.Contracts.Pure] char? peek(int offset = 1) {
			if (index + offset > file_contents.Length ) {
				return null;
			}
			return file_contents[index];
		}
		char consume() {
			return file_contents[index++];
		}
		string buffer = string.Empty;
		static void clear(ref string input) {
			input = string.Empty;
		}


		List<Token> tokens = [];
		while (peek().HasValue) {
            if (char.IsLetter(peek().Value)) {
				buffer += consume();
                while (peek().HasValue && char.IsLetterOrDigit(peek().Value)) {
					buffer += consume();
				}
                if (buffer == "exit") {
					tokens.Add(new Token(TokenType.exit));
					clear(ref buffer);
				} else {
					throw new Exception("Error: Undeclared Identifier: `" + buffer + "`");
				}
			} else if (char.IsDigit(peek().Value)) {
                buffer += consume();
				while (peek().HasValue && char.IsDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(new Token(TokenType.int_literal, buffer));
				clear(ref buffer);
			} else if ( peek().Value == ';') {
				consume();
				tokens.Add(new Token(TokenType.semi));
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				throw new Exception("Error: Invalid Character: `" + peek() + "`");
			}
		}
        return tokens;
	}
}
#pragma warning restore CS8629 // Nullable value type may be null.