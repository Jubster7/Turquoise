using System.Diagnostics.Contracts;

#pragma warning disable CS8629 // Nullable value type may be null.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

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
	[Pure] public static string Generate(in NodeProgram root) {
		int stack_size = 0;
		int label_count = 0;
		string entry_point_name = Program.assembly_entry_point_label;
		string output = $"global {entry_point_name}\n{entry_point_name}:\n";
		List<Variable> variables = [];
		List<int> scopes = [];

		unsafe void GenerateTerm(in NodeTerm nodeTerm) {
			nodeTerm.term.Switch(
				NodeTermIntLiteral => {
					push(NodeTermIntLiteral.int_literal.value);
				},
				NodeTermIdentifier => {
					var identifier_value = NodeTermIdentifier.identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count != 1) {
						var (line_number, column_number, _) = NodeTermIdentifier.identifier;
						Program.Error("Undeclared identifier `" + identifier_value + "`", line_number, column_number);
					}
					push("QWORD [rsp + " + (stack_size - values[0].stack_location - 1) * 8 + "]");
				},
				NodeTermParentheses => {
					GenerateExpression(*NodeTermParentheses.expression);
				}
			);
		}

		unsafe void GenerateBinaryExpression(in NodeBinaryExpression binaryExpression) {
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

		void GenerateExpression(in NodeExpression nodeExpression) {
			nodeExpression.expression.Switch(
				NodeTerm => {
					GenerateTerm(NodeTerm);
				},
				NodeBinaryExpression => {
					GenerateBinaryExpression(NodeBinaryExpression);
				}
			);
		}

		void push(in string? value) {
			output += "\tpush " + value +"\n";
			stack_size++;
		}

		void pop(in string register) {
			output += "\tpop " + register +"\n";
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

		string create_label() {
			return "label" + label_count++;
		}

		void GenerateScope(in NodeScope nodeScope) {
			BeginScope();
			foreach (NodeStatement statement in nodeScope.statements) {
				GenerateStatement(statement);
			}
			EndScope();
		}

		unsafe void GenerateIfPredicate(in NodeIfPredicate predicate, string final_label) {
			predicate.predicate.Switch(
				NodeIfPredicateElseIf => {
                    GenerateElseIf(NodeIfPredicateElseIf, final_label);
                },
				NodeIfPredicateElse => {
					if (NodeIfPredicateElse.statement != null) {
						GenerateStatement(*NodeIfPredicateElse.statement);
					}
				}
			);
		}

		void GenerateStatement(in NodeStatement nodesStatement) {
			nodesStatement.statement.Switch(
				NodeStatementExit => {
					GenerateExpression(NodeStatementExit.expression);
					output += "\tmov rax, " + (int)SystemCall.exit + "\n";
					pop("rdi");
					output += "\tsyscall\n";
				},
				NodeStatementVar => {
					var identifier_value = NodeStatementVar.identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count > 0) {
						var (line_number, column_number, _) = NodeStatementVar.identifier;
						Program.Error("Identifier `" + identifier_value + "` is already declared", line_number, column_number);
					}
					variables.Add(new Variable { name = identifier_value + "", stack_location = stack_size });
					GenerateExpression(NodeStatementVar.expression);
				},
				NodeScope => {
					GenerateScope(NodeScope);
				},
				NodeStatementIf => {
					string final_label = create_label();
					GenerateIf(NodeStatementIf, final_label);
					output += final_label + ":\n";
				},
				NodeStatementAssign => {
					string? identifier_value = NodeStatementAssign.identifier.value;
					var values = variables.FindAll(variable => variable.name == identifier_value);
					if (values.Count == 0) {
						var (line_number, column_number, _) = NodeStatementAssign.identifier;
						Program.Error("Undeclared identifier `" + identifier_value + "`", line_number, column_number);
					}
					GenerateExpression(NodeStatementAssign.expression);
					pop("rax");
					output += "\tmov [rsp + " + (stack_size - values[0].stack_location - 1) * 8 + " ], rax\n";
				},
				NodeStatementEmpty => {}
			);
		}

		unsafe void GenerateIf(in NodeStatementIf if_, string final_label) {
			GenerateExpression(if_.expression);
			pop("rax");
			string label = create_label();
			output += "\ttest rax, rax\n";
			output += "\tjz " + (if_.predicate->HasValue ? label : final_label) + "\n";
			GenerateStatement(*if_.statement);
			if (if_.predicate->HasValue) {
				output += "\tjmp " + final_label + "\n";
				output += label + ":\n";
				GenerateIfPredicate(if_.predicate->Value, final_label);
			}
		}

        unsafe void GenerateElseIf(NodeIfPredicateElseIf if_, in string final_label) => GenerateIf(*(NodeStatementIf*)&if_, final_label);

        foreach (NodeStatement nodesStatement in root.statements) {
			GenerateStatement(nodesStatement);
		}

		output += "\tmov rax, " + (int)SystemCall.exit + "\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";

		return output.Trim();
	}
}