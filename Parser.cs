using System.Diagnostics.Contracts;

#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Compiler;

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

/* unsafe struct NodeBinaryExpressionMultiplication {
	public NodeExpression* lhs;
	public NodeExpression* rhs;
} */

struct NodeBinaryExpression {
	public/* OneOf< */NodeBinaryExpressionAddition/* , NodeBinaryExpressionMultiplication> */ binary_expression;
}

struct NodeTerm {
	public OneOf<NodeTermIntLiteral, NodeTermIdentifier> term;
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

		NodeTerm? Parse_term() {
			if(try_consume_out(TokenType.int_literal, out Token? int_literal)) {
				return new NodeTerm{term = new NodeTermIntLiteral{int_literal = int_literal.Value}};
			}
			if(try_consume_out(TokenType.identifier, out Token? identifier)) {
				return new NodeTerm{term = new NodeTermIdentifier{identifier = identifier.Value}};
			}

			return null;
		}

		unsafe NodeExpression? Parse_expression() {
			NodeTerm? term = Parse_term();
			if (term.HasValue) {
				if(try_consume(TokenType.plus)) {
					var binary_expression = new NodeBinaryExpression();
					var binary_expression_addition = new NodeBinaryExpressionAddition();
					var lhs_expression = new NodeExpression {expression = term.Value};
					binary_expression_addition.lhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression));
					*binary_expression_addition.lhs = lhs_expression;

					var rhs = Parse_expression();
					if (rhs.HasValue) {
						var rhs_expression = rhs.Value;
						binary_expression_addition.rhs = (NodeExpression*)Marshal.AllocCoTaskMem(sizeof(NodeExpression));
						*binary_expression_addition.rhs = rhs_expression;
						binary_expression.binary_expression = binary_expression_addition;
						return new NodeExpression {expression = binary_expression};
					} else {
						throw new Exception("Error: Expected expression");
					}
				} else {
					var expression = new NodeExpression {expression = term.Value};
					return expression;
				}
			}
			return null;
		}

		NodesStatement? Parse_statement() {
			if (try_consume(TokenType.exit)) {
				try_consume_error(TokenType.open_parentheses,  "Error: Expected `(`");
				NodeStatementExit statementExit;
				NodeExpression? expression;
				if((expression = Parse_expression()).HasValue) {
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


				NodeExpression? expression = Parse_expression();
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
		Token? peek(int offset = 0) {
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