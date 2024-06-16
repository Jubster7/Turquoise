using System.Diagnostics.Contracts;
#pragma warning disable CS8629 // Nullable value type may be null.

namespace Turquoise;

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
	minus,
	asterisk,
	forward_slash,
}

struct Token {
	public TokenType type;
	public string? value;
}


static class Tokenizer {

	public static int? OperatorPrecedence(this TokenType type) {
		return type switch {
			TokenType.plus => 0,
			TokenType.minus => 0,
			TokenType.asterisk => 1,
			TokenType.forward_slash => 1,
			_ => null
		};
	}

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
			} else if ( peek().Value == '-') {
				consume();
				tokens.Add(new Token { type = TokenType.minus});
			} else if ( peek().Value == '*') {
				consume();
				tokens.Add(new Token { type = TokenType.asterisk});
			} else if ( peek().Value == '/') {
				consume();
				tokens.Add(new Token { type = TokenType.forward_slash});
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				throw new Exception("Error: Invalid Character `" + peek() + "`");
			}
		}
		return tokens;
    }
}