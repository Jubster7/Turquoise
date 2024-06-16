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

struct NodeScope {
	public List<NodeStatement> statements;
}

unsafe struct NodeStatementIf {
	public NodeExpression expression;
	public NodeStatement* statement;
}

struct NodeStatementVar {
	public Token identifier;
	public NodeExpression expression;
}

struct NodeStatement {
	public OneOf<NodeStatementExit, NodeStatementVar, NodeScope, NodeStatementIf> statement;
}

struct NodeProgram() {
	public List<NodeStatement> statements;
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
				if (!expression.HasValue) {
					Console.Error.WriteLine("Error: Expected expression");
					Environment.Exit(0);
				}

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
					Console.Error.WriteLine("Error: Unable to parse expression");
					Environment.Exit(0);
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
					Console.Error.WriteLine("Error: parsing of " + operator_token.type + " operator not defined");
					Environment.Exit(0);
				}
				expression_lhs.expression = expression;
			}
			return expression_lhs;
		}

		NodeScope? ParseScope() {
			if (!try_consume(TokenType.open_brace)) {
				return null;
			}

			var scope = new NodeScope{statements = []};
			NodeStatement? statement;
			while ((statement = ParseStatement()).HasValue) {
				scope.statements.Add(statement.Value);
			}

			try_consume_error(TokenType.close_brace, "Error: Expected `}`");
			return scope;
		}

		NodeStatement? ParseStatement() {
			if (try_consume(TokenType.exit)) {
				try_consume_error(TokenType.open_parentheses,  "Error: Expected `(`");
				NodeStatementExit statementExit;
				NodeExpression? expression;
				if((expression = ParseExpression()).HasValue) {
					statementExit = new NodeStatementExit {expression = expression.Value };
				} else {
					Console.Error.WriteLine("Error: Unable to parse Expression");
					Environment.Exit(0);
					return null;
				}
				try_consume_error(TokenType.close_parentheses, "Error: Expected `)`");
				try_consume_error(TokenType.semicolon, "Error: Expected `;`");
				return new NodeStatement {statement = statementExit};
			} else if ( try_consume(TokenType.var)) {

				if (!try_consume_out(TokenType.identifier, out var statement_identifier)) {
					Console.Error.WriteLine("Error: Expected identifier");
					Environment.Exit(0);
				}

				try_consume_error(TokenType.equals, "Error: Expected `=`");

				NodeStatementVar statementVar = new NodeStatementVar {identifier = statement_identifier.Value};

				NodeExpression? expression = ParseExpression();
				if (expression.HasValue) {
					statementVar.expression = expression.Value;
				} else {
					Console.Error.WriteLine("Error: Invalid Expression");
					Environment.Exit(0);
				}

				try_consume_error(TokenType.semicolon, "Error: Expected `;`");
				return new NodeStatement {statement = statementVar};
			} else if (peek().HasValue && peek().Value.type == TokenType.open_brace) {
				var scope = ParseScope();
				if (!scope.HasValue) {
					Console.Error.WriteLine("Error: Expected `{`");
					Environment.Exit(0);
				}
				return new NodeStatement{statement = scope.Value};
			} else if (try_consume_out(TokenType.if_, out var if_)) unsafe {
				try_consume_error(TokenType.open_parentheses, "Error: Expected `(`");
				var expression = ParseExpression();
				if (!expression.HasValue) {
					Console.Error.WriteLine("Error: Invalid Expression");
					Environment.Exit(0);
				}
				var statement_if = new NodeStatementIf{expression = expression.Value};
				try_consume_error(TokenType.close_parentheses, "Error: Expected `)`");
				var statement = ParseStatement();
				if (statement.HasValue) {
					statement_if.statement = (NodeStatement*)Marshal.AllocCoTaskMem(sizeof(NodeStatement));
					*statement_if.statement = statement.Value;
				} else {
					Console.Error.WriteLine("Error: Expected `{`");
					Environment.Exit(0);
				}
                return new NodeStatement {statement = statement_if};
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
				Console.Error.WriteLine(error_message);
				Environment.Exit(0);
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
			NodeStatement? statement = ParseStatement();
			if (statement.HasValue) {
				program.statements.Add(statement.Value);
			} else {
				Console.Error.WriteLine("Error: Invalid Statement");
				Environment.Exit(0);
			}
		}

		return program;
	}
}