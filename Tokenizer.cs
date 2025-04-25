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

	public readonly void Deconstruct(out TokenType type, out string? value) {
		type = this.type;
		value = this.value;
	}

	public readonly void Deconstruct(out int line_number, out int column_number, out TokenType type) {
		line_number = this.line_number;
		column_number = this.column_number;
		type = this.type;
	}
}

static class Tokenizer {

	[Pure]
	public static List<Token> Tokenize(string file_contents) {
		int index = 0;
		int line_count = 1;
		int column_count = 1;
		List<Token> tokens = [];

		while (Peek().HasValue) {
			if (char.IsLetter(Peek().Value)) {
				string buffer = "";
				buffer += Consume();
				int line_count_start = line_count;
				int column_count_start = column_count;
				while (Peek().HasValue && char.IsLetterOrDigit(Peek().Value)) {
					buffer += Consume();
				}
				tokens.Add(buffer.ToToken(line_count_start, column_count_start));
				continue;
			} else if (char.IsDigit(Peek().Value)) {
				string buffer = "";
				buffer += Consume();
				int line_count_start = line_count;
				int column_count_start = column_count;
				while (Peek().HasValue && char.IsDigit(Peek().Value)) {
					buffer += Consume();
				}
				tokens.Add(new Token { type = TokenType.int_literal, value = buffer, line_number = line_count_start, column_number = column_count_start });
				continue;
			} else if (Peek().Value == '/' && Peek(1).HasValue && Peek(1).Value == '/') {
				ConsumeMulti(2);
				while (Peek().HasValue && Peek().Value != '\n') {
					Consume();
				}
				continue;
			} else if (Peek().Value == '/' && Peek(1).HasValue && Peek(1).Value == '*') {
				ConsumeMulti(2);
				while (Peek().HasValue && Peek(1).HasValue && (Peek(1).Value != '/' || Peek().Value != '*')) {
					Consume();
				}
				if (Peek().HasValue && Peek(1).HasValue) {
					ConsumeMulti(2);
				} else {
					ErrorExpected("*/", line_count, column_count);
				}
				continue;
			}

			var token_type = Peek().Value.GetTokenType();
			if (token_type.HasValue) {
				Consume();
				CreateToken(token_type.Value);
			} else if (char.IsWhiteSpace(Peek().Value)) {
				Consume();
			} else {
				Program.Error("Error: Invalid Character `" + Peek() + "`");
			}
		}
		return tokens;

		[Pure] char? Peek(int offset = 0) {
			return index + offset >= file_contents.Length ? null : file_contents[index + offset];
		}

		char Consume() {
			CheckCount();
			return file_contents[index++];
		}

		string ConsumeMulti(in int count = 0) {
			string output = "";
			for (int i = 0; i < count; i++) {
				CheckCount();
				output += file_contents[index++];
			}
			return output;
		}

		void CheckCount() {
			if (Peek().Value == '\t') {
				column_count += 4;
			} else if (Peek().Value == '\n') {
				line_count++;
				column_count = 0;
			} else {
				column_count++;
			}
		}

		void CreateToken(in TokenType type, in string? value = null) {
			tokens.Add(new Token { type = type, line_number = line_count, column_number = column_count, value = value });
		}
	}

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

	[Pure]
	public static TokenType? GetTokenType(this char input) {
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

	[Pure]
	public static Token ToToken(this string input, int line_number, int column_number) {
		return input switch {
			"exit" => new Token { type = TokenType.exit, line_number = line_number, column_number = column_number },
			"var" => new Token { type = TokenType.var, line_number = line_number, column_number = column_number },
			"if" => new Token { type = TokenType.if_, line_number = line_number, column_number = column_number },
			"else" => new Token { type = TokenType.else_, line_number = line_number, column_number = column_number },
			_ => new Token { type = TokenType.identifier, value = input, line_number = line_number, column_number = column_number },
		};
	}

	private static void ErrorExpected(in string expected_string, in int line_number, in int column_number) {
		Program.Error("Error: Expected `" + expected_string + "`", line_number, column_number + 1);
	}
}