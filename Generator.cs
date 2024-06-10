using System.Diagnostics.Contracts;
namespace Compiler;

enum SystemCall {
	exit = 0x2000001,
	//read = 0x20000003,
	//write = 0x20000004,
	//sleep = 0x20000240,
}

struct Variable {
	public int stack_location;
}

static class Generator {

	[Pure] public static string Generate(NodeProgram root) {
		int stack_size = 0;
		string entry_point_name = Program.assembly_entry_point_label;
		string output = $"global {entry_point_name}\n{entry_point_name}:\n";
		Dictionary<string, Variable> variables = [];



		void GenerateExpression(NodeExpression nodeExpression) {
			nodeExpression.expression.Switch(
				NodeExpressionIntLiteral => {
					push(NodeExpressionIntLiteral.int_literal.value + "");
				},
				NodeExpressionIdentifier => {
					var identifier_value = NodeExpressionIdentifier.identifier.value;
					if (!variables.TryGetValue(identifier_value!, out Variable value)) {
						throw new Exception("Error: Undeclared identifier `" + identifier_value + "`");
					}
					push("QWORD [rsp + " + (stack_size - value.stack_location - 1) * 8 + "]");
				}
			);
		}

		void push(string register) {
			output += "\tpush " + register +"\n";
			stack_size++;
		}

		void pop(string register) {
			output += "\tpop " + register +"\n";
			stack_size--;
		}

		void GenerateStatement(in NodesStatement nodesStatement) {
			nodesStatement.statement.Switch(
				NodeStatementExit => {
					GenerateExpression(NodeStatementExit.expression);
					output += "\tmov rax, " + (int)SystemCall.exit + "\n";
					pop("rdi");
					output += "\tsyscall\n";
				},
				NodeStatementVar => {
					if (variables.ContainsKey(NodeStatementVar.identifier.value!)) {
						throw new Exception("Error: Identifier `" + NodeStatementVar.identifier.value + "` is already used");
					}
                    variables.Add(NodeStatementVar.identifier.value! , new Variable { stack_location = stack_size });
					GenerateExpression(NodeStatementVar.expression);
				}
			);
		}

		foreach (NodesStatement nodesStatement in root.statements) {
			GenerateStatement(nodesStatement);
		}


		output += "\tmov rax, " + (int)SystemCall.exit + "\n";
		output += "\tmov rdi, 0\n";
		output += "\tsyscall";
		return output;
	}
}