using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using OneOf;

#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Turquoise;
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

struct NodeExpression {
	public OneOf<NodeTerm, NodeBinaryExpression> expression;
}

struct NodeStatementExit {
	public NodeExpression expression;
}

struct NodeScope {
	public List<NodeStatement> statements;
}


unsafe struct NodeIfPredicateElseIf {
	public NodeExpression expression;
	public NodeStatement* statement;
	public NodeIfPredicate?* predicate;
}

unsafe struct NodeIfPredicateElse {
	public NodeStatement* statement;
}
struct NodeIfPredicate {
	public OneOf<NodeIfPredicateElseIf, NodeIfPredicateElse> predicate;
}

unsafe struct NodeStatementIf {
	public NodeExpression expression;
	public NodeStatement* statement;
	public NodeIfPredicate?* predicate;
}

struct NodeStatementAssign {
	public Token identifier;
	public NodeExpression expression;
}
struct NodeStatementEmpty;

struct NodeStatement {
	public OneOf<NodeStatementExit, NodeStatementVar, NodeScope, NodeStatementIf, NodeStatementAssign, NodeStatementEmpty> statement;
}

struct NodeStatementVar {
	public Token identifier;
	public NodeExpression expression;
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
					ErrorExpected("expression");
				}

				var term_parentheses = new NodeTermParentheses { expression = Allocate(expression.Value) };

				try_consume_error(TokenType.close_parentheses);
                return new NodeTerm {term = term_parentheses};
			}
			return null;
		}

		unsafe NodeExpression? ParseExpression(in int min_precedence = 0) {
			NodeTerm? term_lhs = ParseTerm();
			if (!term_lhs.HasValue) return null;
			var expression_lhs = new NodeExpression{expression = term_lhs.Value};

			while (true) {
				Token? current_token = peek();
                int? precedence;
                if (current_token.HasValue) {
					precedence = current_token.Value.type.OperatorPrecedence();
					if (!precedence.HasValue || precedence < min_precedence) break;
				} else {
					break;
				}

				int next_min_precedence = precedence.Value + 1;
                TokenType type = consume().type;
				var expression_rhs = ParseExpression(next_min_precedence);

				if (!expression_rhs.HasValue) {
					Error("Unable to parse expression");
				}

				var expression = new NodeBinaryExpression();
				if (type == TokenType.plus) {
					expression.binary_expression = new NodeBinaryExpressionAddition {
                        lhs = Allocate(expression_lhs),
                    	rhs = Allocate(expression_rhs.Value)
                    };
				} else if (type == TokenType.asterisk) {
					expression.binary_expression = new NodeBinaryExpressionMultiplication {
                        lhs = Allocate(expression_lhs),
                        rhs = Allocate(expression_rhs.Value)
                    };
				} else if (type == TokenType.minus) {
					expression.binary_expression = new NodeBinaryExpressionSubtraction {
                        lhs = Allocate(expression_lhs),
                        rhs = Allocate(expression_rhs.Value)
                    };
				} else if (type == TokenType.forward_slash) {
					expression.binary_expression = new NodeBinaryExpressionDivision {
                        lhs = Allocate(expression_lhs),
                        rhs = Allocate(expression_rhs.Value)
					};
				} else {
					Error("parsing of " + type.Name() + " operator not defined");
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
			while ((statement = ParseStatement(in_scope: true)).HasValue) {
				scope.statements.Add(statement.Value);
			}

			try_consume_error(TokenType.close_brace);
			return scope;
		}

		unsafe NodeIfPredicate? ParseIfPredicate() {
			if (try_consume(TokenType.else_)) {
				if (try_consume(TokenType.if_)) {
					try_consume_error(TokenType.open_parentheses);
					NodeIfPredicateElseIf else_if_predicate = new NodeIfPredicateElseIf {};
					var expression = ParseExpression();
					if (expression.HasValue) {
						else_if_predicate.expression = expression.Value;
					} else {
						ErrorExpected("expression");
					}
					try_consume_error(TokenType.close_parentheses);
					var else_if_statement = ParseStatement();
					if (else_if_statement.HasValue) {
						else_if_predicate.statement = Allocate(else_if_statement.Value);
					}
					else_if_predicate.predicate = Allocate(ParseIfPredicate());
					return new NodeIfPredicate{predicate = else_if_predicate};
				}
				var else_predicate = new NodeIfPredicateElse();
				var else_statement = ParseStatement();
				if (else_statement.HasValue) {
					else_predicate.statement = Allocate(else_statement.Value);
				}
				return new NodeIfPredicate{predicate = else_predicate};
			}

			return null;
		}

		NodeStatement? ParseStatement(bool in_scope = false) {
			if (try_consume(TokenType.exit)) {
				try_consume_error(TokenType.open_parentheses);
				NodeStatementExit statementExit;
				NodeExpression? expression;
				if((expression = ParseExpression()).HasValue) {
					statementExit = new NodeStatementExit {expression = expression.Value };
				} else {
					Error("Unable to parse Expression");
					return null;
				}
				try_consume_error(TokenType.close_parentheses);
				try_consume_error(TokenType.semicolon);
				return new NodeStatement {statement = statementExit};
			}
			if (try_consume(TokenType.var)) {

				var statement_identifier = try_consume_error(TokenType.identifier);

				try_consume_error(TokenType.equals);

				NodeStatementVar statementVar = new NodeStatementVar {identifier = statement_identifier};

				NodeExpression? expression = ParseExpression();
				if (expression.HasValue) {
					statementVar.expression = expression.Value;
				} else {
					Error("Invalid Expression");
				}

				try_consume_error(TokenType.semicolon);
				return new NodeStatement {statement = statementVar};
			}
			if (peek().HasValue && peek().Value.type == TokenType.identifier && peek(1).HasValue && peek(1).Value.type == TokenType.equals) {
				var assign = new NodeStatementAssign{identifier = consume()};
				consume();
				var expression = ParseExpression();
				if (expression.HasValue) {
					assign.expression = expression.Value;
				} else {
					ErrorExpected("expression");
				}
				try_consume_error(TokenType.semicolon);
				return new NodeStatement {statement = assign};
			}
			if (peek().HasValue && peek().Value.type == TokenType.open_brace) {
				var scope = ParseScope();
				if (!scope.HasValue) {
					ErrorExpectedToken(TokenType.open_brace);
				}
				return new NodeStatement{statement = scope.Value};
			}
			if (try_consume_out(TokenType.if_, out var if_)) unsafe {
				try_consume_error(TokenType.open_parentheses);
				var expression = ParseExpression();
				if (!expression.HasValue) {
					ErrorExpected("expression");
				}
				var statement_if = new NodeStatementIf{expression = expression.Value};
				try_consume_error(TokenType.close_parentheses);
				var statement = ParseStatement();
				if (statement.HasValue) {
					if (statement.Value.statement.IsT1) Error("Variable declaration not allowed on non scoped if statements");
					statement_if.statement = Allocate(statement.Value);
				}
				statement_if.predicate = Allocate(ParseIfPredicate());
                return new NodeStatement {statement = statement_if};
			}
			if (peek().HasValue && peek().Value.type == TokenType.semicolon) {
				consume();
				return new NodeStatement{statement = new NodeStatementEmpty()};
			}
			if (peek().HasValue && peek().Value.type == TokenType.close_brace && in_scope) {
				return null;
			}
			if (peek().HasValue) {
				Error("Statement cannot start with " + peek().Value.type.Name());
				return null;
			} else {
				ErrorExpectedToken(TokenType.close_brace);
				return null;
			}
		}

		int index = 0;
		[Pure] Token? peek(in int offset = 0) {
			if (index + offset >= tokens.Count) {
				return null;
			}
			return tokens[index + offset];
		}

		Token consume() {
			return tokens[index++];
		}

		Token try_consume_error(in TokenType tokenType) {
			if (!(peek().HasValue && peek().Value.type == tokenType)) {
				ErrorExpectedToken(tokenType);
			}
			return consume();
		}

 		bool try_consume(in TokenType tokenType) {
			if (peek().HasValue && peek().Value.type == tokenType) {
				consume();
				return true;
			}
			return false;
		}

		bool try_consume_out(in TokenType tokenType, out Token? token) {
			if (peek().HasValue && peek().Value.type == tokenType) {
				token = consume();
				return true;
			}
			token = null;
			return false;
		}

		void Error(in string error_message) {
			var (line_number, column_number, _) = peek() ?? tokens.Last();
			Program.Error(error_message, line_number, column_number);
		}

		void ErrorExpected(in string expected_string) {
			Error("Expected " + expected_string);
		}

		void ErrorExpectedToken(in TokenType expected_token) {
			Error("Expected " + expected_token.Name());
		}

		NodeProgram program =  new NodeProgram{statements = []};

		while (peek().HasValue) {
			NodeStatement? statement = ParseStatement();
			if (statement.HasValue) {
				program.statements.Add(statement.Value);
			}
		}

		return program;
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