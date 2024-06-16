#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Turquoise;

using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;
using OneOf;

struct NodeTermIntLiteral() {
	public Token int_literal;
}

struct NodeTermIdentifier() {
	public Token identifier;
}

unsafe struct NodeBinaryExpressionAddition {
	public NodeExpression* lhs;
	public NodeExpression* rhs;
}

unsafe struct NodeBinaryExpressionSubtraction {
	public NodeExpression* lhs;
	public NodeExpression* rhs;
}

unsafe struct NodeBinaryExpressionMultiplication {
	public NodeExpression* lhs;
	public NodeExpression* rhs;
}

unsafe struct NodeBinaryExpressionDivision {
	public NodeExpression* lhs;
	public NodeExpression* rhs;
}

struct NodeBinaryExpression {
	public OneOf<NodeBinaryExpressionAddition, NodeBinaryExpressionSubtraction, NodeBinaryExpressionMultiplication, NodeBinaryExpressionDivision>  binary_expression;
}

struct NodeTerm {
	public OneOf<NodeTermIntLiteral, NodeTermIdentifier, NodeTermParentheses> term;
}

unsafe struct NodeTermParentheses {
	public NodeExpression* expression;
}

unsafe struct NodeExpression {
	public OneOf<NodeTerm, NodeBinaryExpression> expression;
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

		unsafe NodeTerm? ParseTerm() {
			if (try_consume_out(TokenType.int_literal, out Token? int_literal)) {
				return new NodeTerm{term = new NodeTermIntLiteral{int_literal = int_literal.Value}};
			}
			if (try_consume_out(TokenType.identifier, out Token? identifier)) {
				return new NodeTerm{term = new NodeTermIdentifier{identifier = identifier.Value}};
			}
			if (try_consume_out(TokenType.open_parentheses, out Token? open_parentheses)) {
				var expression = ParseExpression();
				if (!expression.HasValue) throw new Exception("Error: Expected expression");

				var term_parentheses = new NodeTermParentheses { expression = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression)) };
				*term_parentheses.expression = expression.Value;

				try_consume_error(TokenType.close_parentheses, "Expected `)`");
                return new NodeTerm {term = term_parentheses};
			}

			return null;
		}

		unsafe NodeExpression? ParseExpression(int min_precedence = 0) {
			NodeTerm? term_lhs = ParseTerm();
			if (!term_lhs.HasValue) return null;
			var expression_lhs = new NodeExpression{expression = term_lhs.Value};

			while (true) {
				Token? current_token = peek();
				int? precedence = null;
				if (current_token.HasValue) {
					precedence = current_token.Value.type.OperatorPrecedence();
					if (!precedence.HasValue || precedence < min_precedence) break;
				} else {
					break;
				}

				Token operator_token = consume();
				int next_min_precedence = precedence.Value + 1;
				var expression_rhs = ParseExpression(next_min_precedence);

				if (!expression_rhs.HasValue) {
					throw new Exception("Error: Unable to parse expression");
				}

				var expression = new NodeBinaryExpression();

				if (operator_token.type == TokenType.plus) {
                    var add = new NodeBinaryExpressionAddition {
                        lhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression)),
                    	rhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression))
                    };
                    *add.lhs = expression_lhs;
                    *add.rhs = expression_rhs.Value;
					expression.binary_expression = add;
				} else if (operator_token.type == TokenType.asterisk) {
                    var mul = new NodeBinaryExpressionMultiplication {
                        lhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression)),
                        rhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression))
                    };
                    *mul.lhs = expression_lhs;
                    *mul.rhs = expression_rhs.Value;
					expression.binary_expression = mul;
				} else if (operator_token.type == TokenType.minus) {
                    var sub = new NodeBinaryExpressionSubtraction {
                        lhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression)),
                        rhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression))
                    };
                    *sub.lhs = expression_lhs;
                    *sub.rhs = expression_rhs.Value;
					expression.binary_expression = sub;
				} else if (operator_token.type == TokenType.forward_slash) {
					var div = new NodeBinaryExpressionDivision {
						lhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression)),
						rhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression))
					};
					*div.lhs = expression_lhs;
					*div.rhs = expression_rhs.Value;
					expression.binary_expression = div;
				} else {
					throw new Exception("Error: parsing of " + operator_token.type + " operator not defined");
				}
				expression_lhs.expression = expression;
			}
			return expression_lhs;
		}

		NodesStatement? ParseStatement() {
			if (try_consume(TokenType.exit)) {
				try_consume_error(TokenType.open_parentheses,  "Error: Expected `(`");
				NodeStatementExit statementExit;
				NodeExpression? expression;
				if((expression = ParseExpression()).HasValue) {
					statementExit = new NodeStatementExit {expression = expression.Value };
				} else {
					throw new Exception("Error: Unable to parse Expression");
				}
				try_consume_error(TokenType.close_parentheses, "Error: Expected `)`");
				try_consume_error(TokenType.semicolon, "Error: Expected `;`");
				return new NodesStatement {statement = statementExit};
			} else if ( try_consume(TokenType.var)) {

				if (!try_consume_out(TokenType.identifier, out var statement_identifier)) {
					throw new Exception("Error: Expected identifier");
				}

				try_consume_error(TokenType.equals, "Error: Expected `=`");

				NodeStatementVar statementVar = new NodeStatementVar {identifier = statement_identifier.Value};

				NodeExpression? expression = ParseExpression();
				if (expression.HasValue) {
					statementVar.expression = expression.Value;
				} else {
					throw new Exception("Error: Invalid Expression");
				}

				try_consume_error(TokenType.semicolon, "Error: Expected `;`");
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

		void try_consume_error(TokenType tokenType, string error_message) {
			if (!(peek().HasValue && peek().Value.type == tokenType)) {
				throw new Exception(error_message);
			}
			consume();
		}

 		bool try_consume(TokenType tokenType) {
			if (peek().HasValue && peek().Value.type == tokenType) {
				consume();
				return true;
			}
			return false;
		}

		bool try_consume_out(TokenType tokenType, out Token? token) {
			if (peek().HasValue && peek().Value.type == tokenType) {
				token = consume();
				return true;
			}
			token = null;
			return false;
		}

		NodeProgram program =  new NodeProgram{statements = []};

		while (peek().HasValue) {
			NodesStatement? statement = ParseStatement();
			if (statement.HasValue) {
				program.statements.Add(statement.Value);
			} else {
				throw new Exception("Error: Invalid Statement");
			}
		}

		return program;
	}
}