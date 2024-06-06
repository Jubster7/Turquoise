namespace Compiler;
using System.Diagnostics.Contracts;
struct NodeExit {
	NodeExpression expression;
}
struct NodeExpression {
	Token int_literal;
}

static class Parser {
	static NodeExpression? Parse_expression() {
		return null;
	}

	#pragma warning disable CS8629 // Nullable value type may be null.

	static NodeExit? Parse(List<Token> tokens) {
		int index = 0;

		[Pure] Token? peek(int offset = 0) {
			if (index > tokens.Count) {
                return null;
			}
			return tokens[index];
		}

		Token consume() {
			return tokens[index++];
		}

		NodeExit? exit_node = null;
		while(peek().HasValue) {
            if (peek().Value.type == TokenType.exit) {
				NodeExpression? expression;
				if((expression = Parse_expression()).HasValue) {

				} else {
					throw new Exception("Unable to parse Expression");
				}
			}
        }
		return exit_node;
	}
	#pragma warning restore CS8629 // Nullable value type may be null.
}