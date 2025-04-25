using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using OneOf;

#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Turquoise;

struct NodeTermIntLiteral() {
	public Token Int_literal;
}

struct NodeTermIdentifier() {
	public Token Identifier;
}

unsafe struct NodeBinaryExpressionAddition {
	public NodeExpression* Lhs;
	public NodeExpression* Rhs;
}

unsafe struct NodeBinaryExpressionSubtraction {
	public NodeExpression* Lhs;
	public NodeExpression* Rhs;
}

unsafe struct NodeBinaryExpressionMultiplication {
	public NodeExpression* Lhs;
	public NodeExpression* Rhs;
}

unsafe struct NodeBinaryExpressionDivision {
	public NodeExpression* Lhs;
	public NodeExpression* Rhs;
}

struct NodeBinaryExpression {
	public OneOf<NodeBinaryExpressionAddition, NodeBinaryExpressionSubtraction, NodeBinaryExpressionMultiplication, NodeBinaryExpressionDivision> Binary_expression;
}

struct NodeTerm {
	public OneOf<NodeTermIntLiteral, NodeTermIdentifier, NodeTermParentheses> Term;
}

unsafe struct NodeTermParentheses {
	public NodeExpression* Expression;
}

struct NodeExpression {
	public OneOf<NodeTerm, NodeBinaryExpression> Expression;
}

struct NodeStatementExit {
	public NodeExpression Expression;
}

struct NodeScope {
	public List<NodeStatement> Statements;
}

unsafe struct NodeIfPredicateElseIf {
	public NodeExpression Expression;
	public NodeStatement* Statement;
	public NodeIfPredicate?* Predicate;
}

unsafe struct NodeIfPredicateElse {
	public NodeStatement* Statement;
}

struct NodeIfPredicate {
	public OneOf<NodeIfPredicateElseIf, NodeIfPredicateElse> Predicate;
}

unsafe struct NodeStatementIf {
	public NodeExpression Expression;
	public NodeStatement* Statement;
	public NodeIfPredicate?* Predicate;
}

struct NodeStatementAssign {
	public Token Identifier;
	public NodeExpression Expression;
}

struct NodeStatementEmpty;

struct NodeStatement {
	public OneOf<NodeStatementExit, NodeStatementVar, NodeScope, NodeStatementIf, NodeStatementAssign, NodeStatementEmpty> Statement;
}

struct NodeStatementVar {
	public Token Identifier;
	public NodeExpression Expression;
}

struct NodeProgram() {
	public required List<NodeStatement> Statements;
}

static class Parser {

