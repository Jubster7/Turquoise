#pragma warning disable CS8629 // Nullable value type may be null.
namespace Compiler;

using System.Diagnostics.Contracts;

struct NodeExit {
	public NodeExit(NodeExpression _expression) {
		expression = _expression;
	}
	public NodeExpression expression;
}
struct NodeExpression {
	public NodeExpression (Token _int_literal) {
		int_literal = _int_literal;
	}
	public Token int_literal;
}

static class Parser {

	public static NodeExit? Parse(List<Token> tokens) {
		int index = 0;

		NodeExpression? Parse_expression() {
			if(peek().HasValue && peek().Value.type == TokenType.int_literal) {
				return new NodeExpression(consume());
			}
			return null;
		}

		[Pure] Token? peek(int offset = 0) {
			if (index + offset >= tokens.Count) {
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
				consume();
				NodeExpression? expression;
				if((expression = Parse_expression()).HasValue) {
					exit_node = new NodeExit(expression.Value);
				} else {
					throw new Exception("Unable to parse Expression");
				}
				if(peek().HasValue && peek().Value.type == TokenType.semi) {
					consume();
				} else {
					throw new Exception("Expected `;`");
				}
			}
        }
		return exit_node;
	}
}
#pragma warning restore CS8629 // Nullable value type may be null.