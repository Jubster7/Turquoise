using System.Diagnostics.Contracts;
#pragma warning disable CS8629 // Nullable value type may be null.

namespace Compiler;

public enum TokenType {
	exit,
	int_literal,
	semicolon,
	open_parentheses,
	close_parentheses,
	identifier,
	var,
	equals,
	plus,
}

struct Token {
	public TokenType type;
	public string? value;
}


static class Tokenizer {

	public static List<Token> Tokenize(string file_contents) {

		int index = 0;

		[Pure] char? peek(int offset = 0) {
			if (index + offset >= file_contents.Length ) {
				return null;
			}
			return file_contents[index + offset];
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
					tokens.Add(new Token { type = TokenType.exit });
					clear(ref buffer);
				} else if (buffer == "var") {
					tokens.Add(new Token {type = TokenType.var});
					clear(ref buffer);
				} else {
					tokens.Add(new Token {type = TokenType.identifier, value = buffer});
					clear(ref buffer);
				}
			} else if (char.IsDigit(peek().Value)) {
				buffer += consume();
				while (peek().HasValue && char.IsDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(new Token { type = TokenType.int_literal, value = buffer });
				clear(ref buffer);
			} else if (peek().Value == '(') {
				consume();
				tokens.Add(new Token { type = TokenType.open_parentheses });
			} else if (peek().Value == ')') {
				consume();
				tokens.Add(new Token { type = TokenType.close_parentheses});
			} else if ( peek().Value == ';') {
				consume();
				tokens.Add(new Token { type = TokenType.semicolon});
			} else if ( peek().Value == '=') {
				consume();
				tokens.Add(new Token { type = TokenType.equals});
			} else if ( peek().Value == '+') {
				consume();
				tokens.Add(new Token { type = TokenType.plus});
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				throw new Exception("Error: Invalid Character `" + peek() + "`");
			}
		}
		return tokens;
	}
}