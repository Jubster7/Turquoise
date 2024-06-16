using System.Collections.Specialized;
using System.Diagnostics.Contracts;
namespace Turquoise;

enum SystemCall {
	exit = 0x2000001,
	//read = 0x20000003,
	//write = 0x20000004,
	//sleep = 0x20000240,
}

struct Variable {
	public string name;
	public int stack_location;
}

static class Generator {

	[Pure] public static string Generate(NodeProgram root) {
		int stack_size = 0;
		int label_count = 0;
		string entry_point_name = Program.assembly_entry_point_label;
		string output = $"global {entry_point_name}\n{entry_point_name}:\n";
		List<Variable> variables = [];
		List<int> scopes = [];

		unsafe void GenerateTerm(NodeTerm nodeTerm) {
			nodeTerm.term.Switch(
				NodeTermIntLiteral => {
					push(NodeTermIntLiteral.int_literal.value);
				},
				NodeTermIdentifier => {
					var identifier_value = NodeTermIdentifier.identifier.value;
					Variable[] values = variables.Where(variable => variable.name == identifier_value).ToArray();
					if (values.Length != 1) {
						Console.Error.WriteLine("Error: Undeclared identifier `" + identifier_value + "`");
						Environment.Exit(0);
					}
					push("QWORD [rsp + " + (stack_size - values[0].stack_location - 1) * 8 + "]");
				},
				NodeTermParentheses => {
					GenerateExpression(*NodeTermParentheses.expression);
				}
			);
		}

		unsafe void GenerateBinaryExpression(NodeBinaryExpression binaryExpression) {
			binaryExpression.binary_expression.Switch(
				NodeBinaryExpressionAddition => {
					GenerateExpression(*NodeBinaryExpressionAddition.rhs);
					GenerateExpression(*NodeBinaryExpressionAddition.lhs);
					pop("rax");
					pop("rbx");
					output += "\tadd rax, rbx\n";
					push("rax");
				},
				NodeBinaryExpressionSubtraction => {
					GenerateExpression(*NodeBinaryExpressionSubtraction.rhs);
					GenerateExpression(*NodeBinaryExpressionSubtraction.lhs);
					pop("rax");
					pop("rbx");
					output += "\tsub rax, rbx\n";
					push("rax");
				},
				NodeBinaryExpressionMultiplication => {
					GenerateExpression(*NodeBinaryExpressionMultiplication.rhs);
					GenerateExpression(*NodeBinaryExpressionMultiplication.lhs);
					pop("rax");
					pop("rbx");
					output += "\tmul rbx\n";
					push("rax");
				},
				NodeBinaryExpressionDivision => {
					GenerateExpression(*NodeBinaryExpressionDivision.rhs);
					GenerateExpression(*NodeBinaryExpressionDivision.lhs);
					pop("rax");
					pop("rbx");
					output += "\tmov rdx, 0\n";
					output += "\tdiv rbx\n";
					push("rax");
				}
			);
		}

		unsafe void GenerateExpression(NodeExpression nodeExpression) {
			nodeExpression.expression.Switch(
				NodeTerm => {
					GenerateTerm(NodeTerm);
				},
				NodeBinaryExpression => {
					GenerateBinaryExpression(NodeBinaryExpression);
				}
			);
		}

		void push(string? value) {
			output += "\tpush " + value +"\n";
			stack_size++;
		}

		void pop(string register) {
			output += "\tpop " + register +"\n";
			stack_size--;
		}

		void BeginScope() {
			scopes.Add(variables.Count);
		}

		void EndScope() {
			int pop_count = variables.Count - scopes.Last();
			output += "\tadd rsp, " + pop_count * 8 + "\n";
			stack_size -= pop_count;
			variables.RemoveRange(scopes.Last(), pop_count);
			scopes.RemoveAt(scopes.Count - 1);
		}

		string create_label() {
			return "label" + label_count++;
		}

		void GenerateScope(NodeScope nodeScope) {
			BeginScope();
			foreach (NodeStatement statement in nodeScope.statements) {
				GenerateStatement(statement);
			}
			EndScope();
		}

		unsafe void GenerateStatement(in NodeStatement nodesStatement) {
			nodesStatement.statement.Switch(
				NodeStatementExit => {
					GenerateExpression(NodeStatementExit.expression);
					output += "\tmov rax, " + (int)SystemCall.exit + "\n";
					pop("rdi");
					output += "\tsyscall\n";
				},
				NodeStatementVar => {
					var identifier_value = NodeStatementVar.identifier.value;
					Variable[] values = variables.Where(variable => variable.name == identifier_value).ToArray();

					if (values.Length > 0) {
						Console.Error.WriteLine("Error: Identifier `" + identifier_value + "` is already declared");
						Environment.Exit(0);
					}
					variables.Add(new Variable { name = identifier_value + "", stack_location = stack_size });
					GenerateExpression(NodeStatementVar.expression);
				},
				NodeScope => {
					GenerateScope(NodeScope);
				},
				NodeStatementIf => {
					GenerateExpression(NodeStatementIf.expression);
					pop("rax");
					string label = create_label();
					output += "\ttest rax, rax\n";
					output += "\tjz " + label + "\n";
					GenerateStatement(*NodeStatementIf.statement);
					output += label + ":\n";
				}
			);
		}

		foreach (NodeStatement nodesStatement in root.statements) {
			GenerateStatement(nodesStatement);
		}

		output += "\tmov rax, " + (int)SystemCall.exit + "\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";

		return output.Trim();
	}
}