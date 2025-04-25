using System.Diagnostics.Contracts;

#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
#pragma warning disable IDE0053 // Use expression body for lambda expression

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
	[Pure]
	public static string Generate(in NodeProgram root) {
		int stack_size = 0;
		int label_count = 0;
		const string output_start = $"global {Program.Assembly_entry_point_label}\n{Program.Assembly_entry_point_label}:\n";

		string output = output_start;

		List<Variable> variables = [];
		List<int> scopes = [];

		foreach (NodeStatement nodesStatement in root.Statements) {
			GenerateStatement(nodesStatement);
		}

		output += "\tmov rax, " + (int)SystemCall.exit + "\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";

		return output;

		unsafe void GenerateTerm(in NodeTerm nodeTerm) {
			nodeTerm.Term.Switch(
				(NodeTermIntLiteral nodeTermIntLiteral) => {
					Push(nodeTermIntLiteral.Int_literal.value);
				},
				(nodeTermIdentifier) => {
					var identifier_value = nodeTermIdentifier.Identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count != 1) {
						var (line_number, column_number, _) = nodeTermIdentifier.Identifier;
						Program.Error("Undeclared identifier `" + identifier_value + "`", line_number, column_number);
					}
					Push("QWORD [rsp + " + (stack_size - values[0].stack_location - 1) * 8 + "]");
				},
				(NodeTermParentheses nodeTermParentheses) => {
					GenerateExpression(*nodeTermParentheses.Expression);
				}
			);
		}

		unsafe void GenerateBinaryExpression(in NodeBinaryExpression binaryExpression) {
			binaryExpression.Binary_expression.Switch(
				(NodeBinaryExpressionAddition nodeBinaryExpressionAddition) => {
					GenerateExpression(*nodeBinaryExpressionAddition.Rhs);
					GenerateExpression(*nodeBinaryExpressionAddition.Lhs);
					Pop("rax");
					Pop("rbx");
					output += "\tadd rax, rbx\n";
					Push("rax");
				},
				(NodeBinaryExpressionSubtraction nodeBinaryExpressionSubtraction) => {
					GenerateExpression(*nodeBinaryExpressionSubtraction.Rhs);
					GenerateExpression(*nodeBinaryExpressionSubtraction.Lhs);
					Pop("rax");
					Pop("rbx");
					output += "\tsub rax, rbx\n";
					Push("rax");
				},
				(NodeBinaryExpressionMultiplication nodeBinaryExpressionMultiplication) => {
					GenerateExpression(*nodeBinaryExpressionMultiplication.Rhs);
					GenerateExpression(*nodeBinaryExpressionMultiplication.Lhs);
					Pop("rax");
					Pop("rbx");
					output += "\tmul rbx\n";
					Push("rax");
				},
				(NodeBinaryExpressionDivision nodeBinaryExpressionDivision) => {
					GenerateExpression(*nodeBinaryExpressionDivision.Rhs);
					GenerateExpression(*nodeBinaryExpressionDivision.Lhs);
					Pop("rax");
					Pop("rbx");
					output += "\tmov rdx, 0\n";
					output += "\tdiv rbx\n";
					Push("rax");
				}
			);
		}

		void GenerateExpression(in NodeExpression nodeExpression) {
			nodeExpression.Expression.Switch(
				(NodeTerm nodeTerm) => {
					GenerateTerm(nodeTerm);
				},
				(NodeBinaryExpression nodeBinaryExpression) => {
					GenerateBinaryExpression(nodeBinaryExpression);
				}
			);
		}

		void Push(in string? value) {
			output += "\tpush " + value + "\n";
			stack_size++;
		}

		void Pop(in string register) {
			output += "\tpop " + register + "\n";
			stack_size--;
		}

		void BeginScope() {
			scopes.Add(variables.Count);
		}

		void EndScope() {
			int pop_count = variables.Count - scopes.Last();
			if (pop_count != 0) {
				output += "\tadd rsp, " + pop_count * 8 + "\n";
				stack_size -= pop_count;
				variables.RemoveRange(scopes.Last(), pop_count);
			}
			scopes.RemoveAt(scopes.Count - 1);
		}

		string Create_label() {
			return "label" + label_count++;
		}

		void GenerateScope(in NodeScope nodeScope) {
			BeginScope();
			foreach (NodeStatement statement in nodeScope.Statements) {
				GenerateStatement(statement);
			}
			EndScope();
		}

		unsafe void GenerateIfPredicate(in NodeIfPredicate predicate, string final_label) {
			predicate.Predicate.Switch(
				(NodeIfPredicateElseIf nodeIfPredicateElseIf) => {
					GenerateElseIf(nodeIfPredicateElseIf, final_label);
				},
				(NodeIfPredicateElse nodeIfPredicateElse) => {
					if (nodeIfPredicateElse.Statement != null) {
						GenerateStatement(*nodeIfPredicateElse.Statement);
					}
				}
			);
		}

		void GenerateStatement(in NodeStatement nodesStatement) {
			nodesStatement.Statement.Switch(
				(NodeStatementExit nodeStatementExit) => {
					GenerateExpression(nodeStatementExit.Expression);
					output += "\tmov rax, " + (int)SystemCall.exit + "\n";
					Pop("rdi");
					output += "\tsyscall\n";
				},
				(NodeStatementVar nodeStatementVar) => {
					var identifier_value = nodeStatementVar.Identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count > 0) {
						var (line_number, column_number, _) = nodeStatementVar.Identifier;
						Program.Error("Identifier `" + identifier_value + "` is already declared", line_number, column_number);
					}
					variables.Add(new Variable { name = identifier_value + "", stack_location = stack_size });
					GenerateExpression(nodeStatementVar.Expression);
				},
				(NodeScope nodeScope) => {
					GenerateScope(nodeScope);
				},
				(NodeStatementIf nodeStatementIf) => {
					string final_label = Create_label();
					GenerateIf(nodeStatementIf, final_label);
					output += final_label + ":\n";
				},
				(NodeStatementAssign nodeStatementAssign) => {
					string? identifier_value = nodeStatementAssign.Identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count == 0) {
						var (line_number, column_number, _) = nodeStatementAssign.Identifier;
						Program.Error("Undeclared identifier `" + identifier_value + "`", line_number, column_number);
					}
					GenerateExpression(nodeStatementAssign.Expression);
					Pop("rax");
					output += "\tmov [rsp + " + (stack_size - values[0].stack_location - 1) * 8 + " ], rax\n";
				},
				(NodeStatementEmpty nodeStatementEmpty) => { }
			);
		}

		unsafe void GenerateIf(in NodeStatementIf if_, in string final_label) {
			GenerateExpression(if_.Expression);
			Pop("rax");
			string label = Create_label();
			output += "\ttest rax, rax\n";
			output += "\tjz " + (if_.Predicate->HasValue ? label : final_label) + "\n";
			GenerateStatement(*if_.Statement);
			if (if_.Predicate->HasValue) {
				output += "\tjmp " + final_label + "\n";
				output += label + ":\n";
				GenerateIfPredicate(if_.Predicate->Value, final_label);
			}
		}

		unsafe void GenerateElseIf(NodeIfPredicateElseIf if_, in string final_label) {
			GenerateIf(*(NodeStatementIf*)&if_, final_label);
		}
	}
}