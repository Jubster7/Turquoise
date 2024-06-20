using System.Diagnostics.Contracts;

#pragma warning disable CS8629 // Nullable value type may be null.

namespace Turquoise;
public enum TokenType {
	exit,
	int_literal,
	semicolon,
	open_parentheses,
	close_parentheses,
	open_brace,
	close_brace,
	identifier,
	var,
	equals,
	plus,
	minus,
	asterisk,
	forward_slash,
	if_,
	else_,
}

struct Token {
	public TokenType type;
	public string? value;
	public readonly void Deconstruct(out TokenType type , out string? value) {
		type = this.type;
		value = this.value;
	}
	public int line_count;
	public int column_count;
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

	public static List<Token> Tokenize(in string file_contents) {
		return Tokenize(file_contents, out _, out _);
	}

	public static List<Token> Tokenize(string file_contents, out int total_line_count, out int total_column_count) {

		int index = 0;
		int line_count = 1;
		int column_count = 1;

		[Pure] char? peek(int offset = 0) {
			if (index + offset >= file_contents.Length ) {
				return null;
			}
			return file_contents[index + offset];
		}

		char consume() {
			if (peek().Value == '\t') {
				column_count += 4;
			} else if (peek().Value == '\n') {
				line_count ++;
				column_count = 0;
			} else {
				column_count++;
			}


			return file_contents[index++];
		}

		List<Token> tokens = [];
		while (peek().HasValue) {
			if (char.IsLetter(peek().Value)) {
				string buffer = string.Empty;
				buffer += consume();
				while (peek().HasValue && char.IsLetterOrDigit(peek().Value)) {
					buffer += consume();
				}

				var (token_type, token_value) = buffer switch {
					"exit" => (TokenType.exit, null),
					"var" => (TokenType.var, null),
					"if" => (TokenType.if_, null),
					"else" => (TokenType.else_, null),
					_ => (TokenType.identifier, buffer),
				};

				tokens.Add(new Token{type = token_type, value = token_value, line_count = line_count, column_count = column_count});
			} else if (char.IsDigit(peek().Value)) {
				string buffer = string.Empty;
				buffer += consume();
				while (peek().HasValue && char.IsDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(new Token { type = TokenType.int_literal, value = buffer, line_count = line_count, column_count = column_count});
			} else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '/') {
				consume();
				consume();
				while (peek().HasValue && peek().Value != '\n') {
					consume();
				}
			} else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '*') {
				consume();
				consume();
				while (peek().HasValue && peek(1).HasValue && (peek(1).Value != '/' || peek().Value != '*')) {
					consume();
				}
				if (peek().HasValue && peek(1).HasValue) {
					consume();
					consume();
				} else {
					Program.ErrorExpected("*/", new Token {line_count = line_count, column_count = column_count + 1});
				}
			} else if (peek().Value == '(') {
				consume();
				tokens.Add(new Token { type = TokenType.open_parentheses, line_count = line_count, column_count = column_count});
			} else if (peek().Value == '{') {
				consume();
				tokens.Add(new Token { type = TokenType.open_brace, line_count = line_count, column_count = column_count});
			} else if (peek().Value == '}') {
				consume();
				tokens.Add(new Token { type = TokenType.close_brace, line_count = line_count, column_count = column_count});
			} else if (peek().Value == ')') {
				consume();
				tokens.Add(new Token { type = TokenType.close_parentheses, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == ';') {
				consume();
				tokens.Add(new Token { type = TokenType.semicolon, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == '=') {
				consume();
				tokens.Add(new Token { type = TokenType.equals, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == '+') {
				consume();
				tokens.Add(new Token { type = TokenType.plus, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == '-') {
				consume();
				tokens.Add(new Token { type = TokenType.minus, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == '*') {
				consume();
				tokens.Add(new Token { type = TokenType.asterisk, line_count = line_count, column_count = column_count});
			} else if ( peek().Value == '/') {
				consume();
				tokens.Add(new Token { type = TokenType.forward_slash, line_count = line_count, column_count = column_count});
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				Program.Error("Error: Invalid Character `" + peek() + "`");
			}
		}
		total_line_count = line_count;
		total_column_count = column_count + 1;
		return tokens;
	}
}