	public static NodeProgram Parse(List<Token> tokens) {
		int index = 0;
		NodeProgram program = new NodeProgram { Statements = [] };

		while (Peek().HasValue) {
			NodeStatement? statement = ParseStatement();
			if (statement.HasValue) {
				program.Statements.Add(statement.Value);
			}
		}
		return program;

		unsafe NodeTerm? ParseTerm() {
			if (Try_consume_out(TokenType.int_literal, out Token? int_literal)) {
				return new NodeTerm { Term = new NodeTermIntLiteral { Int_literal = int_literal.Value } };
			}
			if (Try_consume_out(TokenType.identifier, out Token? identifier)) {
				return new NodeTerm { Term = new NodeTermIdentifier { Identifier = identifier.Value } };
			}
			if (Try_consume_out(TokenType.open_parentheses, out Token? open_parentheses)) {
				var expression = ParseExpression();
				if (!expression.HasValue) {
					ErrorExpected("expression");
				}

				var term_parentheses = new NodeTermParentheses { Expression = Allocate(expression.Value) };

				Try_consume_error(TokenType.close_parentheses);
				return new NodeTerm { Term = term_parentheses };
			}
			return null;
		}

		unsafe NodeExpression? ParseExpression(in int min_precedence = 0) {
			NodeTerm? term_lhs = ParseTerm();
			if (!term_lhs.HasValue) return null;
			var expression_lhs = new NodeExpression { Expression = term_lhs.Value };

			while (true) {
				Token? current_token = Peek();
				int? precedence;
				if (current_token.HasValue) {
					precedence = current_token.Value.type.OperatorPrecedence();
					if (!precedence.HasValue || precedence < min_precedence) break;
				} else {
					break;
				}

				int next_min_precedence = precedence.Value + 1;
				TokenType type = Consume().type;
				var expression_rhs = ParseExpression(next_min_precedence);

				if (!expression_rhs.HasValue) {
					Error("Unable to parse expression");
				}

				var expression = new NodeBinaryExpression();
				if (type == TokenType.plus) {
					expression.Binary_expression = new NodeBinaryExpressionAddition {
						Lhs = Allocate(expression_lhs),
						Rhs = Allocate(expression_rhs.Value)
					};
				} else if (type == TokenType.asterisk) {
					expression.Binary_expression = new NodeBinaryExpressionMultiplication {
						Lhs = Allocate(expression_lhs),
						Rhs = Allocate(expression_rhs.Value)
					};
				} else if (type == TokenType.minus) {
					expression.Binary_expression = new NodeBinaryExpressionSubtraction {
						Lhs = Allocate(expression_lhs),
						Rhs = Allocate(expression_rhs.Value)
					};
				} else if (type == TokenType.forward_slash) {
					expression.Binary_expression = new NodeBinaryExpressionDivision {
						Lhs = Allocate(expression_lhs),
						Rhs = Allocate(expression_rhs.Value)
					};
				} else {
					Error("parsing of " + type.Name() + " operator not defined");
				}
				expression_lhs.Expression = expression;
			}
			return expression_lhs;
		}

		NodeScope? ParseScope() {
			if (!Try_consume(TokenType.open_brace)) {
				return null;
			}

			var scope = new NodeScope { Statements = [] };
			NodeStatement? statement;
			while ((statement = ParseStatement(in_scope: true)).HasValue) {
				scope.Statements.Add(statement.Value);
			}

			Try_consume_error(TokenType.close_brace);
			return scope;
		}

		unsafe NodeIfPredicate? ParseIfPredicate() {
			if (Try_consume(TokenType.else_)) {
				if (Try_consume(TokenType.if_)) {
					Try_consume_error(TokenType.open_parentheses);
					NodeIfPredicateElseIf else_if_predicate = new NodeIfPredicateElseIf { };
					var expression = ParseExpression();
					if (expression.HasValue) {
						else_if_predicate.Expression = expression.Value;
					} else {
						ErrorExpected("expression");
					}
					Try_consume_error(TokenType.close_parentheses);
					var else_if_statement = ParseStatement();
					if (else_if_statement.HasValue) {
						else_if_predicate.Statement = Allocate(else_if_statement.Value);
					}
					else_if_predicate.Predicate = Allocate(ParseIfPredicate());
					return new NodeIfPredicate { Predicate = else_if_predicate };
				}
				var else_predicate = new NodeIfPredicateElse();
				var else_statement = ParseStatement();
				if (else_statement.HasValue) {
					else_predicate.Statement = Allocate(else_statement.Value);
				}
				return new NodeIfPredicate { Predicate = else_predicate };
			}

			return null;
		}

		NodeStatement? ParseStatement(in bool in_scope = false) {
			if (Try_consume(TokenType.exit)) {
				Try_consume_error(TokenType.open_parentheses);
				NodeStatementExit statement_exit;
				NodeExpression? expression;
				if ((expression = ParseExpression()).HasValue) {
					statement_exit = new NodeStatementExit { Expression = expression.Value };
				} else {
					Error("Unable to parse Expression");
					return null;
				}
				Try_consume_error(TokenType.close_parentheses);
				Try_consume_error(TokenType.semicolon);
				return new NodeStatement { Statement = statement_exit };
			}
			if (Try_consume(TokenType.var)) {

				var statement_identifier = Try_consume_error(TokenType.identifier);

				Try_consume_error(TokenType.equals);

				NodeStatementVar statementVar = new NodeStatementVar { Identifier = statement_identifier };

				NodeExpression? expression = ParseExpression();
				if (expression.HasValue) {
					statementVar.Expression = expression.Value;
				} else {
					Error("Invalid Expression");
				}

				Try_consume_error(TokenType.semicolon);
				return new NodeStatement { Statement = statementVar };
			}
			if (Peek().HasValue && Peek().Value.type == TokenType.identifier && Peek(1).HasValue && Peek(1).Value.type == TokenType.equals) {
				var assign = new NodeStatementAssign { Identifier = Consume() };
				Consume();
				var expression = ParseExpression();
				if (expression.HasValue) {
					assign.Expression = expression.Value;
				} else {
					ErrorExpected("expression");
				}
				Try_consume_error(TokenType.semicolon);
				return new NodeStatement { Statement = assign };
			}
			if (Peek().HasValue && Peek().Value.type == TokenType.open_brace) {
				var scope = ParseScope();
				if (!scope.HasValue) {
					ErrorExpectedToken(TokenType.open_brace);
				}
				return new NodeStatement { Statement = scope.Value };
			}
			if (Try_consume_out(TokenType.if_, out var if_)) unsafe {
					Try_consume_error(TokenType.open_parentheses);
					var expression = ParseExpression();
					if (!expression.HasValue) {
						ErrorExpected("expression");
					}
					var statement_if = new NodeStatementIf { Expression = expression.Value };
					Try_consume_error(TokenType.close_parentheses);
					var statement = ParseStatement();
					if (statement.HasValue) {
						if (statement.Value.Statement.IsT1) Error("Variable declaration not allowed on non scoped if statements");
						statement_if.Statement = Allocate(statement.Value);
					}
					statement_if.Predicate = Allocate(ParseIfPredicate());
					return new NodeStatement { Statement = statement_if };
				}
			if (Peek().HasValue && Peek().Value.type == TokenType.semicolon) {
				Consume();
				return new NodeStatement { Statement = new NodeStatementEmpty() };
			}
			if (Peek().HasValue && Peek().Value.type == TokenType.close_brace && in_scope) {
				return null;
			}
			if (Peek().HasValue) {
				Error("Statement cannot start with " + Peek().Value.type.Name());
				return null;
			} else {
				ErrorExpectedToken(TokenType.close_brace);
				return null;
			}
		}

		[Pure] Token? Peek(in int offset = 0) {
			if (index + offset >= tokens.Count) {
				return null;
			}
			return tokens[index + offset];
		}

		Token Consume() {
			return tokens[index++];
		}

		Token Try_consume_error(in TokenType tokenType) {
			if (!(Peek().HasValue && Peek().Value.type == tokenType)) {
				ErrorExpectedToken(tokenType);
			}
			return Consume();
		}

		bool Try_consume(in TokenType tokenType) {
			if (Peek().HasValue && Peek().Value.type == tokenType) {
				Consume();
				return true;
			}
			return false;
		}

		bool Try_consume_out(in TokenType tokenType, out Token? token) {
			if (Peek().HasValue && Peek().Value.type == tokenType) {
				token = Consume();
				return true;
			}
			token = null;
			return false;
		}

		void Error(in string error_message) {
			var (line_number, column_number, _) = Peek() ?? tokens.Last();
			Program.Error(error_message, line_number, column_number);
		}

		void ErrorExpected(in string expected_string) {
			Error("Expected " + expected_string);
		}

		void ErrorExpectedToken(in TokenType expected_token) {
			Error("Expected " + expected_token.Name());
		}
	}

	private static unsafe T* Allocate<T>(in T value) {
		var ptr = (T*)Marshal.AllocCoTaskMem(sizeof(T));
		*ptr = value;
		return ptr;
	}

	private static unsafe T* Allocate<T>() {
		return (T*)Marshal.AllocCoTaskMem(sizeof(T));
	}
}