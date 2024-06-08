using System.Diagnostics.Contracts;

#pragma warning disable CS8629 // Nullable value type may be null.

namespace Compiler;
using OneOf;

struct NodeExpressionIntLiteral() {
	public Token int_literal;
}
struct NodeExpressionIdentifier() {
	public Token identifier;
}

struct NodeExpression() {
   public OneOf<NodeExpressionIntLiteral, NodeExpressionIdentifier> expression;
}

struct NodeStatementExit {
	public NodeExpression expression;
}

struct NodeStatementVar {
	public Token identifier;
	public NodeExpression expression;
}

struct NodesStatement {
	public OneOf<NodeStatementExit, NodeStatementVar> statement;
}

struct NodeProgram() {
	public List<NodesStatement> statements;
}

static class Parser {
	public static NodeProgram Parse(List<Token> tokens) {
		NodeExpression? Parse_expression() {
			if(peek().HasValue && peek().Value.type == TokenType.int_literal) {
				return new NodeExpression {expression = new NodeExpressionIntLiteral {int_literal = consume()}};
			}
			if (peek().HasValue && peek().Value.type == TokenType.identifier) {
				return new NodeExpression {expression = new NodeExpressionIdentifier {identifier = consume()}};
			}

			return null;
		}
		NodesStatement? Parse_statement() {
			if (peek().HasValue && peek().Value.type == TokenType.exit && peek(1).HasValue && peek(1).Value.type == TokenType.open_parentheses) {
				if (peek().Value.type == TokenType.exit && peek(1).HasValue) {
					if (peek(1).Value.type != TokenType.open_parentheses) {
						throw new Exception("Expected `(`");
					}
					consume();
					consume();
					NodeStatementExit statementExit;
					NodeExpression? expression;
					if((expression = Parse_expression()).HasValue) {
						statementExit = new NodeStatementExit {expression = expression.Value };
					} else {
						throw new Exception("Error: Unable to parse Expression");
					}
					if(peek().HasValue && peek().Value.type == TokenType.close_parentheses) {
						consume();
					} else {
						throw new Exception("Expected `)`");
					}
					if(peek().HasValue && peek().Value.type == TokenType.semi) {
						consume();
					} else {
						throw new Exception("Expected `;`");
					}
					return new NodesStatement {statement = statementExit};
				}
			} else if (
				peek().HasValue && peek().Value.type == TokenType.var &&
				peek(1).HasValue && peek(1).Value.type == TokenType.identifier &&
				peek(2).HasValue && peek(2).Value.type == TokenType.equals) {
				consume();
				NodeStatementVar statementVar = new NodeStatementVar {identifier = consume()};
				consume();
				NodeExpression? expression = Parse_expression();
				if (expression.HasValue) {
					statementVar.expression = expression.Value;
				} else {
					throw new Exception("Error: Invalid Expression");
				}
				if (peek().HasValue && peek().Value.type == TokenType.semi) {
					consume();
				} else {
					throw new Exception("Expected `;`");
				}
				return new NodesStatement {statement = statementVar};
			}
			return null;
		}
		int index = 0;
		[Pure] Token? peek(int offset = 0) {
			if (index + offset >= tokens.Count) {
				return null;
			}
			return tokens[index + offset];
		}
		Token consume() {
			return tokens[index++];
		}

		NodeProgram program =  new NodeProgram{statements = []};
		while (peek().HasValue) {
			NodesStatement? statement = Parse_statement();
			if (statement.HasValue) {
				program.statements.Add(statement.Value);
			} else {
				throw new Exception("Error: Invalid Statement");
			}
		}

		return program;
	}
}
#pragma warning restore CS8629 // Nullable value type may be null.