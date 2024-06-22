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
	public int line_number;
	public int column_number;

	public readonly void Deconstruct(out TokenType type , out string? value) {
		type = this.type;
		value = this.value;
	}

	public readonly void Deconstruct(out int line_number , out int column_number, out TokenType type) {
		line_number = this.line_number;
		column_number = this.column_number;
		type = this.type;
	}
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

	public static string Name(this TokenType type) {
		return type switch {
            TokenType.exit => "`exit`",
            TokenType.int_literal => "integer literal",
            TokenType.semicolon => "`;`",
            TokenType.open_parentheses => "`(`",
            TokenType.close_parentheses => "`)`",
            TokenType.open_brace => "`{`",
            TokenType.close_brace => "`}`",
            TokenType.identifier => "identifier",
            TokenType.var => "`var`",
            TokenType.equals => "`=`",
            TokenType.plus => "`+`",
            TokenType.minus => "`-`",
            TokenType.asterisk => "`*`",
            TokenType.forward_slash => "`/`",
            TokenType.if_ => "`if`",
            TokenType.else_ => "`else`",
			_ => throw new NotImplementedException("Cannot convert " + type + " to string")
        };
	}

	/// <summary>
	/// Converts a string input in to a <see cref="Token"/>
	/// </summary>
	/// <param name="file_contents">The input string to Tokenize</param>
	/// <returns></returns>
	[Pure] public static List<Token> Tokenize(string file_contents) {
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
			check_count();
			return file_contents[index++];
		}

		string consume_multi(int count = 0) {
			check_count();
			var output = "";
			for (int i = 0; i < count; i++) {
				output += file_contents[index++];
			}
			return output;
		}

		void check_count() {
			if (peek().Value == '\t') {
				column_count += 4;
			} else if (peek().Value == '\n') {
				line_count ++;
				column_count = 0;
			} else {
				column_count++;
			}
		}

		void ErrorExpected(in string expected_string, int line_number, int column_number) {
			Program.Error("Expected `" + expected_string + "`", line_number , column_number + 1);
		}


		List<Token> tokens = [];
		while (peek().HasValue) {
			if (char.IsLetter(peek().Value)) {
				string buffer = string.Empty;
				buffer += consume();
				int line_count_start = line_count;
				int column_count_start = column_count;

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

				tokens.Add(new Token{type = token_type, value = token_value, line_number = line_count_start, column_number = column_count_start});
			} else if (char.IsDigit(peek().Value)) {
				string buffer = string.Empty;
				buffer += consume();
				int line_count_start = line_count;
				int column_count_start = column_count;
				while (peek().HasValue && char.IsDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(new Token { type = TokenType.int_literal, value = buffer, line_number = line_count_start, column_number = column_count_start});
			} else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '/') {
				consume_multi(2);
				while (peek().HasValue && peek().Value != '\n') {
					consume();
				}
			} else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '*') {
				consume_multi(2);
				while (peek().HasValue && peek(1).HasValue && (peek(1).Value != '/' || peek().Value != '*')) {
					consume();
				}
				if (peek().HasValue && peek(1).HasValue) {
					consume_multi(2);
				} else {
					ErrorExpected("*/", line_count, column_count);
				}
			} else if (peek().Value == '(') {
				consume();
				tokens.Add(new Token { type = TokenType.open_parentheses, line_number = line_count, column_number = column_count});
			} else if (peek().Value == '{') {
				consume();
				tokens.Add(new Token { type = TokenType.open_brace, line_number = line_count, column_number = column_count});
			} else if (peek().Value == '}') {
				consume();
				tokens.Add(new Token { type = TokenType.close_brace, line_number = line_count, column_number = column_count});
			} else if (peek().Value == ')') {
				consume();
				tokens.Add(new Token { type = TokenType.close_parentheses, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == ';') {
				consume();
				tokens.Add(new Token { type = TokenType.semicolon, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == '=') {
				consume();
				tokens.Add(new Token { type = TokenType.equals, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == '+') {
				consume();
				tokens.Add(new Token { type = TokenType.plus, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == '-') {
				consume();
				tokens.Add(new Token { type = TokenType.minus, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == '*') {
				consume();
				tokens.Add(new Token { type = TokenType.asterisk, line_number = line_count, column_number = column_count});
			} else if ( peek().Value == '/') {
				consume();
				tokens.Add(new Token { type = TokenType.forward_slash, line_number = line_count, column_number = column_count});
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				Program.Error("Error: Invalid Character `" + peek() + "`");
			}
		}
		return tokens;
	}
}