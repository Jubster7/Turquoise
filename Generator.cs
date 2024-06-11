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

		void Generate_Term(NodeTerm nodeTerm) {
			nodeTerm.term.Switch(
				NodeTermIntLiteral => {
					push(NodeTermIntLiteral.int_literal.value);
				},
				NodeTermIdentifier => {
					var identifier_value = NodeTermIdentifier.identifier.value;
					if (!variables.TryGetValue(identifier_value!, out Variable value)) {
						throw new Exception("Error: Undeclared identifier `" + identifier_value + "`");
					}
					push("QWORD [rsp + " + (stack_size - value.stack_location - 1) * 8 + "]");
				}
			);
		}

		unsafe void GenerateExpression(NodeExpression nodeExpression) {
			nodeExpression.expression.Switch(
				NodeTerm => {
					Generate_Term(NodeTerm);
				},
				NodeBinaryExpression => {
					GenerateExpression(*NodeBinaryExpression.binary_expression.lhs);
					GenerateExpression(*NodeBinaryExpression.binary_expression.rhs);
					pop("rax");
					pop("rbx");
					output += "\tadd rax, rbx\n";
					push("rax");
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