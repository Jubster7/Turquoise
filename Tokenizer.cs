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

	/// <summary>
	///	gets to precedence of the specified token
	/// </summary>
	/// <param name="type"></param>
	/// <returns>The precedence of the operator</returns>
	public static int? OperatorPrecedence(this TokenType type) {
		return type switch {
			TokenType.plus => 0,
			TokenType.minus => 0,
			TokenType.asterisk => 1,
			TokenType.forward_slash => 1,
			_ => null
		};
	}

	/// <summary>
	///	Gets the name of the specified token type
	/// </summary>
	/// <param name="type">The TokenType to get the name of</param>
	/// <returns>A string containing the name of the TokenType</returns>
	/// <exception cref="NotImplementedException"></exception>
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
	/// Gets the corresponding TokenType for the specified character
	/// </summary>
	/// <param name="input">the character to get the token type of</param>
	/// <returns>The type of Token the character represents as a TokenType</returns>
	[Pure] public static TokenType? GetTokenType(this char input) {
		return input switch {
			';' => TokenType.semicolon,
			'(' => TokenType.open_parentheses,
			')' => TokenType.close_parentheses,
			'{' => TokenType.open_brace,
			'}' => TokenType.close_brace,
			'=' => TokenType.equals,
			'+' => TokenType.plus,
			'-' => TokenType.minus,
			'*' => TokenType.asterisk,
			'/' => TokenType.forward_slash,
			_ => null
		};
	}

	/// <summary>
	/// Converts a string into a token with the corresponding type and value
	/// </summary>
	/// <param name="input"></param>
	/// <returns>The corresponding type and value of the string as a Token</returns>
	[Pure] public static Token ToToken(this string input, int line_number, int column_number) {
		return input switch {
			"exit" => new Token { type = TokenType.exit, line_number = line_number, column_number = column_number},
			"var" => new Token { type = TokenType.var, line_number = line_number, column_number = column_number},
			"if" => new Token { type = TokenType.if_, line_number = line_number, column_number = column_number},
			"else" => new Token { type = TokenType.else_, line_number = line_number, column_number = column_number},
			_ => new Token { type = TokenType.identifier, value = input, line_number = line_number, column_number = column_number},
		};
	}

	private static void ErrorExpected(in string expected_string, in int line_number, in int column_number) {
		Program.Error("Expected `" + expected_string + "`", line_number, column_number + 1);
	}

	/// <summary>
	/// Converts a string input in to a <see cref="Token"/>
	/// </summary>
	/// <param name="file_contents">The input string to Tokenize</param>
	/// <returns>A list containing the tokens in file_contents</returns>
	[Pure] public static List<Token> Tokenize(string file_contents) {
		int index = 0;
		int line_count = 1;
		int column_count = 1;
		List<Token> tokens = [];

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

		string consume_multi(in int count = 0) {
			string output = "";
			for (int i = 0; i < count; i++) {
				check_count();
				output += file_contents[index++];
			}
			return output;
		}

		void check_count() {
			if (peek().Value == '\t') {
				column_count += 4;
			} else if (peek().Value == '\n') {
				line_count++;
				column_count = 0;
			} else {
				column_count++;
			}
		}

		void CreateToken(in TokenType type, in string? value = null) {
			tokens.Add(new Token{type = type, line_number = line_count, column_number = column_count, value = value});
		}

		while (peek().HasValue) {
			if (char.IsLetter(peek().Value)) {
				string buffer = "";
				buffer += consume();
				int line_count_start = line_count;
				int column_count_start = column_count;
				while (peek().HasValue && char.IsLetterOrDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(buffer.ToToken(line_count_start, column_count_start));
				continue;
			} else if (char.IsDigit(peek().Value)) {
				string buffer = "";
				buffer += consume();
				int line_count_start = line_count;
				int column_count_start = column_count;
				while (peek().HasValue && char.IsDigit(peek().Value)) {
					buffer += consume();
				}
				tokens.Add(new Token { type = TokenType.int_literal, value = buffer, line_number = line_count_start, column_number = column_count_start});
				continue;
			} else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '/') {
				consume_multi(2);
				while (peek().HasValue && peek().Value != '\n') {
					consume();
				}
				continue;
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
				continue;
			}

			var token_type = peek().Value.GetTokenType();
			if (token_type.HasValue) {
				consume();
				CreateToken(token_type.Value);
			} else if (char.IsWhiteSpace(peek().Value)) {
				consume();
			} else {
				Program.Error("Error: Invalid Character `" + peek() + "`");
			}
		}
		return tokens;
	}
}